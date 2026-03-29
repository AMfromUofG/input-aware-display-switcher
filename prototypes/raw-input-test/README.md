# Raw Input Prototype

This folder contains the Windows-only feasibility harness used for:

- Issue #2: prototype physical device detection using Raw Input
- Issue #3: investigate stable device identity and metadata for persistence

The runnable project lives at `prototypes/raw-input-test/RawInputPrototype`.

## Purpose

This is research tooling, not the application.

It is intended to answer two narrow questions:

- can Raw Input attribute keyboard and mouse activity to the correct physical device at runtime?
- what metadata appears useful for recognising the same physical device later?

It does not implement:

- persistence storage
- zone mapping
- switching logic
- tray behaviour
- main app UI

## What The Prototype Shows

The current prototype provides:

- a live event log showing runtime event attribution
- a device snapshot table for currently visible keyboards and mice
- a selected-device analysis view for identity-related metadata
- clipboard export for snapshot comparison during restart and reconnect testing

The current snapshot and analysis tooling can expose:

- Raw Input handle
- device type
- raw device path
- VID/PID when derivable
- `RID_DEVICE_INFO` details
- normalized path and related path fragments
- instance ID when resolvable
- friendly name when available
- candidate key and fingerprint-style research summaries

## Findings Established So Far

On the current test setup, the prototype has already helped establish that:

- Raw Input can distinguish multiple physical keyboards and mice at runtime.
- Distinct physical devices produced distinct event sources and handles during a running session.
- A shared wireless receiver can still expose separate keyboard and pointing-device identities.
- Raw handles are useful for live runtime identification, but are not a safe sole persisted identity.
- VID/PID is useful supporting metadata, but not strong enough on its own for persistence.
- A composite identity strategy appears more realistic for later persistence work.

## What It Does Not Prove

The prototype does not yet prove that:

- a single persisted identifier will remain stable across every reconnect, reboot, receiver change, or port move
- every device class will fit the same model
- controller-style devices should be included in the main switching scope

The current controller behaviour is a reminder of that last point:

- an Xbox controller appeared with handle `0x0000000000000000`
- metadata was unresolved
- only the Xbox/Guide button appeared in the current harness

This suggests controller support should remain out of scope for the main keyboard/mouse-driven switching logic.

## How To Use It

Run this from Windows, not WSL. WPF, Raw Input, and SetupAPI device enumeration are Windows-only.

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

## Suggested Manual Workflow

### Runtime Attribution Check

1. Start the prototype.
2. Press keys on one keyboard, then another.
3. Move or click one mouse, then another.
4. Compare the event log and confirm the runtime source changes with the physical device.

### Restart / Reconnect Identity Check

1. Start the prototype and select the target device row.
2. Copy the device snapshot.
3. Close and reopen the prototype, then compare the same device again.
4. Disconnect and reconnect the device, refresh the snapshot, and compare again.
5. If practical, move a receiver or USB connection to another port and compare once more.

Useful fields to compare:

- Raw Input handle
- raw device path
- normalized path
- instance ID
- candidate key / fingerprint
- VID/PID
- friendly name and related descriptive metadata

## Interpreting The Results

Use the current evidence conservatively:

- raw handles appear suitable for live session tracking
- persisted mappings should likely be based on stronger composite metadata
- reconnect or rebind behaviour may still be needed if the preferred persisted key changes

Avoid treating the current findings as universal until they have been tested on more hardware and connection types.
