# Raw Input Prototype

This folder contains a Windows-only feasibility harness for Issue #2. Its job is to answer one question: can Windows Raw Input distinguish between multiple physical keyboards and mice connected to the same machine?

## Prototype Purpose

The prototype does not try to implement the future application. It only:

- registers for keyboard and mouse Raw Input
- listens for `WM_INPUT` and `WM_INPUT_DEVICE_CHANGE`
- logs which device produced each event
- shows a snapshot of currently detected keyboard and mouse devices
- exposes enough detail to compare two physical devices manually

The runnable project lives at `prototypes/raw-input-test/RawInputPrototype`.

## What It Tests

- whether keyboard events include a device-specific source handle
- whether mouse events include a device-specific source handle
- whether Raw Input exposes a stable device path or identifier while the app is running
- whether two physical keyboards and/or two physical mice appear as distinct event sources
- whether reconnecting a device triggers useful arrival and removal information

## What The Window Shows

The WPF window has two sections:

- a device snapshot table with device type, handle, parsed VID/PID when available, raw device path, and type-specific details
- a bounded live event log with timestamp, event type, foreground/background source, device handle, identifier, and an event summary

The event log is capped at 300 entries so the UI stays usable during manual testing.

## How to Run

Run this from Windows, not WSL. WPF and Raw Input are Windows-only.

1. Install the .NET 8 SDK on Windows if it is not already installed.
2. Open Windows Terminal, PowerShell, or a Developer Command Prompt.
3. Change into the project directory:

```powershell
cd .\prototypes\raw-input-test\RawInputPrototype
```

4. Launch the prototype:

```powershell
dotnet run
```

You can also open `RawInputPrototype.csproj` in Visual Studio 2022 and run it there.

## Exact Manual Test Steps

Use these steps to test with two keyboards or two mice:

1. Connect at least two keyboards and/or two mice to the same Windows machine.
2. Start the prototype and leave it open.
3. Check the device snapshot table first.
4. Identify whether the table shows separate rows for the devices you plan to test.
5. Press a key on keyboard A several times.
6. Press a different key on keyboard B several times.
7. Compare the `Handle` and `Device` values in the live event log for keyboard A versus keyboard B.
8. If you have two mice, move mouse A, click once or twice, then repeat with mouse B.
9. Compare the `Handle` and `Device` values in the live event log for mouse A versus mouse B.
10. Disconnect and reconnect one device if you want to observe arrival/removal behavior and whether the identifier changes.

## What To Look For

During testing, focus on whether:

- keyboard A and keyboard B generate different device handles and/or identifiers
- mouse A and mouse B generate different device handles and/or identifiers
- the same physical device keeps the same identifier while the app remains open
- the snapshot metadata matches the devices that are generating events
- reconnecting a device changes the identifier or device handle unexpectedly

## What Counts As Success Or Failure

Success for this prototype means:

- the window receives raw keyboard and mouse input events
- each event can be associated with a device handle
- at least two physical keyboards and/or two physical mice can be told apart by the identifiers shown in the UI

Partial success means:

- events are received and handles differ, but the metadata is noisy, incomplete, or inconsistent enough that more research is needed

Failure means:

- raw events are not received reliably
- multiple physical devices collapse into the same source identity
- the available identifiers are too unstable or ambiguous to support later mapping logic

## Evidence To Capture

Record real observations only. Useful evidence includes:

- a screenshot of the device snapshot table
- copied event log entries showing keyboard A versus keyboard B and/or mouse A versus mouse B
- Windows version, device models, and connection type notes
- reconnect observations, including whether handles or identifiers changed

The `Copy Event Log` button copies the current visible log rows to the clipboard to make this easier.

## Limitations

- This is a feasibility spike, not production architecture.
- It does not switch displays, define zones, store mappings, or persist anything.
- It relies on Raw Input device handles, device paths, and parsed VID/PID values when available.
- It does not currently resolve polished human-friendly device names through SetupAPI or other device-enumeration layers.
- Some hardware setups may surface composite devices, shared receivers, or integrated devices in ways that need further interpretation.
- The prototype uses `RIDEV_INPUTSINK`, so it can continue receiving input while the window remains open in the background.
