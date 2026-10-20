# Smoke Runbook

Use this runbook when you need a practical manual or semi-automated smoke pass on the Decisions workspace without writing to Power Platform.

## Purpose

The smoke runbook does three things:

1. Verifies the local smoke artifacts and seed file are present.
2. Summarizes the seeded governance data and the smoke scenarios.
3. Produces a run checklist you can follow during a manual test pass.

## Inputs

- Seed data: `powerplatform/src/seed/sample-governance-data.json`
- Scenarios: `tests/smoke/smoke-scenarios.md`
- Checklist helper: `scripts/Invoke-SmokeRunbook.ps1`

## Recommended Flow

1. Run the helper in dry-run mode.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-SmokeRunbook.ps1 -DryRun
```

2. Confirm the helper reports:

- the scenario list
- the seed summary
- any missing local files
- the run checklist

3. Use the checklist to conduct the manual pass in the target environment.

4. Record outcomes for each scenario in your test notes or issue tracker.

## Manual Checkpoints

- Confirm the seeded governance body, committee, terms of reference, members, meeting series, meeting, agenda items, decisions, and actions exist.
- Walk the smoke scenarios in order from `SMK-01` through `SMK-17`.
- Local smoke runs can skip those scenarios with a recorded reason when Teams, Planner, or SharePoint connectors are not enabled.
- Treat missing Teams, Planner, Office 365 Users, Graph 403 responses, and placeholder deployment settings as environment blockers, not app-core defects, unless the scenario explicitly depends on the live connector.
- Before using seed data, re-run the seed dry-run if `powerplatform/src/seed/sample-governance-data.json` changed since the last generated payload.
- For AI scenarios, confirm generated output remains draft until a human reviewer, review time, prompt/run evidence, and accept/edit/reject state are recorded.
- Mark scenarios as pass, fail, or skipped with a reason.
- Capture evidence for any behavioural issue, environment blocker, or connector gap.

## Exit Criteria

- All required local files are present.
- The seed file matches the expected smoke path.
- Every scenario has a recorded result.
- Connector, Graph, placeholder, import, stale seed, and AI human review blockers have an owner and a next step.
