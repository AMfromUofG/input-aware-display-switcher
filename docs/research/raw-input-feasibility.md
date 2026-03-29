# Raw Input Feasibility

This document captures the main findings from the Raw Input prototype work for Issue #2:

> Can Windows Raw Input distinguish between multiple physical keyboards and mice well enough to support later zone-based switching logic?

## Objective

Determine whether Windows Raw Input can attribute keyboard and mouse activity to the correct physical device at runtime on the target setup.

## Prototype Summary

The prototype under `prototypes/raw-input-test/RawInputPrototype`:

- registers for raw keyboard and mouse input
- listens for `WM_INPUT` and device-change messages
- logs the device source associated with each event
- shows a current device snapshot for manual comparison
- now includes richer identity-analysis metadata for the related persistence investigation

## Runtime Device Separation Findings

On the current test setup:

- Raw Input successfully distinguished multiple physical keyboards and mice at runtime.
- Distinct physical devices produced distinct event sources and raw handles during a running session.
- This included separate desk peripherals and a living-room handheld keyboard/trackpad setup.
- A shared wireless receiver still exposed separate keyboard and pointing-device identities in the current harness.

These findings suggest Raw Input is a viable basis for runtime keyboard and mouse source attribution on the tested setup.

## Evidence Summary

The useful evidence gathered so far is:

- live event log output showing different devices producing different active event sources during the same run
- device snapshot output showing separate rows for the devices under test
- successful separation of the handheld keyboard and its pointing device despite the shared overall setup

## Limits Of The Finding

This is a runtime-attribution finding, not a persistence finding.

It does not show that:

- raw handles are durable across reconnects
- all Windows hardware classes will behave the same way
- non-keyboard and non-mouse devices fit the same model

Those questions belong to later identity and scope decisions.

## Conclusion

Runtime device attribution using Windows Raw Input appears feasible for keyboards and pointing devices on the current test setup. This supports continuing with Raw Input as the leading runtime-input approach, while keeping persistence and edge-case behaviour as separate follow-on concerns.
