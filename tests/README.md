# Testing Strategy

The `tests/` directory contains automated coverage for the new MVP switching slice.

Current focus:

- unit tests for device registry resolution and zone mapping
- unit tests for decision engine v1 rule outcomes
- orchestration-boundary tests to prove blocked decisions do not execute and allowed decisions do
- JSON persistence round-trip testing for the registry store

Still intentionally out of scope for automation:

- end-to-end Raw Input hardware attribution
- real Windows display switching success across monitor/TV hardware combinations

Those behaviours remain prototype/manual-validation concerns and should continue to be recorded in `docs/research/feasibility-notes.md`.
