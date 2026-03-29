# Domain Model

This document defines the proposed core domain model for Input Aware Display Switcher.

The aim is to give later MVP implementation a clear, testable model for devices, zones, display profiles, rules, and switching decisions without pretending that all platform details are already solved.

The model is grounded in the current feasibility findings from Issues #2 to #4:

- runtime device attribution is feasible with Raw Input for keyboards and pointing devices on the tested setup
- runtime raw handles are useful during a session but are not strong enough to act as persisted identities
- shared receiver setups can still expose separate logical keyboard and pointing devices
- display topology inspection is feasible
- reliable programmatic switching is not yet proven with the current generic topology-flag approach

## Purpose

The domain model should:

- support later MVP implementation without committing too early to brittle Windows API shapes
- separate runtime concerns from persisted configuration concerns
- represent display changes as logical intent rather than as a fixed low-level switching payload
- make decision reasoning explicit enough for diagnostics, logging, and future UI explanations
- stay simple enough for a final-year project scope while remaining open to later expansion

## Design Principles

- Runtime identity is not the same as persisted identity.
- Core concepts should not depend directly on Raw Input structs, `HANDLE` values, or `SetDisplayConfig` payloads.
- Display profiles should describe desired behaviour, not assumed implementation details.
- Decision evidence and blocked reasons should be first-class outputs rather than inferred later from logs.
- The MVP model should be simple, but future weighted matching and richer switching policies should remain possible.

## Core Entities

### DeviceKind

Represents the kind of input device the system currently cares about.

Suggested values:

- `Keyboard`
- `Mouse`
- `PointingDevice`
- `Unknown`

`Mouse` and `PointingDevice` are both useful because current research indicates the project should focus on keyboard and pointing input, while shared handheld devices may expose a keyboard and trackpad as separate logical devices.

Controller-style devices remain out of scope for the main switching logic for now.

### RuntimeDeviceObservation

Represents a session-scoped observation of an active device source.

Purpose:

- route live input events during the current run
- track whether a currently observed device can be matched to a saved identity
- expose enough evidence for diagnostics without persisting fragile fields as primary keys

Suggested fields:

- `SessionDeviceId`
- `DeviceKind`
- `RawHandle`
- `RawDevicePath`
- `NormalizedDevicePath`
- `InstanceId`
- `VendorId`
- `ProductId`
- `FriendlyName`
- `ObservedAtUtc`
- `LastSeenAtUtc`
- `IsAvailableThisSession`

Notes:

- `RawHandle` is explicitly runtime-only.
- `RawDevicePath`, normalized path, instance ID, and descriptive metadata are useful evidence for matching, not guaranteed durable keys.

### PersistedDeviceIdentity

Represents a saved device record used for mappings and reconciliation across sessions.

Purpose:

- give the user a stable thing to name and assign to a zone
- store a composite persisted identity rather than a single fragile key
- support later matching and rebind logic when device metadata changes

Suggested fields:

- `DeviceId`
- `DisplayLabel`
- `DeviceKind`
- `PreferredPersistenceKey`
- `IdentityEvidence`
- `LastConfirmedAtUtc`
- `IsEnabled`

`IdentityEvidence` may contain:

- raw device path
- normalized path
- instance ID
- selected instance-related path segments
- vendor ID and product ID
- friendly name

The preferred persisted key should be treated as a chosen composite candidate, not as a claim that the persistence problem is fully solved.

### Zone

Represents a logical user-defined area such as Desk or Living Room.

Purpose:

- group one or more input devices under one meaningfully named context
- point to the preferred display outcome for that context
- provide precedence information when multiple zones could be considered active

Suggested fields:

- `ZoneId`
- `Name`
- `PreferredDisplayProfileId`
- `Priority`
- `IsEnabled`
- `Description`

For MVP simplicity, one persisted device should map to at most one zone at a time, while one zone can have many mapped devices.

### ZoneDeviceAssignment

Represents the mapping between a persisted device identity and a zone.

Suggested fields:

- `ZoneId`
- `DeviceId`
- `AssignmentKind`
- `IsPrimary`

This can stay lightweight in MVP, but keeping the relationship explicit makes it easier to extend later with device roles or weighted evidence.

### DisplayProfile

Represents a logical display intent rather than a low-level Windows configuration payload.

Suggested fields:

- `DisplayProfileId`
- `Name`
- `IntentKind`
- `Description`
- `ImplementationHints`
- `IsEnabled`

Example intent kinds:

- `DeskOnly`
- `LivingRoomOnly`
- `Extend`
- `Duplicate`
- `SafeRestore`

`ImplementationHints` are optional and non-authoritative. A future implementation may use them to prefer one execution strategy, but the profile should not embed `SetDisplayConfig` structures or assume the current prototype path is reliable.

### SwitchingPolicy

Represents configuration and policy values that govern when a switch is allowed.

Suggested fields:

- `Cooldown`
- `RecentActivityWindow`
- `ManualLockBehaviour`
- `TargetUnavailableBehaviour`
- `MinimumMatchConfidence`
- `AllowSameProfileRefresh`
- `RequireDisplayAvailabilityCheck`

This is configuration, not runtime state.

### ApplicationRuntimeState

Represents runtime context needed by the decision engine and later diagnostics.

Suggested fields:

- `CurrentZoneId`
- `CurrentIntendedDisplayProfileId`
- `LastSwitchAtUtc`
- `ManualOverrideState`
- `RecentActivitySummary`
- `LastKnownDisplayTopologySummary`
- `LastMatchedDeviceId`

This state should remain platform-neutral. It can refer to topology summaries and availability flags without depending on Windows interop types.

### SwitchCandidate

Represents a candidate inference produced from recent input evidence.

Suggested fields:

- `CandidateZoneId`
- `MatchedDeviceId`
- `ObservedSessionDeviceId`
- `Confidence`
- `Evidence`

This object is optional in implementation terms, but useful in the model because it separates evidence gathering from the final switching decision.

### SwitchDecision

Represents the result of evaluating a candidate against current policy and runtime state.

Suggested fields:

- `DecisionId`
- `ShouldSwitch`
- `CandidateZoneId`
- `TargetDisplayProfileId`
- `ReasonCodes`
- `EvaluatedAtUtc`

Example reason codes:

- `Allowed`
- `BlockedByCooldown`
- `BlockedByManualLock`
- `BlockedByDisabledZone`
- `BlockedByUnavailableDisplayTarget`
- `BlockedBecauseProfileAlreadyActive`
- `BlockedByLowConfidence`
- `NoMatchedPersistedDevice`

### SwitchExecutionResult

Represents what happened when the system attempted to apply the chosen logical display profile.

Suggested fields:

- `DecisionId`
- `WasAttempted`
- `Status`
- `ExecutionPath`
- `ErrorCode`
- `ErrorMessage`
- `RecordedAtUtc`

This keeps policy evaluation separate from execution success or failure. That distinction matters because a decision may be valid even when the platform execution path later fails.

## Relationships Between Entities

The main relationships are:

- one `Zone` has many `ZoneDeviceAssignment` entries
- one `ZoneDeviceAssignment` references one `PersistedDeviceIdentity`
- one `Zone` references one preferred `DisplayProfile`
- one `RuntimeDeviceObservation` may match zero or one `PersistedDeviceIdentity` in MVP
- the decision engine uses `SwitchingPolicy` plus `ApplicationRuntimeState` to evaluate a `SwitchCandidate`
- one `SwitchDecision` may lead to zero or one `SwitchExecutionResult`

Simple relationship summary:

```text
RuntimeDeviceObservation
    -> matched against PersistedDeviceIdentity
    -> resolved through ZoneDeviceAssignment
    -> Zone
    -> preferred DisplayProfile

SwitchingPolicy + ApplicationRuntimeState + SwitchCandidate
    -> SwitchDecision
    -> optional SwitchExecutionResult
```

## Runtime Identity Vs Persisted Identity

This separation is the most important design choice in the model.

Current research indicates that Raw Input device handles are strong enough for current-session event routing, but not strong enough to act as saved device identities across reconnects. The model therefore uses:

- `RuntimeDeviceObservation` for live, session-scoped evidence
- `PersistedDeviceIdentity` for saved mappings and later reconciliation

This allows the system to:

- react immediately to live input using the active raw handle
- store device mappings without claiming the handle is durable
- match runtime observations back to saved devices using composite metadata
- explain uncertain matches or missing mappings later in diagnostics

The matching process should be treated as reconciliation, not as simple equality on one field.

## Display Profile As Logical Intent

Current research indicates that display topology inspection is feasible, but reliable switching is not yet solved with the current generic topology-flag approach.

For that reason, `DisplayProfile` should describe a desired outcome such as:

- focus on the desk setup
- focus on the living-room setup
- extend across available displays
- duplicate where supported
- restore to a previously safe state

This keeps the domain model honest about current uncertainty:

- the core model can express what the user wants
- infrastructure can later decide how or whether that intent can be applied
- failures in one switching mechanism do not invalidate the model itself

A future implementation may translate a logical profile into:

- a validated path/mode restore
- a Windows-supported topology command
- a safer fallback or recovery action

That translation should remain an infrastructure concern.

## Rule And State Modelling

The design separates persisted policy from runtime state.

### Persisted Policy

`SwitchingPolicy` should hold stable configuration values such as:

- cooldown duration after a successful or attempted switch
- recent activity window used to interpret device activity
- minimum confidence threshold for automatic switching
- behaviour when a target display appears unavailable
- whether a manual lock blocks all automatic switching

`Zone` may also carry a simple `Priority` so the system can resolve conflicts without embedding all policy rules directly into the zone object.

### Runtime State

`ApplicationRuntimeState` should hold the moving parts of the current session such as:

- currently active zone
- currently intended display profile
- recent activity summary by device or zone
- manual lock or override state
- last switch timestamp
- last known display topology summary

This keeps the decision engine deterministic: it consumes policy plus current state and produces an explainable result.

## Decision And Outcome Modelling

The model treats decision-making as a first-class concern rather than hiding it inside imperative control flow.

Suggested flow:

1. Input activity produces a `RuntimeDeviceObservation`.
2. A matcher attempts to resolve that observation to a `PersistedDeviceIdentity`.
3. Zone mapping produces a `SwitchCandidate`.
4. The decision engine evaluates the candidate against `SwitchingPolicy` and `ApplicationRuntimeState`.
5. The result is recorded as a `SwitchDecision`.
6. If a switch is attempted, infrastructure records a `SwitchExecutionResult`.

This gives later UI and logging layers a way to answer questions such as:

- why did the application switch just now?
- why was the switch blocked?
- which device evidence was used?
- which zone and profile were inferred?
- did policy allow the change but execution fail?

That diagnostic value is important for a project like this because both device identity and display execution can be affected by environment-specific behaviour.

## Suggested Future Code Organisation

If lightweight shared domain code is later added, the following split is suggested:

```text
src/
  Core/
    Domain/
      Devices/
      Zones/
      DisplayProfiles/
      Policies/
      State/
      Decisions/
    Application/
      Matching/
      ZoneResolution/
      Switching/
  Infrastructure/
    Input/
    Display/
    Persistence/
    Logging/
```

The intent is:

- `Core/Domain` contains platform-neutral records, enums, and value objects
- `Core/Application` contains orchestration and decision services
- `Infrastructure` contains Raw Input, display APIs, JSON storage, and other Windows-specific adapters

No production code is required to prove the model at this stage.

## Alternatives Considered

### Flatter Config-Only Model

A flatter model could store devices, zones, and profiles as simple JSON records and let implementation logic infer everything else.

Pros:

- minimal upfront structure
- quick to wire into an MVP

Cons:

- runtime identity and persisted identity can blur together
- decision reasons become harder to preserve
- diagnostics become an afterthought

### Richer Domain Model With Explicit State And Decision Objects

This approach introduces explicit runtime state, candidate, decision, and execution result objects.

Pros:

- clearer reasoning and diagnostics
- better fit for testable switching logic
- cleaner separation of policy, state, and execution

Cons:

- slightly more structure than a basic config model

### Embedding All Rules Directly Into Zones

This would make each zone carry its own cooldowns, thresholds, and lock behaviour.

Pros:

- simpler object count
- easy to understand for very small systems

Cons:

- policy becomes duplicated or inconsistent across zones
- global behaviours such as manual lock become awkward
- later advanced logic becomes harder to reason about

### Representing Display Profiles As Low-Level Windows Configurations

This would store profile definitions as direct display path or topology payloads.

Pros:

- potentially closer to a future implementation path

Cons:

- overstates certainty that the execution route is already known
- couples the core model tightly to Windows API details
- makes the current design brittle while switching feasibility is still open

## Chosen Direction And Rationale

The chosen direction is a modestly rich domain model with:

- explicit separation between `RuntimeDeviceObservation` and `PersistedDeviceIdentity`
- explicit `Zone`, `DisplayProfile`, and `SwitchingPolicy` configuration objects
- explicit runtime state and decision/outcome objects for diagnostics and future UI explanation
- `DisplayProfile` represented as logical intent rather than a low-level Windows switching payload

This direction is the best fit for the current evidence because it:

- reflects what current research actually supports
- avoids locking the project to an unproven display-switching implementation
- keeps the future MVP testable and explainable
- remains small enough to implement without overengineering
