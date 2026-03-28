# Raw Input Feasibility Log

This document is a focused evidence template for the Raw Input prototype. Fill it in after running the prototype on real hardware. Do not record assumptions as findings.

## Objective

Determine whether Windows Raw Input can reliably distinguish between multiple physical keyboards and mice connected to the same system.

## Prototype Location

- `prototypes/raw-input-test/RawInputPrototype`

## Implementation Summary

- WPF desktop window
- Raw Input registration for keyboards and mice
- `WM_INPUT` parsing for keyboard and mouse activity
- device snapshot populated from `GetRawInputDeviceList` and `GetRawInputDeviceInfo`
- live bounded event log showing timestamp, source handle, identifier, and event summary

## Environment Under Test

Fill this table in for each manual run.

| Date | Windows version | .NET SDK/runtime | Devices connected | Notes |
| --- | --- | --- | --- | --- |
| _pending_ | _pending_ | _pending_ | _pending_ | _pending_ |

## Manual Test Procedure

1. Launch the prototype from Windows.
2. Confirm the device snapshot shows the connected keyboards and mice you want to compare.
3. Press several keys on keyboard A, then keyboard B.
4. Move and click mouse A, then mouse B if available.
5. Compare the logged device handles and identifiers for each physical device.
6. Refresh the device snapshot if you reconnect hardware.
7. Optionally disconnect and reconnect one device to observe handle or identifier changes.

## Evidence Checklist

- screenshot of the device snapshot table
- copied event log excerpt showing at least one sequence from each physical device
- notes on exact hardware involved
- notes on whether identifiers stayed stable during the run
- notes on reconnect behavior if tested

## Findings

- Manual findings pending.

## Open Questions

- Are device handles stable across separate launches of the prototype?
- Do common wireless receivers expose distinct enough identifiers for multiple devices?
- Do integrated laptop devices behave differently from external USB devices?
- Is Raw Input alone sufficient, or will later mapping need extra metadata from other Windows APIs?
