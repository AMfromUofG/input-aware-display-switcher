# Input Aware Display Switcher

Input Aware Display Switcher is a planned Windows desktop utility that changes display output automatically based on which physical input device is active.

The project is intended to serve both as a practical desktop tool and as a final-year software engineering/dissertation project, so the repository is structured to support prototyping, evaluation, and disciplined iteration from the start.

## Problem Statement

Windows display switching is often manual and awkward for setups that span more than one physical location, such as a desk monitor in one room and a TV in another. The intended application will monitor device activity, associate devices with logical zones, and switch to the most appropriate display profile without requiring repeated manual intervention.

Typical target scenario:

- Desk keyboard and mouse should favor a desk monitor profile.
- Living room keyboard and mouse should favor a TV profile.
- The system should avoid unnecessary switching, recover sensibly from edge cases, and provide enough diagnostics to understand why a switch happened.

## Current Status

The repository is currently in foundation and feasibility mode. The immediate focus is to:

- validate whether Windows can reliably distinguish physical keyboards and mice
- validate whether display profiles can be switched programmatically on the target hardware
- document architecture, risks, conventions, and evaluation criteria before implementation begins

No application logic, GUI implementation, or production switching pipeline is being claimed as complete at this stage.

## Planned Architecture Summary

The intended design is modular so that Windows-specific APIs, decision-making logic, persistence, and user interface concerns can evolve independently.

Planned modules:

- Input Monitor
- Device Registry
- Zone Mapper
- Decision Engine
- State Manager
- Profile Manager
- Display Switcher
- Config Storage
- Logging / Telemetry
- GUI Application

Further detail lives in [docs/architecture/overview.md](docs/architecture/overview.md), [docs/architecture/modules.md](docs/architecture/modules.md), and [docs/architecture/decisions.md](docs/architecture/decisions.md).

## Roadmap Summary

The current plan is split into six phases:

1. Feasibility
2. MVP automatic switching
3. GUI and persistence
4. Robustness and smart logic
5. Polish
6. Stretch and research

The full roadmap and milestone breakdown are documented in [docs/planning/roadmap.md](docs/planning/roadmap.md) and [docs/planning/milestones.md](docs/planning/milestones.md).

## Repository Structure

```text
.
|-- .github/
|-- docs/
|   |-- architecture/
|   |-- planning/
|   |-- evaluation/
|   `-- research/
|-- prototypes/
|   |-- raw-input-test/
|   `-- display-switch-test/
|-- src/
`-- tests/
```

High-level intent:

- `docs/` holds architecture, planning, evaluation, and research material.
- `prototypes/` holds scoped feasibility spikes before production code is committed.
- `src/` will later contain the production application and supporting libraries.
- `tests/` will hold automated tests and supporting test assets where appropriate.

## Technology Direction

- Language: C#
- Runtime/platform: .NET
- Desktop UI: WPF
- Configuration: JSON
- Diagnostics: structured logging
- Operating system: Windows only

Some of these choices are still documented as provisional where feasibility evidence is still needed. See [docs/architecture/decisions.md](docs/architecture/decisions.md).

## Development Focus

Near-term work should stay focused on:

- feasibility prototypes
- documentation and design clarification
- evaluation planning
- repository hygiene and workflow discipline

Implementation of Raw Input handling, display switching logic, GUI screens, and the decision engine belongs to later issues once the feasibility work has produced evidence.
