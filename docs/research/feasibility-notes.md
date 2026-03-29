# Feasibility Notes

This file is the running log for early technical spikes. It should capture evidence, not just conclusions, so later design decisions can point back to what was actually observed.

## How to Use This Log

- Add dated notes after each prototype or spike.
- Record environment details when they matter.
- Prefer concrete observations over assumptions.
- Link to prototype code, screenshots, logs, or issue discussions where useful.

## Raw Input Runtime Feasibility

### Objective

Determine whether Windows Raw Input can reliably distinguish between multiple physical keyboards and mice connected to the same system, and whether runtime event attribution is strong enough to support later switching logic.

### Prototype Summary

- Prototype project: `prototypes/raw-input-test/RawInputPrototype`
- Windows-only WPF harness
- Registers for raw keyboard and mouse input
- Logs `WM_INPUT` events with source device handle and event summary
- Enumerates currently visible keyboard and mouse devices
- Supports richer identity inspection for later persistence analysis

### Observed Behaviour On The Current Test Setup

- Raw Input successfully distinguished multiple physical keyboards and mice during a running session.
- Distinct physical devices produced distinct event sources and raw handles at runtime.
- This included separate desk peripherals and a living-room handheld keyboard/trackpad setup.
- A shared wireless receiver still exposed separate keyboard and pointing-device activity in the current prototype.
- This suggests runtime attribution for keyboard and mouse activity is feasible on the tested setup.

### Key Evidence Gathered

- live event log entries showed distinct sources for different keyboards and mice during the same session
- snapshot rows showed separate devices for the handheld keyboard and its pointing component despite the shared overall setup
- reconnect testing showed device identity needed more analysis beyond the active raw handle

### Current Conclusion

Runtime device attribution appears feasible for keyboards and pointing devices on the current test setup. This reduces the early risk that all devices would collapse into one undifferentiated stream, but it does not by itself solve durable persistence.

## Device Identity / Persistence Feasibility

### Objective

Determine which identifiers and metadata can be used to recognise the same physical device across reconnects, restarts, and other re-enumeration events.

### Observed Behaviour On The Current Test Setup

- Raw input handles were useful for identifying the active source during a running session.
- Unplugging and reconnecting a keyboard caused the raw handle to change.
- Restarting the prototype without changing the physical connection preserved the newly assigned handle.
- This suggests the raw handle is useful for live-session attribution but is not a safe sole persisted identity.
- The prototype exposed more promising metadata for persistence and reconciliation:
  - device type
  - VID/PID
  - raw device path
  - normalized path and instance-related path segments
  - instance ID when resolvable
  - friendly name when available
  - candidate key and fingerprint-style summaries
- VID/PID alone was not strong enough to treat as a durable key.
- The observed behaviour suggests a composite identity strategy is the more realistic future direction.

### Key Evidence Gathered

- before/after reconnect comparison showed handle reassignment for a keyboard
- restart testing showed the post-reconnect handle remained stable across a simple application restart
- snapshot metadata exposed additional path- and instance-related fields that remained better persistence candidates than the raw handle
- the prototype could surface separate keyboard and pointing identities even for a shared wireless receiver setup

### Edge Cases / Scope Limits

- An Xbox controller appeared with handle `0x0000000000000000` and unresolved metadata in the current harness.
- Only the central Xbox/Guide button appeared to surface in the current prototype.
- This suggests controller-style devices do not fit the current keyboard/mouse model cleanly and should remain outside the main switching scope for now.

### Current Conclusion

The current evidence supports separating runtime routing from persisted identity:

- runtime routing can use active raw handles
- persisted mappings should likely use stronger composite metadata
- reconnect or port-change reconciliation behaviour may still be required

### Open Questions

- How stable is device instance ID across reboot on the same machine?
- What changes when a wireless receiver is moved to another USB port?
- How do Bluetooth devices behave compared with wired USB devices and shared receivers?
- Which fallback fields are strong enough to support safe rebinding when the preferred key changes?

## Display Switching Feasibility

### Objective

Determine whether the target Windows setup can switch between useful display states programmatically and repeatably enough for the planned project, with particular attention to the intended desk monitor + TV setup.

### Prototype Summary

- Prototype project: `prototypes/display-switch-test/DisplaySwitchPrototype`
- Windows-only WPF harness
- Reads active display paths and mode entries through `QueryDisplayConfig`
- Resolves source and target names where Windows exposes them through `DisplayConfigGetDeviceInfo`
- Attempts topology changes through `SetDisplayConfig`
- Validates a switching request before apply so the harness can distinguish invalid request shapes from apply-time failures
- Supports snapshot capture plus a supplied-configuration restore attempt using the captured active path and mode arrays
- Keeps a bounded diagnostics log for manual testing

### Observed Behaviour On The Current Test Setup

- Reading the active topology worked on the current setup.
- The prototype reflected manual `Win + P` changes usefully.
- The snapshot view could distinguish between a single active display path and clone / duplicate-like topology.
- The current generic `SetDisplayConfig` database-topology flag path did not succeed reliably on the tested setup.
- Attempts such as internal / primary only and clone / duplicate returned `ERROR_INVALID_PARAMETER (87)` with the current parameter/flag path.
- This suggests the current simple topology-flag strategy is not valid enough on the tested setup to treat as a reliable switching mechanism.
- The current harness primarily reasons about displays that are already present in the active topology view, which leaves inactive-but-attached display handling unresolved.

### What To Test Manually

- refresh the current display state and record the active path summary
- attempt extend between the desk monitor and TV
- attempt clone / duplicate and note whether the current mode combination is accepted
- attempt external / secondary only and record which display remains active
- if the setup includes an internal panel, attempt internal / primary only
- capture a snapshot, switch away from it, then attempt restore and note whether validation succeeds before apply
- repeat tests with the TV on, off, asleep, or waking if practical

### Evidence To Record

- timestamp and test environment details
- physical display state before the action
- action attempted
- API path used
- validation flags and apply flags
- return code and readable status message from the diagnostics log
- before/after topology snapshot
- latency, flicker, black-screen periods, or recovery quirks
- whether manual intervention was needed

### Findings

- Query/display inspection through the Windows display configuration APIs worked usefully on the current setup.
- The prototype correctly reflected manual `Win + P` changes and identified active topology states.
- The current generic `SetDisplayConfig` topology-flag switching path did not succeed reliably on the tested setup.
- `ERROR_INVALID_PARAMETER (87)` suggests that the current parameter/flag combination is not valid for the attempted database-topology call path on this setup.
- This makes the issue partially successful: inspection worked, but reliable programmatic switching is not yet proven with the current approach.

### Open Questions

- Can the intended monitor + TV setup switch reliably enough using database topology actions alone?
- Can a captured valid path/mode configuration be restored reliably?
- How should inactive-but-attached displays be handled if they are not already represented in the active topology view?
- Is a simpler OS-supported switching route more appropriate for MVP if direct topology manipulation remains fragile?
- When Windows has multiple external displays available, does `External` choose the desired target consistently enough for the project?
- What failure modes appear when the TV is powered off, asleep, slow to wake, or missing?
- Are there driver- or GPU-specific behaviours that materially affect feasibility?

## Project-Level Open Questions

- What minimum confidence is required before an automatic switch should occur?
- What recovery behaviour is acceptable when the target display is unavailable?
- Which constraints should be treated as acceptable project scope boundaries rather than defects?
