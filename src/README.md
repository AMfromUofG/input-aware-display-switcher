# Source Layout

The `src/` directory now contains the first production-ready MVP slice for the automatic switching pipeline.

Current projects:

- `InputAwareDisplaySwitcher.Core` for platform-neutral domain and application logic
- `InputAwareDisplaySwitcher.Infrastructure` for JSON persistence and Windows display switching adapters

The current split follows the architecture docs directly:

- `Core/Domain` models runtime device observations, persisted devices, zones, display profiles, policy, decisions, and execution outcomes
- `Core/Application` contains the device registry service, decision engine v1, and orchestration path from observation to attempted switch
- `Infrastructure/Configuration` contains JSON-backed registry persistence
- `Infrastructure/Windows` contains the initial Windows display switcher implementation behind a core abstraction

No WPF shell has been added yet. The current production code focuses on the reusable core + infrastructure boundaries needed for Issues #6, #7, and #8.
