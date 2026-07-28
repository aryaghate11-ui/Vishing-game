using System.Collections;
using UnityEngine;

public class PhoneCallTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnPlayerEnter,
        OnStartAfterDelay,
        ManualOnly
    }

    [Header("Trigger")]
    public TriggerMode triggerMode = TriggerMode.OnPlayerEnter;
    public string playerTag = "Player";
    public bool triggerOnce = true;
    public float delaySeconds = 0f;

    [Header("Call")]
    public string callerName = "Cyber Crime Cell";
    public string callerSubtitle = "Unknown number";

    [Header("Optional Follow Up")]
    public bool showBankingAppAfterAnswer = false;
    public float bankingDelayAfterAnswer = 4f;

    private bool hasTriggered;

    private void Start()
    {
        if (triggerMode == TriggerMode.OnStartAfterDelay)
            StartCoroutine(TriggerAfterDelay());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.OnPlayerEnter) return;
        if (!other.CompareTag(playerTag)) return;
        TriggerCall();
    }

    public void TriggerCall()
    {
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        if (PhoneCallBridge.Instance == null)
        {
            Debug.LogWarning("[PhoneCallTrigger] No PhoneCallBridge found in scene.");
            return;
        }

        PhoneCallBridge.Instance.TriggerIncomingCall(callerName, callerSubtitle);

        if (showBankingAppAfterAnswer)
        {
            PhoneCallBridge.Instance.OnCallAnswered -= HandleCallAnswered;
            PhoneCallBridge.Instance.OnCallAnswered += HandleCallAnswered;
        }
    }

    private IEnumerator TriggerAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        TriggerCall();
    }

    private void HandleCallAnswered(string payload)
    {
        PhoneCallBridge.Instance.OnCallAnswered -= HandleCallAnswered;
        StartCoroutine(ShowBankingAfterDelay());
    }

    private IEnumerator ShowBankingAfterDelay()
    {
        yield return new WaitForSeconds(bankingDelayAfterAnswer);
        PhoneCallBridge.Instance.ShowBankingApp();
    }
}
