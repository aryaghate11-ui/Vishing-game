# Unity Phone Call Relay

This folder runs the middle layer between Unity and the player's phone. The included `public` page is only a temporary placeholder that shows `Phone Connected` after the QR is scanned.

## Start

```bash
npm install
npm start
```

By default it runs at:

```text
http://localhost:3000
```

For testing on a real phone, the phone must reach the computer running the server. Use your computer's LAN IP:

```bash
set PUBLIC_BASE_URL=http://YOUR_LAN_IP:3000
npm start
```

Example:

```bash
set PUBLIC_BASE_URL=http://192.168.1.14:3000
npm start
```

Then set the same base URL in Unity's `PhoneCallBridge`.

## Events Unity Can Send

```json
{ "event": "incoming_call", "payload": { "caller": "Cyber Crime Cell", "subtitle": "Unknown number" } }
{ "event": "show_banking_app", "payload": {} }
{ "event": "end_call", "payload": {} }
{ "event": "show_idle", "payload": {} }
```

## Events Unity Receives

```json
{ "event": "phone_ready" }
{ "event": "call_trigger_received" }
{ "event": "call_answered" }
{ "event": "call_declined" }
{ "event": "call_hung_up" }
{ "event": "account_frozen" }
```

## Hackathon Safety

- Session IDs isolate rooms.
- Empty sessions are cleaned up.
- Expired sessions are closed.
- Payloads are size-limited.
- Invalid roles and invalid session IDs are rejected.
