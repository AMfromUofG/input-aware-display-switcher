# Feasibility Notes

This file is the running log for early technical spikes. It should capture evidence, not just conclusions, so later design decisions can point back to what was actually observed.

## How to Use This Log

- Add dated notes after each prototype or spike.
- Record environment details when they matter.
- Prefer concrete observations over assumptions.
- Link to prototype code, screenshots, logs, or issue discussions where useful.

## Raw Input Feasibility

### Questions

- Can Windows reliably distinguish between multiple physical keyboards?
- Can Windows reliably distinguish between multiple physical mice?
- Are the available device identifiers stable enough for mapping?

### Findings

- No findings recorded yet.

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
