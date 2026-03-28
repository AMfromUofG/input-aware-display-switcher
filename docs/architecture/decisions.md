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

## ADR-004: Treat Raw Input as the leading candidate for device-specific input detection

- Status: Provisional
- Date: 2026-03-28
- Decision: Early feasibility work will prioritise Windows Raw Input for distinguishing physical keyboards and mice.
- Rationale: Raw Input appears to be the most promising Windows mechanism for receiving device-associated input events.
- Consequence: Prototype work should validate whether it provides stable enough device identity and event coverage for the target scenarios before it is adopted in production.

## ADR-005: Isolate Windows-specific API code from core logic

- Status: Accepted
- Date: 2026-03-28
- Decision: Platform-specific API calls should be isolated behind infrastructure components or adapters.
- Rationale: This keeps switching policy, state handling, and mapping logic easier to test and reason about.
- Consequence: Core modules should consume abstractions and domain models rather than direct Windows API calls.

## ADR-006: Treat feasibility evidence as a gate before production implementation

- Status: Accepted
- Date: 2026-03-28
- Decision: Feasibility prototypes and evaluation notes should be completed before significant production implementation begins.
- Rationale: The project includes several platform-level uncertainties that could materially affect scope and design.
- Consequence: Early repository work should prioritise prototypes, documentation, and decision records over premature application scaffolding.
