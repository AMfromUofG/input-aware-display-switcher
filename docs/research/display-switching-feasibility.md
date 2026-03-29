# Display Switching Feasibility

This note captures the research framing for Issue #4:

> Can display profile changes be triggered programmatically and reliably on the target Windows setup?

## Objective

Determine whether a Windows-based prototype can:

- observe the current display topology in a useful way
- trigger practical display switching actions programmatically
- report success, failure, and recovery behaviour clearly enough for manual evaluation on the intended monitor + TV setup

## Candidate API Approach

The current preferred feasibility path is the Windows display configuration API set:

- `QueryDisplayConfig`
- `DisplayConfigGetDeviceInfo`
- `SetDisplayConfig`

Why this path was chosen:

- it is the supported Windows configuration API surface for display path and topology work
- it exposes active paths and mode information directly
- it can request common topologies such as internal, external, extend, and clone
- it gives concrete return codes for failure analysis

## Prototype Summary

The prototype under `prototypes/display-switch-test/DisplaySwitchPrototype` is a Windows-only WPF harness that:

- enumerates active display paths and mode entries
- shows a readable snapshot and active-path table
- exposes a few practical topology-switching actions
- validates a switch request before apply where practical
- captures the current active state for later restore attempts
- keeps a bounded diagnostics log with timestamps, API path labels, flags, and status codes

The prototype is intentionally limited to feasibility work. It is not the future application architecture.

## Findings So Far

On the current setup:

- querying and displaying the active topology worked usefully
- the prototype reflected manual `Win + P` changes correctly enough for research use
- the harness could identify single-active-path and clone / duplicate-like states
- the current simple `SetDisplayConfig` database-topology flag approach did not succeed reliably
- attempts such as internal / primary only and clone / duplicate returned `ERROR_INVALID_PARAMETER (87)` on the current parameter/flag path

This suggests:

- topology inspection is feasible on the current setup
- simple generic topology-flag switching is not yet a reliable basis for MVP on this hardware/software combination
- a more explicit path/mode-based strategy, or a different OS-supported switching route, may be required later

## Manual Test Plan

### Baseline Observation

1. Connect the desk monitor and TV in the starting state you want to test.
2. Launch the prototype.
3. Click `Refresh Current Display State`.
4. Record the active path count, target names, topology summary, and availability fields.

### Switching Trials

1. Click `Capture Current Snapshot`.
2. Attempt `Extend` and record whether validation succeeds before apply.
3. Attempt `Clone / Duplicate`.
4. Attempt `External / Secondary Only`.
5. If applicable, attempt `Internal / Primary Only`.
6. After at least one switch away from the baseline state, try `Restore Captured Snapshot`.

### Availability / Recovery Trials

Repeat selected actions while observing:

- TV powered on and already active
- TV powered on but previously inactive
- TV asleep or waking
- TV unavailable or disconnected if practical

## Evidence Log Template

Use a separate entry for each trial:

| Field | Notes |
| --- | --- |
| Date/time |  |
| Machine / Windows version |  |
| GPU / driver version |  |
| Displays connected |  |
| TV state | On / off / asleep / waking |
| Starting topology snapshot |  |
| Action attempted |  |
| API path used | Database topology flag call or supplied captured path/mode restore call |
| Validation flags |  |
| Apply flags |  |
| Status code |  |
| Interpreted status |  |
| Visible behaviour |  |
| Approximate latency |  |
| Resulting topology snapshot |  |
| Manual recovery needed? |  |
| Notes |  |

## Failure Cases To Observe

Record these carefully if they appear:

- API failure codes from `SetDisplayConfig`
- validation failure before apply, especially `ERROR_INVALID_PARAMETER (87)`
- apparent success with no real topology change
- long black-screen periods or slow recovery
- clone failure because Windows rejects the active mode combination
- restore failure after a target display disappears or changes state
- Windows choosing an unexpected display for the `External` topology
- target names or availability data changing across refreshes
- inactive-but-attached displays not appearing in the active-topology-oriented view

## Preliminary Recommendations

Treat the prototype as a measurement tool first.

The project should only rely on automatic display switching once the real monitor + TV setup shows:

- repeatable behaviour across several runs
- understandable failure modes
- a workable recovery path when the preferred target is unavailable

At this stage, the stronger recommendation is narrower:

- keep `QueryDisplayConfig`-based inspection as a useful research and diagnostic path
- do not assume the current generic database-topology `SetDisplayConfig` path is sufficient for MVP
- treat explicit path/mode restore or an alternative OS-supported switching route as the next thing to validate

## Open Questions

- Does the target setup switch cleanly enough using only Windows topology actions?
- Can a captured valid path/mode configuration be restored reliably enough to be useful?
- How should inactive-but-attached displays be handled when they are absent from the active topology view?
- Is a simpler OS-supported switching route more appropriate for MVP if direct topology manipulation remains fragile?
- Is a captured-path restore materially more reliable than the database topology actions?
- How sensitive is behaviour to GPU driver state, resolution, refresh rate, or TV power state?
- If multiple external displays are visible, can the system be steered predictably enough for the planned product?
