# Architecture Overview

The application is split into modular components so that input monitoring, decision-making, switching, persistence, and GUI concerns remain separated.

## Modules

- Input Monitor
- Device Registry
- Zone Mapper
- Decision Engine
- State Manager
- Profile Manager
- Display Switcher
- Config Storage
- Logging Service
- GUI Application

## Design intent

This modular structure supports:
- easier testing
- clearer design justification
- safer future expansion
- cleaner dissertation documentation
