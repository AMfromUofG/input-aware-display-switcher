# Evaluation Plan

This document outlines the main dimensions that should be evaluated as the project moves from feasibility prototypes into an MVP and then into a more robust application.

The exact metrics and thresholds may be refined after the first feasibility prototypes produce evidence on what is realistically measurable.

## Evaluation Dimensions

| Dimension | Why It Matters | Initial Evaluation Approach |
| --- | --- | --- |
| Device detection reliability | The application is only useful if it can correctly attribute activity to the right physical device. | Measure whether repeated use of the same keyboards and mice produces consistent device-level identification across sessions and reconnects. |
| Display switching reliability | Successful switching is the core user-visible outcome. | Record whether the intended display profile is applied correctly across repeated trials and edge conditions. |
| Switching latency | Slow switching may make the system feel confusing or intrusive. | Measure elapsed time between relevant input activity and completed display change during prototype and MVP testing. |
| False-switch rate | Incorrect switching directly harms usability. | Track how often incidental or low-confidence activity triggers an unwanted display change. |
| Behaviour under reconnect or disconnect scenarios | Real-world use will include sleeping displays, HDMI reconnection, and device reconnects. | Test controlled scenarios involving monitor sleep, TV wake-up, unplug/replug events, and device re-enumeration. |
| Usability of setup and configuration | A technically correct tool can still fail if setup is too difficult. | Evaluate how easily a user can define devices, zones, and profiles without needing developer intervention. |
| Quality of diagnostics and explainability | Users need to understand why a switch happened or why it failed. | Assess whether logs and visible status information make switching decisions and failures understandable. |

## Planned Evidence Sources

- prototype observations and logs
- manual test runs on the target Windows hardware
- structured notes on edge cases and failures
- later MVP and GUI validation sessions

## Review Points

- Revisit this plan after the Raw Input prototype.
- Revisit this plan after the display-switching prototype.
- Refine metrics again once the MVP exists and real end-to-end behaviour can be measured.
