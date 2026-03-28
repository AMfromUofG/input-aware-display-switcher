# Feasibility Notes

This file is the running log for early technical spikes. It should capture evidence, not just conclusions, so later design decisions can point back to what was actually observed.

## How to Use This Log

- Add dated notes after each prototype or spike.
- Record environment details when they matter.
- Prefer concrete observations over assumptions.
- Link to prototype code, screenshots, logs, or issue discussions where useful.

## Raw Input Feasibility

### Objective

Determine whether Windows Raw Input can reliably distinguish between multiple physical keyboards and mice connected to the same system, and whether the identifiers exposed are usable enough to support later mapping decisions.

### Implementation Summary

- Prototype project: `prototypes/raw-input-test/RawInputPrototype`
- Windows-only WPF harness
- Registers for raw keyboard and mouse input
- Logs `WM_INPUT` events with source device handle, parsed identifier, and event summary
- Enumerates current keyboard and mouse devices using Raw Input device APIs

### Questions

- Can Windows reliably distinguish between multiple physical keyboards?
- Can Windows reliably distinguish between multiple physical mice?
- Are the available device identifiers stable enough for mapping?

### Findings

- Prototype harness added; empirical findings are still pending manual testing on Windows hardware.
- No behaviour conclusions recorded yet.

### Manual Test Checklist

- Connect two keyboards and press keys on each in separate bursts.
- Connect two mice if available and move or click each in separate bursts.
- Compare the logged device handles and identifiers between the devices.
- Note whether identifiers remain stable while the app stays open.
- Optionally disconnect and reconnect one device to record whether the handle or identifier changes.

### Evidence To Record

- screenshot of the device snapshot table
- copied event log excerpt showing distinct devices
- Windows version and hardware notes
- reconnect observations if tested

## Display Switching Feasibility

### Questions

- Can the target display profiles be switched programmatically and repeatably?
- How does switching behave when a display is sleeping, disconnected, or waking up?
- Are there GPU driver or Windows version constraints that materially affect behaviour?

### Findings

- No findings recorded yet.

## Stable Device Identity Findings

### Questions

- Which identifiers remain stable across reconnects, reboots, or wireless receiver changes?
- Are there device classes where stability is materially worse?

### Findings

- No findings recorded yet.

## Open Questions

- What minimum confidence is required before an automatic switch should occur?
- What recovery behaviour is acceptable when the target display is unavailable?
- Which constraints should be treated as acceptable project scope boundaries rather than defects?
