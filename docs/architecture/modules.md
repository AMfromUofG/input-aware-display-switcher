# Module Catalogue

This document records the planned modules for the application, their intended responsibilities, and the boundaries we expect to maintain as implementation begins.

| Module | Primary Responsibility | Expected Inputs | Expected Outputs | Likely Layer | Notes on Dependencies |
| --- | --- | --- | --- | --- | --- |
| Input Monitor | Observe device-specific keyboard and mouse activity from Windows APIs. | Raw input events, device metadata from Windows. | Normalised input activity events. | Infrastructure | Depends on Windows-specific APIs and should stay isolated from decision logic. |
| Device Registry | Maintain known devices and their metadata for user mapping and diagnostics. | Discovered device identifiers, user-assigned names, device metadata. | Registered device records. | Core / Infrastructure boundary | Reads and writes persisted device data via configuration storage. |
| Zone Mapper | Associate devices with logical zones such as Desk or Living Room. | Device identifiers, stored zone mappings. | Zone lookup results for active devices. | Core | Should not depend directly on Windows APIs. |
| Decision Engine | Decide whether a display switch should occur based on current activity and rules. | Activity events, active zone, cooldown state, policy configuration. | Switching decisions and reasons. | Core | Depends on zone mapping and state information, but not on UI or platform API details. |
| State Manager | Track runtime context such as active zone, recent events, cooldowns, and locks. | Activity events, switching outcomes, override actions. | Current runtime state. | Core | Supports the decision engine and diagnostics. |
| Profile Manager | Store and retrieve logical display profiles and switching targets. | User-defined profiles, persisted configuration. | Resolved profile definitions. | Core / Infrastructure boundary | Should remain independent of the low-level switching mechanism. |
| Display Switcher | Execute a display profile change using Windows display management mechanisms. | Chosen display profile, switching command context. | Success or failure result with diagnostics. | Infrastructure | Encapsulates Windows-specific display switching APIs or command wrappers. |
| Config Storage | Persist devices, zones, profiles, and application settings. | Domain configuration objects. | Saved and loaded configuration data. | Infrastructure | JSON is the initial persistence format; storage details should not leak into core logic. |
| Logging / Telemetry | Record operational events, warnings, failures, and switch explanations. | Structured events from all major modules. | Logs and diagnostic traces. | Cross-cutting infrastructure | Should support both developer diagnostics and future user-facing troubleshooting. |
| GUI Application | Provide setup, configuration, status, and diagnostics views. | User actions, configuration data, runtime state. | Commands, updated configuration, visible system state. | Presentation | WPF is the leading UI direction; the GUI should depend on application services rather than low-level platform code. |

## Boundary Notes

- Core logic should remain testable without Windows hooks or display APIs.
- Windows-specific concerns should be pushed into infrastructure adapters.
- The GUI should orchestrate and present information, not own switching policy.
- Feasibility prototypes may temporarily cut across layers, but production code should not.
