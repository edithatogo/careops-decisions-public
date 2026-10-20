# Smoke Runbook

This page describes the local smoke runbook for the Decisions workspace. It is built for manual or semi-automated verification only and does not perform live Power Platform writes without writing to Power Platform.

## What It Uses

- `powerplatform/src/seed/sample-governance-data.json`
- `tests/smoke/smoke-scenarios.md`
- `tests/smoke/seed-data-checklist.md`
- `scripts/Invoke-SmokeRunbook.ps1`

## How To Run It

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-SmokeRunbook.ps1 -DryRun
```

The helper reads the seed JSON, extracts the smoke scenario list, verifies the expected local files, and prints a run checklist. Use the output as the starting point for a manual pass in the target environment.
Record outcomes for each scenario in your notes or issue tracker.

## Run Order

1. Confirm the local files exist.
2. Review the seed summary.
3. Review the smoke scenarios.
4. Execute the scenarios in order.
5. Record results, evidence, and blockers.

Local smoke runs can skip those scenarios with a recorded reason when Teams, Planner, or SharePoint connectors are not enabled.

## Evidence To Capture

- Environment name
- Solution version
- Tester
- Date and time
- Scenario result
- Screenshot or record link when useful
- Notes for any skip or failure

## CI Wrapper Strategy

The smoke runbook can be invoked through the GitHub Actions CI workflow as a manual or post-deployment validation step. The CI wrapper does the following:

1. **Repo validation first** — `Invoke-RepoValidation.ps1 -RequireImplementationScaffolds` runs on every PR and push. This covers file presence, YAML structure, and solution bootstrap checks.
2. **Solution pack** — `New-ReleaseArtifact.ps1 -Unmanaged` packs the solution and uploads the artifact. No live environment is required.
3. **Solution checker** — Runs on non-PR events when `SOLUTION_CHECKER_ENABLED=true`. This is the closest CI gets to a live Power Platform operation without deploying.
4. **Evidence capture** — CI emits a structured evidence JSON file as a workflow artifact. The evidence records scenario results, environment metadata, and skip reasons for connector-dependent tests.
5. **Connector-gated tests** — `requires-live-connector` tests are skipped in PR validation with the skip reason surfaced in the evidence output. Full validation runs (scheduled or dispatch) can run all tests when credentials exist.

Extend the existing `.github/workflows/validate.yml` with an optional smoke-runbook step:

```yaml
- name: Run smoke runbook (dry-run)
  if: github.event_name == 'workflow_dispatch'
  shell: pwsh
  run: ./scripts/Invoke-SmokeRunbook.ps1 -DryRun -ActivationValidation -ReviewGateValidation -ReportingValidation
```

This step is dispatched manually and does not run on PR or push to keep CI fast. The output appears in the workflow run log.

## Notes

This runbook stays repo-local on purpose. If a scenario depends on Planner, Teams, SharePoint, AI Builder, or another connector that is not available, record the gap instead of trying to force a write path.
