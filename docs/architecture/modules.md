# Module Catalogue

This document records the planned modules for the application, their intended responsibilities, and the boundaries we expect to maintain as implementation begins.

| Module | Primary Responsibility | Expected Inputs | Expected Outputs | Likely Layer | Notes on Dependencies |
| --- | --- | --- | --- | --- | --- |
| Input Monitor | Observe device-specific keyboard and pointing activity from Windows APIs and expose runtime observations. | Raw input events, device metadata from Windows. | `RuntimeDeviceObservation` data and normalised activity events. | Infrastructure | Depends on Windows-specific APIs and should stay isolated from decision logic. |
| Device Registry | Maintain persisted device identities and user-facing metadata for mapping and diagnostics. | Composite identity evidence, user-assigned names, persisted config data. | `PersistedDeviceIdentity` records. | Core / Infrastructure boundary | Reads and writes persisted device data via configuration storage. Raw handles must not become saved primary keys. |
| Device Matcher | Reconcile runtime observations with persisted device identities. | `RuntimeDeviceObservation`, persisted device records. | Match results, confidence, unmatched-device diagnostics. | Core | This module makes the runtime-vs-persisted identity split explicit and should remain platform-neutral. |
| Zone Mapper | Associate matched persisted devices with logical zones such as Desk or Living Room. | Match results, stored zone mappings. | Zone lookup results and candidate zone information. | Core | Should not depend directly on Windows APIs. One zone may contain many mapped devices. |
| Decision Engine | Decide whether a display switch should occur based on current activity, state, and policy. | Candidate zone, runtime state, switching policy, display availability summary. | `SwitchDecision` objects and reason codes. | Core | Depends on zone mapping and state information, but not on UI or platform API details. |
| State Manager | Track runtime context such as active zone, current intended profile, recent events, cooldowns, and locks. | Activity events, switching outcomes, override actions, topology summaries. | `ApplicationRuntimeState`. | Core | Supports the decision engine and diagnostics. |
| Profile Manager | Store and retrieve logical display profile intents. | User-defined profiles, persisted configuration. | `DisplayProfile` definitions. | Core / Infrastructure boundary | Profiles should remain logical intents rather than low-level Windows switching payloads. |
| Display Switcher | Attempt to apply a chosen logical display profile through Windows display-management mechanisms. | Chosen `DisplayProfile`, switching command context, current topology information. | `SwitchExecutionResult` with success, failure, and execution-path diagnostics. | Infrastructure | Encapsulates Windows-specific display switching APIs or command wrappers. The execution path remains an implementation concern rather than part of the core profile model. |
| Config Storage | Persist devices, zones, profiles, policies, and application settings. | Domain configuration objects. | Saved and loaded configuration data. | Infrastructure | JSON is the initial persistence format; storage details should not leak into core logic. |
| Logging / Telemetry | Record operational events, warnings, failures, and switch explanations. | Structured events from all major modules. | Logs and diagnostic traces. | Cross-cutting infrastructure | Should support both developer diagnostics and future user-facing troubleshooting. |
| GUI Application | Provide setup, configuration, status, and diagnostics views. | User actions, configuration data, runtime state. | Commands, updated configuration, visible system state. | Presentation | WPF is the leading UI direction; the GUI should depend on application services rather than low-level platform code. |

## Boundary Notes

- Core logic should remain testable without Windows hooks or display APIs.
- Windows-specific concerns should be pushed into infrastructure adapters.
- Runtime device observations and persisted device identities should remain distinct domain concepts.
- Decision outputs should preserve reasons and evidence rather than only returning a boolean switch command.
- The GUI should orchestrate and present information, not own switching policy.
- Feasibility prototypes may temporarily cut across layers, but production code should not.
