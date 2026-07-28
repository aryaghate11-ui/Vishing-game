using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhoneCallBridge : MonoBehaviour
{
    public static PhoneCallBridge Instance { get; private set; }

    [Header("Relay")]
    [Tooltip("Use your LAN IP for real phone testing, for example http://192.168.1.14:3000")]
    public string httpBaseUrl = "http://10.20.3.193";

    [Tooltip("Leave empty to generate a fresh session each play.")]
    public string sessionId = "";

    [Header("Auto UI")]
    public bool createQrPanelOnStart = true;
    public KeyCode testCallKey = KeyCode.C;
    public KeyCode testBankKey = KeyCode.B;

    public event Action<string> OnPhoneReady;
    public event Action<string> OnCallAnswered;
    public event Action<string> OnCallDeclined;
    public event Action<string> OnCallHungUp;
    public event Action<string> OnAccountFrozen;
    public event Action<string, string> OnRawPhoneEvent;

    private ClientWebSocket socket;
    private CancellationTokenSource cancellation;
    private SynchronizationContext unityContext;
    private Text statusText;
    private RawImage qrImage;
    private bool isConnecting;

    public string ConnectUrl => $"{HttpBaseUrlClean}/connect?session={Uri.EscapeDataString(sessionId)}";
    public string WebSocketUrl => $"{HttpBaseUrlClean.Replace("https://", "wss://").Replace("http://", "ws://")}/socket?role=unity&session={Uri.EscapeDataString(sessionId)}";
    private string HttpBaseUrlClean => httpBaseUrl.TrimEnd('/');

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        unityContext = SynchronizationContext.Current;

        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = GenerateSessionId();
    }

    private IEnumerator Start()
    {
        if (createQrPanelOnStart)
            CreateQrPanel();

        yield return StartCoroutine(LoadQrCode());
        Connect();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testCallKey))
            TriggerIncomingCall("Cyber Crime Cell", "Unknown number");

        if (Input.GetKeyDown(testBankKey))
            ShowBankingApp();
    }

    private async void OnDestroy()
    {
        if (Instance == this) Instance = null;
        cancellation?.Cancel();

        if (socket != null)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unity closing", CancellationToken.None);
            }
            catch
            {
                // Unity is closing; no recovery needed.
            }
            socket.Dispose();
        }

        cancellation?.Dispose();
    }

    public async void Connect()
    {
        if (isConnecting) return;
        isConnecting = true;
        SetStatus($"Connecting session {sessionId}...");

        cancellation?.Cancel();
        cancellation = new CancellationTokenSource();

        socket?.Dispose();
        socket = new ClientWebSocket();

        try
        {
            await socket.ConnectAsync(new Uri(WebSocketUrl), cancellation.Token);
            SetStatus($"Phone session ready: {sessionId}");
            _ = ReceiveLoop(cancellation.Token);
        }
        catch (Exception exception)
        {
            SetStatus($"Relay connection failed: {exception.Message}");
        }
        finally
        {
            isConnecting = false;
        }
    }

    public void TriggerIncomingCall(string caller = "Unknown Caller", string subtitle = "Mobile")
    {
        SendEvent("incoming_call", $"{{\"caller\":\"{Escape(caller)}\",\"subtitle\":\"{Escape(subtitle)}\"}}");
    }

    public void ShowBankingApp()
    {
        SendEvent("show_banking_app", "{}");
    }

    public void EndCall()
    {
        SendEvent("end_call", "{}");
    }

    public void ShowIdle()
    {
        SendEvent("show_idle", "{}");
    }

    public async void SendEvent(string eventName, string payloadJson = "{}")
    {
        if (socket == null || socket.State != WebSocketState.Open)
        {
            SetStatus("Cannot send. Relay is not connected.");
            return;
        }

        string json = $"{{\"type\":\"event\",\"target\":\"phone\",\"event\":\"{Escape(eventName)}\",\"payload\":{payloadJson}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellation.Token);
        }
        catch (Exception exception)
        {
            SetStatus($"Send failed: {exception.Message}");
        }
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        byte[] buffer = new byte[8192];

        while (!token.IsCancellationRequested && socket != null && socket.State == WebSocketState.Open)
        {
            try
            {
                WebSocketReceiveResult result;
                StringBuilder builder = new StringBuilder();

                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        SetStatus("Relay disconnected.");
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                string json = builder.ToString();
                unityContext.Post(_ => HandleMessage(json), null);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                SetStatus($"Receive failed: {exception.Message}");
                return;
            }
        }
    }

    private void HandleMessage(string json)
    {
        string eventName = ExtractJsonString(json, "event");
        string payload = ExtractJsonObject(json, "payload");

        if (string.IsNullOrEmpty(eventName))
            return;

        OnRawPhoneEvent?.Invoke(eventName, payload);

        switch (eventName)
        {
            case "phone_ready":
                SetStatus("Phone connected and sound enabled.");
                OnPhoneReady?.Invoke(payload);
                break;
            case "call_answered":
                SetStatus("Phone call answered.");
                OnCallAnswered?.Invoke(payload);
                break;
            case "call_declined":
                SetStatus("Phone call declined.");
                OnCallDeclined?.Invoke(payload);
                break;
            case "call_hung_up":
                SetStatus("Phone call ended.");
                OnCallHungUp?.Invoke(payload);
                break;
            case "account_frozen":
                SetStatus("Account freeze confirmed.");
                OnAccountFrozen?.Invoke(payload);
                break;
        }
    }

    private IEnumerator LoadQrCode()
    {
        if (qrImage == null) yield break;

        string qrUrl = $"{HttpBaseUrlClean}/qr/{Uri.EscapeDataString(sessionId)}.png";
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(qrUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetStatus($"QR load failed. Open manually: {ConnectUrl}");
            request.Dispose();
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        qrImage.texture = texture;
        request.Dispose();
    }

    private void CreateQrPanel()
    {
        Canvas canvas = new GameObject("Phone Call QR Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvas.gameObject);

        GameObject panelObject = new GameObject("QR Panel");
        panelObject.transform.SetParent(canvas.transform, false);
        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0.02f, 0.03f, 0.04f, 0.88f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(260f, 350f);

        qrImage = CreateRawImage(panelObject.transform, "QR Image", new Vector2(0f, 44f), new Vector2(210f, 210f));
        statusText = CreateText(panelObject.transform, "Status", $"Session: {sessionId}\nScan QR, then tap Connect.", 15, new Vector2(0f, -110f), new Vector2(220f, 90f));
        CreateText(panelObject.transform, "Title", "Phone Link", 20, new Vector2(0f, 144f), new Vector2(220f, 34f));
    }

    private RawImage CreateRawImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RawImage image = obj.AddComponent<RawImage>();
        image.color = Color.white;
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return image;
    }

    private Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return text;
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[PhoneCallBridge] {message}");
        if (statusText != null)
            statusText.text = $"Session: {sessionId}\n{message}";
    }

    private static string GenerateSessionId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Random random = new System.Random();
        char[] id = new char[6];
        for (int i = 0; i < id.Length; i++)
            id[i] = chars[random.Next(chars.Length)];
        return new string(id);
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string ExtractJsonString(string json, string key)
    {
        string marker = $"\"{key}\"";
        int keyIndex = json.IndexOf(marker, StringComparison.Ordinal);
        if (keyIndex < 0) return "";

        int colon = json.IndexOf(':', keyIndex);
        if (colon < 0) return "";

        int start = json.IndexOf('"', colon + 1);
        if (start < 0) return "";

        StringBuilder builder = new StringBuilder();
        bool escaping = false;
        for (int i = start + 1; i < json.Length; i++)
        {
            char c = json[i];
            if (escaping)
            {
                builder.Append(c);
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                escaping = true;
                continue;
            }

            if (c == '"')
                return builder.ToString();

            builder.Append(c);
        }

        return "";
    }

    private static string ExtractJsonObject(string json, string key)
    {
        string marker = $"\"{key}\"";
        int keyIndex = json.IndexOf(marker, StringComparison.Ordinal);
        if (keyIndex < 0) return "{}";

        int colon = json.IndexOf(':', keyIndex);
        if (colon < 0) return "{}";

        int start = json.IndexOf('{', colon + 1);
        if (start < 0) return "{}";

        int depth = 0;
        bool inString = false;
        bool escaping = false;

        for (int i = start; i < json.Length; i++)
        {
            char c = json[i];

            if (escaping)
            {
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                escaping = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{') depth++;
            if (c == '}') depth--;

            if (depth == 0)
                return json.Substring(start, i - start + 1);
        }

        return "{}";
    }
}
