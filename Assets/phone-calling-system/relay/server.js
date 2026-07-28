const express = require("express");
const http = require("http");
const path = require("path");
const QRCode = require("qrcode");
const { WebSocket, WebSocketServer } = require("ws");

const PORT = Number(process.env.PORT || 3000);
const PUBLIC_BASE_URL = process.env.PUBLIC_BASE_URL || `http://localhost:${PORT}`;
const SESSION_TTL_MS = Number(process.env.SESSION_TTL_MS || 1000 * 60 * 30);
const HEARTBEAT_MS = Number(process.env.HEARTBEAT_MS || 30000);
const SESSION_RE = /^[A-Z0-9_-]{4,32}$/i;
const ROLES = new Set(["unity", "phone", "monitor"]);

const app = express();
const server = http.createServer(app);
const wss = new WebSocketServer({ server, path: "/socket" });
const sessions = new Map();

app.use(express.static(path.join(__dirname, "public")));

app.get("/connect", (req, res) => {
  res.sendFile(path.join(__dirname, "public", "index.html"));
});

app.get("/qr/:session.png", async (req, res) => {
  const session = normalizeSession(req.params.session);
  if (!session) return res.status(400).send("Invalid session");

  const connectUrl = `${PUBLIC_BASE_URL}/connect?session=${encodeURIComponent(session)}`;
  try {
    const png = await QRCode.toBuffer(connectUrl, {
      type: "png",
      margin: 2,
      width: 512,
      errorCorrectionLevel: "M"
    });
    res.type("png").send(png);
  } catch (error) {
    console.error("QR generation failed", error);
    res.status(500).send("QR generation failed");
  }
});

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    sessions: sessions.size,
    now: Date.now()
  });
});

wss.on("connection", (ws, req) => {
  const url = new URL(req.url, `http://${req.headers.host}`);
  const session = normalizeSession(url.searchParams.get("session"));
  const role = url.searchParams.get("role");

  if (!session || !ROLES.has(role)) {
    ws.close(1008, "Invalid session or role");
    return;
  }

  ws.isAlive = true;
  ws.role = role;
  ws.session = session;

  const room = getRoom(session);
  room.clients.add(ws);
  room.updatedAt = Date.now();

  send(ws, "connected", {
    session,
    role,
    connectUrl: `${PUBLIC_BASE_URL}/connect?session=${encodeURIComponent(session)}`
  });

  broadcast(room, ws, "peer_joined", { role });
  broadcastPresence(room);

  ws.on("pong", () => {
    ws.isAlive = true;
  });

  ws.on("message", (raw) => {
    room.updatedAt = Date.now();
    const message = parseMessage(raw);
    if (!message) {
      send(ws, "error", { reason: "Invalid JSON message" });
      return;
    }

    const type = typeof message.type === "string" ? message.type : "event";
    const event = typeof message.event === "string" ? message.event : "";
    if (!event || event.length > 64) {
      send(ws, "error", { reason: "Missing or invalid event name" });
      return;
    }

    const payload = sanitizePayload(message.payload);
    const target = message.target === "unity" || message.target === "phone" || message.target === "monitor"
      ? message.target
      : null;

    broadcast(room, ws, type, {
      event,
      payload,
      from: role,
      sentAt: Date.now()
    }, target);
  });

  ws.on("close", () => {
    room.clients.delete(ws);
    room.updatedAt = Date.now();
    broadcast(room, null, "peer_left", { role });
    broadcastPresence(room);
    scheduleRoomCleanup(session);
  });
});

const heartbeat = setInterval(() => {
  for (const ws of wss.clients) {
    if (!ws.isAlive) {
      ws.terminate();
      continue;
    }
    ws.isAlive = false;
    ws.ping();
  }
  cleanupExpiredRooms();
}, HEARTBEAT_MS);

wss.on("close", () => clearInterval(heartbeat));

server.listen(PORT, "0.0.0.0", () => {
  console.log(`Relay running on ${PUBLIC_BASE_URL}`);
  console.log(`Unity WebSocket: ${PUBLIC_BASE_URL.replace(/^http/, "ws")}/socket?role=unity&session=YOUR_SESSION`);
});

function normalizeSession(value) {
  if (!value) return null;
  const session = String(value).trim();
  return SESSION_RE.test(session) ? session : null;
}

function getRoom(session) {
  let room = sessions.get(session);
  if (!room) {
    room = { clients: new Set(), updatedAt: Date.now(), cleanupTimer: null };
    sessions.set(session, room);
  }
  if (room.cleanupTimer) {
    clearTimeout(room.cleanupTimer);
    room.cleanupTimer = null;
  }
  return room;
}

function scheduleRoomCleanup(session) {
  const room = sessions.get(session);
  if (!room || room.clients.size > 0 || room.cleanupTimer) return;
  room.cleanupTimer = setTimeout(() => {
    const latest = sessions.get(session);
    if (latest && latest.clients.size === 0) sessions.delete(session);
  }, 10000);
}

function cleanupExpiredRooms() {
  const now = Date.now();
  for (const [session, room] of sessions.entries()) {
    if (room.clients.size === 0 || now - room.updatedAt > SESSION_TTL_MS) {
      for (const client of room.clients) client.close(1001, "Session expired");
      sessions.delete(session);
    }
  }
}

function parseMessage(raw) {
  try {
    return JSON.parse(raw.toString());
  } catch {
    return null;
  }
}

function sanitizePayload(payload) {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) return {};
  const json = JSON.stringify(payload);
  if (json.length > 4096) return { truncated: true };
  return JSON.parse(json);
}

function send(ws, type, data) {
  if (ws.readyState !== WebSocket.OPEN) return;
  ws.send(JSON.stringify({ type, ...data }));
}

function broadcast(room, sender, type, data, targetRole = null) {
  for (const client of room.clients) {
    if (client === sender) continue;
    if (targetRole && client.role !== targetRole) continue;
    send(client, type, data);
  }
}

function broadcastPresence(room) {
  const counts = { unity: 0, phone: 0, monitor: 0 };
  for (const client of room.clients) counts[client.role] += 1;
  broadcast(room, null, "presence", { counts });
}
