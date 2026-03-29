# Display Switching Prototype

This folder contains the Windows-only feasibility harness for Issue #4:

- prototype programmatic display profile switching

The runnable project lives at `prototypes/display-switch-test/DisplaySwitchPrototype`.

## Purpose

This is research tooling, not the application.

It is intended to answer one narrow question:

- can the target Windows setup switch between useful display states programmatically and reliably enough for the planned project?

It does not implement:

- input-device awareness
- zone mapping
- decision logic
- tray behaviour
- config persistence
- the main product UI

## API Approach

The prototype focuses on Windows display configuration APIs:

- `QueryDisplayConfig`
- `DisplayConfigGetDeviceInfo`
- `SetDisplayConfig`

The current harness uses them to:

- enumerate active display paths and mode entries
- resolve source and target names where Windows exposes them
- show a readable snapshot of the current active topology
- attempt a few practical topology switches using Windows database topology flags
- validate a switching call before apply so the harness can distinguish invalid requests from apply-time failures
- capture the active path/mode arrays so the tester can attempt a supplied-configuration restore

## What The Prototype Shows

The current prototype provides:

- a current display snapshot text view
- a clear note that the snapshot is based on `QueryDisplayConfig(OnlyActivePaths)` and therefore reflects the active topology rather than a complete inventory of inactive-but-attached displays
- an active-path table with source, target, output technology, mode details, availability, and identifiers
- switching actions for:
  - refresh current state
  - capture current snapshot
  - restore captured snapshot
  - internal / primary only
  - external / secondary only
  - extend
  - clone / duplicate
- a bounded diagnostics log with timestamps, action names, status codes, and readable error interpretations

## How To Run It

Run this from Windows, not WSL. WPF and the display configuration APIs are Windows-only.

1. Install the .NET 8 SDK on Windows if it is not already installed.
2. Open Windows Terminal, PowerShell, or a Developer Command Prompt.
3. Change into the project directory:

```powershell
cd .\prototypes\display-switch-test\DisplaySwitchPrototype
```

4. Launch the prototype:

```powershell
dotnet run
```

You can also open `DisplaySwitchPrototype.csproj` in Visual Studio 2022 and run it there.

## Current Findings On The Tested Setup

The current prototype has already established that:

- display inspection through the Windows display configuration APIs worked on the current setup
- the snapshot view reflected manual `Win + P` changes usefully
- the prototype could distinguish between a single active display path and clone / duplicate-like topology
- the current generic `SetDisplayConfig` database-topology flag approach did not succeed reliably on the tested setup
- attempts such as internal / primary only and clone / duplicate returned `ERROR_INVALID_PARAMETER (87)` on the current parameter/flag path
- this suggests the simple topology-flag approach is not sufficient yet for reliable switching on the current hardware/software combination

That makes this issue partially successful:

- topology inspection worked
- programmatic switching is not yet proven with the current call strategy

## Suggested Manual Test Workflow

1. Start with the desk monitor and TV connected in the state you want to test first.
2. Click `Refresh Current Display State` and note the active paths, target names, and topology summary.
3. Click `Capture Current Snapshot` before trying any disruptive changes.
4. Try `Attempt Extend` and observe:
   - whether the topology changes as expected
   - how long it takes
   - whether validation succeeds before apply
   - whether the UI stays usable after the change
5. Try `Attempt Clone / Duplicate` and record whether Windows accepts it on the tested resolutions and refresh rates.
6. Try `Attempt External / Secondary Only` and note which display Windows chooses to keep active.
7. If the setup includes an internal panel, try `Attempt Internal / Primary Only`. If not, record the failure behaviour instead.
8. Use `Restore Captured Snapshot` to see whether the previously captured path/mode state validates and can be re-applied cleanly.
9. Repeat some actions with the TV powered on, asleep, waking, or unavailable if practical.

When reading the diagnostics log, pay attention to:

- whether the action used the `Database topology flag call` or the `Supplied captured path/mode restore call`
- the validation flags vs apply flags
- whether failure happened during validation or during apply
- whether the post-action snapshot changed even when Windows returned success

## What Counts As Success Or Failure

Useful success evidence includes:

- the API call returns success
- the post-action snapshot reflects the expected topology change
- the monitor or TV behaves consistently across repeated trials
- the prototype can recover to a prior useful state after testing

Useful failure evidence includes:

- validation rejects the request before apply
- `SetDisplayConfig` returns an error code during apply
- the API returns success but the observed topology does not match expectations
- `ERROR_INVALID_PARAMETER (87)` on the database-topology path
- the target display is missing, asleep, or reported unavailable
- clone or extend fails on a specific mode combination
- recovery is slow, partial, or requires manual Windows intervention

## Evidence To Record

For each manual test run, capture:

- date and time
- Windows version and GPU/driver details if relevant
- physical setup state, including whether the TV is on, off, or asleep
- action attempted
- API path used
- validation flags and apply flags
- diagnostics log status code and message
- before/after snapshot summary
- visible behaviour, latency, or recovery quirks

Screenshots of the prototype window before and after a switch attempt are useful.

## Limitations

This prototype deliberately keeps scope narrow:

- it primarily reasons about the active topology returned by `QueryDisplayConfig(OnlyActivePaths)`
- it relies on Windows topology switching semantics rather than any future app-specific profile system
- `Internal / Primary Only` may be unsupported or unhelpful on a desktop-only setup with no internal panel
- `External / Secondary Only` reflects Windows topology behaviour, not a custom choice of a specific external display
- the current generic database-topology switching path is not yet reliable on the tested setup
- restore uses the captured active path/mode arrays and may still fail if the hardware state has changed since capture
- reliable switching may require a more explicit path/mode-based strategy or a different OS-supported switching route later
