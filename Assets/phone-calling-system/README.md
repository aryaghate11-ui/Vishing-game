# Standalone Unity Real-Phone Calling System

Drop-in relay/Unity system for a Unity desktop game where scanning a QR code connects a real phone browser. For now, the included phone page is only a placeholder: it shows `Phone Connected` and sends `phone_ready` back to Unity. Your teammate can replace `relay/public` later with the real phone UI.

## Folder Structure

```text
phone-calling-system/
  relay/
    package.json
    server.js
    public/
      index.html        # temporary placeholder page
      styles.css
      app.js
  unity/
    Scripts/
      PhoneCallBridge.cs
      PhoneCallTrigger.cs
      PhoneCallSequenceExample.cs
```

## Fast Setup

1. Copy `unity/Scripts` into your Unity project's `Assets/Scripts/PhoneCallSystem`.
2. In Unity, create an empty GameObject named `PhoneCallBridge`.
3. Add the `PhoneCallBridge` component to it.
4. Set `Http Base Url` to your relay URL, for example `http://192.168.1.14:3000`.
5. Press Play. The bridge creates a small QR panel automatically.
6. Scan the QR on your phone.
7. The phone page should show `Phone Connected`.
8. Add `PhoneCallTrigger` to any trigger box or call `PhoneCallBridge.Instance.TriggerIncomingCall(...)` from a coroutine.

## Relay Setup

Open a terminal inside `relay`:

```bash
npm install
npm start
```

If testing on an actual phone, use your computer's LAN IP, not `localhost`:

```bash
set PUBLIC_BASE_URL=http://YOUR_LAN_IP:3000
npm start
```

On macOS/Linux:

```bash
PUBLIC_BASE_URL=http://YOUR_LAN_IP:3000 npm start
```

## Unity Trigger Examples

Box trigger:

1. Add a Box Collider to any GameObject.
2. Enable `Is Trigger`.
3. Add `PhoneCallTrigger`.
4. Pick `Trigger Mode = On Player Enter`.
5. Set caller name and subtitle.

Coroutine/manual trigger:

```csharp
yield return new WaitForSeconds(12f);
PhoneCallBridge.Instance.TriggerIncomingCall("Cyber Crime Cell", "Unknown number");
```

Show fake banking app:

```csharp
PhoneCallBridge.Instance.ShowBankingApp();
```

Listen for phone events:

```csharp
PhoneCallBridge.Instance.OnCallAnswered += payload => Debug.Log("Answered");
PhoneCallBridge.Instance.OnCallDeclined += payload => Debug.Log("Declined");
PhoneCallBridge.Instance.OnAccountFrozen += payload => Debug.Log("Account frozen");
```

## Replacing The Phone UI Later

The real phone app can replace only these files:

```text
relay/public/index.html
relay/public/styles.css
relay/public/app.js
```

Keep the same WebSocket URL pattern:

```text
/socket?role=phone&session=SESSION_ID
```

When the phone connects, send this back to Unity:

```json
{ "type": "event", "target": "unity", "event": "phone_ready", "payload": {} }
```

When Unity sends an incoming call later, your teammate's phone UI should listen for:

```json
{ "type": "event", "event": "incoming_call", "payload": { "caller": "...", "subtitle": "..." } }
```

Mobile browsers block autoplay audio. When the real phone UI is added, include a `Connect / Enable Sound` tap before ringtone/vibration.

For Unity, use a desktop build or editor play mode with API compatibility set to `.NET Framework` / `.NET 4.x` if your project exposes that setting. The included bridge uses `ClientWebSocket`, so this is aimed at desktop/editor builds, not Unity WebGL.

## Demo Flow

1. Start relay.
2. Start Unity scene.
3. Scan QR.
4. Phone shows `Phone Connected`.
5. Walk into a trigger box in Unity.
6. Placeholder page confirms the call trigger was received.
7. Teammate can later replace the placeholder with ringtone, vibration, Answer, and Decline.
