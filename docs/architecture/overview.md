# Architecture Overview

Input Aware Display Switcher is planned as a layered Windows desktop application with a strong separation between platform-specific integrations and the core switching logic.

At a high level, the system will:

1. Observe device activity from Windows.
2. Resolve the active device to a logical zone.
3. Decide whether a display change should occur.
4. Apply the chosen display profile.
5. Record enough state and diagnostics to explain the outcome.

## Planned Modules

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

## High-Level Responsibilities

- The infrastructure side of the application will handle Windows integrations such as raw input collection, display switching, persistence, and logging.
- The core side of the application will manage device-to-zone mapping, runtime state, and switching decisions.
- The GUI application will provide configuration, visibility, and control without embedding low-level platform logic.

## Architectural Intent

The main architectural goals are:

- keep Windows-specific API code isolated
- preserve a testable core for mapping and decision logic
- make feasibility findings easy to fold into the design
- support clear technical justification for a final-year project report

More detailed module boundaries are documented in [modules.md](modules.md), and early design choices are tracked in [decisions.md](decisions.md).
