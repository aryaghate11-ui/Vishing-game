const params = new URLSearchParams(location.search);
const session = sanitizeSession(params.get("session"));
const socketUrl = `${location.protocol === "https:" ? "wss" : "ws"}://${location.host}/socket?role=phone&session=${encodeURIComponent(session || "")}`;

const title = document.getElementById("title");
const statusText = document.getElementById("status");
const sessionLabel = document.getElementById("sessionLabel");

let ws = null;
let reconnectTimer = null;

sessionLabel.textContent = session ? `Session ${session}` : "Missing session";

if (!session) {
  document.body.classList.add("error");
  title.textContent = "Invalid QR";
  statusText.textContent = "Scan the QR code from the Unity game again.";
} else {
  connect();
}

function connect() {
  clearTimeout(reconnectTimer);
  title.textContent = "Connecting...";
  statusText.textContent = "Keep this page open while the game is running.";

  ws = new WebSocket(socketUrl);

  ws.addEventListener("open", () => {
    title.textContent = "Phone Connected";
    statusText.textContent = "The game can now detect this phone session.";
    send("phone_ready", { userAgent: navigator.userAgent });
  });

  ws.addEventListener("message", (event) => {
    const message = safeJson(event.data);
    if (!message || message.type !== "event") return;

    if (message.event === "incoming_call") {
      title.textContent = "Call Trigger Received";
      statusText.textContent = "Phone UI placeholder only. Teammate webapp can replace this screen.";
      send("call_trigger_received", message.payload || {});
    }
  });

  ws.addEventListener("close", () => {
    title.textContent = "Reconnecting...";
    statusText.textContent = "Trying to restore the game connection.";
    reconnectTimer = setTimeout(connect, 1400);
  });

  ws.addEventListener("error", () => {
    title.textContent = "Connection Issue";
    statusText.textContent = "Make sure the relay server is running and the phone is on the same network.";
  });
}

function send(event, payload = {}) {
  if (!ws || ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({
    type: "event",
    target: "unity",
    event,
    payload
  }));
}

function sanitizeSession(value) {
  if (!value) return "";
  const sessionValue = String(value).trim();
  return /^[A-Z0-9_-]{4,32}$/i.test(sessionValue) ? sessionValue : "";
}

function safeJson(value) {
  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}
