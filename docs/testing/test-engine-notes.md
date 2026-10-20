# Power Apps Test Engine Notes

This repository keeps the Power Apps test surface as a skeleton until the
canvas app and control tree are available in the target environment.

## Current Shape

- `tests/powerapps/DecisionsWorkspace.testplan.yaml` is the root plan.
- `tests/powerapps/suites/*.yaml` hold one scenario family each.
- The selectors in these files are placeholders that describe the intended
  control surface, not a verified live tree.

## What This Does Not Claim

- It does not claim the app can run today.
- It does not claim the selectors are bound to a published app.
- It does not claim connector-backed paths are configured.

## Validation

Use `scripts/Test-JsonYaml.ps1` to check JSON syntax and basic YAML presence
and structure. The script is dependency-free and only relies on built-in
PowerShell commands.

## Intended Test Coverage

The skeleton mirrors the Track 22 activation and smoke path:

- governance body creation and edit flow (GB-01, GB-02)
- meeting creation and agenda sequencing (MA-01, MA-02, MA-03)
- minutes, decisions, and actions capture (MDA-01, MDA-02, MDA-03)
- integration status and log surfaces (INT-01, INT-02, INT-03)
- feature switchboard definitions, overrides, and disabled states (FS-01 through FS-04)
- app activation smoke: load, navigation, screen visibility (ACT-01 through ACT-04)
- flow contract activation: triggers, env vars, connector refs (FLO-01 through FLO-04)
- seed data import validation: counts, relationships, idempotency (IMP-01 through IMP-06)
- configuration wizard: field presence, env-var mapping, persistence (CFG-01 through CFG-03)
- human review gate: draft-to-review, approve/reject, publish block (RVW-01 through RVW-05)
- reporting and metrics: dashboards, data-quality, switchboard visibility (RPT-01 through RPT-05)

## Readiness Categories

Each test case carries a `readiness` annotation:

- **local** — passes against the Dataverse-first build without live connectors
- **requires-live-dylan** — needs the live Dylan environment with Teams, Planner, SharePoint, or Graph

The testplan and suites default to `local` readiness. Tests marked `requires-live-dylan` should be skipped when the Dylan environment is not available, with the skip reason recorded.
