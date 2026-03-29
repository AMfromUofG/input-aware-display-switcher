# Device Identity Feasibility

This document is the main research write-up for Issue #3:

> What identifiers and metadata can be used to persistently recognise physical input devices across reconnects, reboots, and receiver changes?

The notes below reflect what the current Raw Input prototype and manual testing have established so far. They do not claim that persistence is fully solved.

## Objective

Determine which identifiers exposed by the current prototype appear to be:

- temporary and useful only during a live session
- stronger candidates for persisted identity
- useful supporting metadata for reconciliation when a stronger identifier changes

The purpose is to inform a later Device Registry design, not to implement it here.

## Current Prototype Support

The current prototype under `prototypes/raw-input-test/RawInputPrototype` can now surface:

- Raw Input device handle
- Raw Input device path from `RIDI_DEVICENAME`
- device type
- `RID_DEVICE_INFO` details
- parsed VID/PID
- normalized path and selected path segments
- instance ID when resolvable through SetupAPI
- friendly name and other descriptive properties when available
- research-only candidate key and fingerprint-style summaries

This is enough to compare live-session identifiers against potentially more stable metadata.

## Observed Behaviour

### Runtime Identification

- Distinct keyboards and mice produced distinct event sources and raw handles during a running session on the current test setup.
- This included separate desk peripherals and a living-room handheld keyboard/trackpad setup.
- A shared wireless receiver still surfaced separate keyboard and pointing-device identities in the current harness.

### Reconnect And Restart Behaviour

- Unplugging and reconnecting a keyboard caused the raw handle to change.
- Restarting the prototype without changing the physical connection preserved the newly assigned handle.
- This suggests raw handles are useful for active runtime attribution, but should not be treated as durable identity keys.

### Metadata Value

- The richer metadata exposed by the prototype appears more promising for persistence and reconciliation than the raw handle alone.
- Useful fields currently include:
  - device type
  - VID/PID
  - raw device path
  - normalized path and instance-related path segments
  - instance ID when resolvable
  - friendly name when available
  - candidate key and fingerprint-style fields

## Temporary Vs More Stable Identifier Candidates

### Probably Temporary

- Raw Input device handle

Why:

- it changed after unplug/reconnect on the current test setup
- it is still valuable for identifying the active device during a running session
- it appears to be a useful-now identifier rather than a durable identity

### More Promising Candidates

- device instance ID when resolvable
- raw device path and normalized path
- instance-related path segments
- candidate key / fingerprint built from path- and instance-related metadata

These fields appear more appropriate for persisted identity work, but they still require further validation across more scenarios.

## Why Raw Handles Are Not Enough

Raw handles are useful because they tie incoming events to an active device at runtime. That makes them a good fit for live routing and monitoring while the application is running.

They are not enough for persistence because the observed reconnect test showed that a handle can change when the device is unplugged and reconnected. A later mapping system therefore cannot rely on the handle alone if it needs to recognise the same physical device across sessions or re-enumeration.

## Why VID/PID Alone Is Not Enough

VID/PID is useful supporting metadata, but it is too weak to serve as a sole persisted identity.

Reasons:

- it describes a model or interface family rather than a unique physical device
- two similar devices can share the same VID/PID
- it does not by itself explain reconnect, receiver, or port-change behaviour

VID/PID should therefore be treated as part of a composite match, not the whole answer.

## Manual Test Evidence Summary

The current conclusions are supported by the following manual observations:

- multiple keyboards and mice produced distinct runtime event sources and raw handles during the same session
- the handheld living-room keyboard and pointing device appeared as separate devices despite the shared overall setup
- unplugging and reconnecting a keyboard caused the raw handle to change
- restarting the prototype without changing the physical connection preserved the newly assigned handle
- richer path- and instance-related metadata was available for comparison in the updated snapshot tooling

## Preliminary Recommendations

The current evidence suggests the following direction for later work:

- use the raw handle for live session tracking only
- store a stronger composite persisted identity rather than the raw handle
- prefer path- and instance-related metadata as the basis of that composite identity
- keep VID/PID, device type, and friendly/descriptive metadata as supporting reconciliation fields
- keep fallback rebind or reconciliation behaviour in reserve for cases where the preferred persisted key changes

In practical terms, runtime routing and persisted identity should be treated as separate concerns.

## Edge Cases And Limitations

- The current findings are based on the current test setup and require further validation on additional hardware.
- A shared wireless receiver can still expose separate keyboard and pointing identities, which is promising, but broader receiver behaviour still needs more testing.
- Non-keyboard and non-mouse devices do not currently fit the model cleanly.
- During testing, an Xbox controller appeared with handle `0x0000000000000000` and unresolved metadata.
- Only the Xbox/Guide button appeared to surface in the current harness.
- This suggests controller support should remain outside the main switching scope for now.

## Open Questions

- How stable is instance-related metadata across reboot on the same machine?
- What changes when a receiver or wired device is moved to another USB port?
- How well does the same approach work for Bluetooth peripherals?
- Which fields should be considered mandatory versus fallback in a later composite identity?
- How much reconciliation logic will still be needed after reconnect or hardware path changes?
