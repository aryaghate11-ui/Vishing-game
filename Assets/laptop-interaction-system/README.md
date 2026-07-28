# Laptop Interaction System

This is a standalone Unity UI system for the apartment laptop.

It supports:

- Press `E` near the laptop to open it.
- Cursor becomes visible and unlocked while the laptop UI is open.
- Player movement scripts can be disabled while the UI is open.
- Clickable UI segments can open other laptop screens.
- Back button returns to the previous screen.
- Escape closes the laptop.
- Optional gating can make the laptop usable only when another script allows it.

## Dependencies

None. This version is fully standalone and does not require `GameDirector`, `StoryBeat`, or `StoryEvent`.

## Setup

1. Add `LaptopInteractionSystem` to the laptop object in the scene.
2. Assign `Player`.
3. Assign `Laptop Ui Root`, the parent object for the whole laptop UI canvas/panel.
4. Assign `Start Screen`, usually the desktop or inbox screen.
5. Add player movement/look scripts to `Movement Scripts To Disable`.
6. For testing today, keep `Require Story Gate` off.

When the player is near the laptop and presses `E`, the laptop opens. Later, when the story system exists, turn `Require Story Gate` on and call `SetStoryGateOpen(true)` only during the email beat.

## UI screen setup

For every laptop UI page:

- Create a child panel, for example `DesktopScreen`, `EmailInboxScreen`, `SuspiciousEmailScreen`, `MalwareLoadingScreen`.
- Add normal email panels too, for example `ElectricityBillEmailScreen`, `BankPromoEmailScreen`, `CollegeNoticeEmailScreen`.
- Add `LaptopScreen` to that panel.
- Keep only the start screen active in your prefab if you want, but the system will show/hide screens at runtime.

## Clickable segments

For each clickable UI button or transparent segment:

1. Add a Unity `Button`.
2. Add `LaptopClickableSegment`.
3. Assign `Laptop`.
4. Optional: assign `Target Screen`.
5. Optional: enable `Report Laptop Event`.
6. Type an event name, for example `SuspiciousLinkClicked`.
7. In the Button `OnClick`, call `LaptopClickableSegment.Click()`.

Recommended story event wiring:

- Inbox/email icon -> target `EmailInboxScreen`, no story event needed.
- Normal email rows -> target their normal email screen, no event needed.
- Suspicious email row -> target `SuspiciousEmailScreen`, report event name `EmailClicked`.
- Suspicious link -> target `MalwareLoadingScreen`, report event name `SuspiciousLinkClicked`.
- Back buttons -> call `LaptopScreen.Back()`.

## Easier email row setup

You can use `LaptopEmailRow` instead of `LaptopClickableSegment` for inbox rows.

For a normal email:

```text
Email Kind = Normal
Email Screen = ElectricityBillEmailScreen
```

For the scam email:

```text
Email Kind = Suspicious
Email Screen = SuspiciousEmailScreen
Report Event For Suspicious = on
Suspicious Email Event Name = EmailClicked
```

In the Button `OnClick`, call:

```text
LaptopEmailRow.OpenEmail()
```

Normal emails will open and go back normally. They will not fire any event.

## Tomorrow story hookup

For now, no story scripts are required.

When the story system exists, use these methods from your GameDirector or adapter:

```csharp
SetRequireStoryGate(true);
SetStoryGateOpen(true);  // only during email checking
SetStoryGateOpen(false); // all other beats
```

Listen to `OnLaptopEvent(string eventName)` for:

```text
LaptopClicked
EmailClicked
SuspiciousLinkClicked
```
