# Architecture Decisions

This file acts as a lightweight ADR log for early project decisions. Entries may remain provisional until feasibility work is complete.

## ADR-001: Use C# and .NET for the application

- Status: Accepted
- Date: 2026-03-28
- Decision: The project will be implemented in C# on .NET.
- Rationale: The application is Windows-only, needs good access to desktop APIs, and should be practical to maintain within a dissertation-style project scope.
- Consequence: The repository and conventions should favour standard .NET tooling and development workflows.

## ADR-002: Use WPF as the preferred desktop UI framework

- Status: Accepted
- Date: 2026-03-28
- Decision: WPF is the preferred UI framework for the desktop client.
- Rationale: WPF is mature, well-supported for Windows desktop applications, and suitable for a configuration-heavy utility.
- Consequence: Future UI work should assume XAML-based desktop views unless later evidence justifies revisiting the choice.

## ADR-003: Use JSON for configuration persistence

- Status: Accepted
- Date: 2026-03-28
- Decision: Configuration data will be stored in JSON.
- Rationale: JSON is simple to inspect, easy to version during development, and sufficient for the expected configuration complexity.
- Consequence: Configuration schemas should be kept explicit and versioning should be considered once persistence is implemented.

## ADR-004: Treat Raw Input as the leading candidate for runtime device-specific input detection

- Status: Accepted
- Date: 2026-03-29
- Decision: Windows Raw Input is the leading approach for runtime keyboard and mouse source attribution.
- Rationale: Prototype work on the current test setup showed that Raw Input can distinguish multiple physical keyboards and mice at runtime, including a shared-receiver handheld keyboard and pointing-device setup.
- Consequence: Production design can continue on the basis that runtime routing should start from Raw Input for keyboards and pointing devices, while still allowing for further hardware validation.

## ADR-005: Treat runtime routing and persisted device identity as separate concerns

- Status: Accepted
- Date: 2026-03-29
- Decision: Runtime device attribution and persisted device identity should not be treated as the same problem.
- Rationale: Prototype testing showed that raw handles are useful during a running session but can change after unplug/reconnect, which makes them unsuitable as a sole persisted identity.
- Consequence: Later architecture should use active raw handles for live routing and a different persisted identity strategy for saved device mappings.

## ADR-006: Persisted device identity should use composite metadata rather than raw handles alone

- Status: Accepted
- Date: 2026-03-29
- Decision: Future persisted device identity should be based on stronger composite metadata, not raw handles alone.
- Rationale: Current findings suggest that path- and instance-related metadata are more promising persistence candidates, while VID/PID alone is too weak and raw handles are not durable enough.
- Consequence: The future Device Registry should likely store a preferred key plus supporting reconciliation metadata rather than assuming one universally stable field.

## ADR-007: Isolate Windows-specific API code from core logic

- Status: Accepted
- Date: 2026-03-28
- Decision: Platform-specific API calls should be isolated behind infrastructure components or adapters.
- Rationale: This keeps switching policy, state handling, and mapping logic easier to test and reason about.
- Consequence: Core modules should consume abstractions and domain models rather than direct Windows API calls.

## ADR-008: Keep controller-style devices out of scope for the main switching logic unless later evidence changes that decision

- Status: Provisional
- Date: 2026-03-29
- Decision: The main switching logic should remain focused on keyboards and pointing devices for now.
- Rationale: Current prototype behaviour did not surface controller-style devices cleanly enough to treat them as part of the same runtime and persistence model.
- Consequence: Architecture and planning should not assume controller support until later evidence justifies expanding scope.

## ADR-009: Treat feasibility evidence as a gate before production implementation

- Status: Accepted
- Date: 2026-03-28
- Decision: Feasibility prototypes and evaluation notes should be completed before significant production implementation begins.
- Rationale: The project includes several platform-level uncertainties that could materially affect scope and design.
- Consequence: Early repository work should prioritise prototypes, documentation, and decision records over premature application scaffolding.
