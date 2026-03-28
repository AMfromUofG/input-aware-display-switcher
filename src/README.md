# Source Layout

The `src/` directory is reserved for future production application code.

The likely long-term split is:

- `App` for the desktop application shell and UI integration
- `Core` for domain logic such as zone mapping, state handling, and switching decisions
- `Infrastructure` for Windows APIs, configuration storage, and logging

The exact project structure should be decided after the feasibility prototypes confirm the technical approach.
