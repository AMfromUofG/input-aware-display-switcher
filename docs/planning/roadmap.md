# Roadmap

The roadmap is intentionally phase-based so the project can respond to feasibility findings without pretending that all technical uncertainties are already resolved.

## Phase 1 — Feasibility

Goal: prove that the core technical approach is viable on the target Windows environment.

- Prototype Raw Input device detection.
- Prototype programmatic display switching.
- Record findings on device identity stability and edge cases.
- Refine architectural assumptions and evaluation criteria.

## Phase 2 — MVP

Goal: build the smallest useful end-to-end automatic switching workflow.

- Introduce a basic device registry.
- Introduce zone mapping.
- Implement a first-pass decision engine.
- Add initial cooldown or anti-thrashing logic.
- Demonstrate end-to-end automatic switching between zones and display profiles.

## Phase 3 — GUI + Persistence

Goal: make the application configurable and usable without developer intervention.

- Add a WPF configuration application.
- Persist devices, zones, profiles, and settings.
- Support editing of mappings and profiles.
- Surface useful diagnostics and current state.

## Phase 4 — Robustness / Smart Logic

Goal: improve reliability and reduce incorrect behaviour in realistic use.

- Add stronger prioritisation and conflict handling.
- Improve reconnect and recovery behaviour.
- Add manual lock or override controls.
- Explore smarter switching heuristics where justified by testing.

## Phase 5 — Polish

Goal: make the tool more stable, understandable, and ready for regular use.

- Improve packaging and installation.
- Improve diagnostics and status visibility.
- Improve startup behaviour and background usage patterns.
- Address usability issues identified during evaluation.

## Phase 6 — Stretch / Research

Goal: investigate advanced capabilities that are useful but not required for the core project outcome.

- Investigate lock screen or secure desktop support.
- Investigate a service companion if needed.
- Explore more advanced decision policies.
- Explore richer context-aware behaviour if time permits.
