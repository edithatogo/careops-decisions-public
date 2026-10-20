# Workflow

## Conductor Operating Model

This repository uses Conductor-style tracks under `conductor/tracks`.

Each track should contain:

- `spec.md`: intent, scope, constraints, and acceptance criteria.
- `plan.md`: implementation steps, owners, dependencies, and validation.
- `metadata.json`: machine-readable status and ownership.

## Parallel Work Rules

- Run research, architecture, testing, and implementation tracks in parallel when they have disjoint write sets.
- Assign each subagent an explicit path ownership boundary.
- The main agent remains integrator and reviewer.
- Do not let multiple agents edit the same file unless ownership is deliberately reassigned.

## Validation Gates

- Repository layout validation: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmLayout.ps1`
- Power Platform solution validation: `pac solution pack`, `pac solution unpack`, and `pac solution check` when environment access permits.
- App testing: `pac test run` / Power Apps Test Engine when canvas app artifacts exist.
- Functional smoke tests: governance body, meeting, agenda, attendees, papers, minutes, decisions, actions, Planner sync, Teams posts, transcript ingestion, and integration logging.
