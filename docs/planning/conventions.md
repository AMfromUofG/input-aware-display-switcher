# Conventions

These conventions are intentionally lightweight. They exist to keep the repository consistent while the project is still in early research and setup.

## Naming

- Use `PascalCase` for C# types, public members, and XAML view names.
- Use `camelCase` for local variables and method parameters.
- Use `_camelCase` for private fields when the codebase starts using fields.
- Use `kebab-case` for Markdown file names and documentation folders.
- Use clear domain terms such as `Zone`, `Profile`, `Device`, and `SwitchDecision` rather than vague abbreviations.

## Branch Naming

Use short, issue-oriented branch names.

Examples:

- `1-set-up-repository-structure-and-net-solution-skeleton`
- `12-prototype-raw-input-device-detection`
- `23-add-zone-mapping-core-models`

## Commit Style

Prefer Conventional Commit style so the history stays easy to scan.

Examples:

- `chore(repo): establish project structure and planning docs`
- `docs(architecture): describe planned module boundaries`
- `feat(core): add initial zone mapping model`
- `test(core): cover decision cooldown rules`

## Separation of Concerns

- Keep Windows API interactions isolated from core decision-making code.
- Keep prototype code out of future production folders unless it has been deliberately promoted.
- Keep GUI concerns separate from switching policy and infrastructure logic.
- Prefer small, focused changes over broad mixed-purpose commits.

## Repository Layout

- Put architecture, planning, evaluation, and research notes under `docs/`.
- Put early technical spikes under `prototypes/`.
- Put future production application code under `src/`.
- Put automated tests and test assets under `tests/`.

## Documentation Expectations

- Update documentation when a design decision, risk, or workflow changes materially.
- Record feasibility findings in `docs/research/feasibility-notes.md`.
- Add or update ADR-style notes in `docs/architecture/decisions.md` when important architectural choices are made.
