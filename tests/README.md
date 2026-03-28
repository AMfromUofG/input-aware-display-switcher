# Testing Strategy

The `tests/` directory is reserved for future automated tests and supporting test assets.

Planned testing approach:

- unit tests for mapping, policy, and decision logic
- integration-style tests where practical for persistence and orchestration boundaries
- manual validation for Windows-specific hardware behaviour that cannot be reliably automated in early stages

Hardware-dependent behaviour should also be documented alongside prototype findings in `docs/research/feasibility-notes.md`.
