# Test Tagging Strategy

This document defines the test tagging convention used across Power Apps Test Engine suites, smoke scenarios, and CI validation. Tags determine whether a test can run locally or requires a live connector.

## Tag Taxonomy

| Tag | Meaning | Run Locally | Run in CI | Run with All Connectors |
| --- | --- | --- | --- | --- |
| `local` | Dataverse-first build, no live connectors required | Yes | Yes | Yes |
| `requires-live-connector` | Needs Teams, Planner, SharePoint, AI Builder, or Graph | Skipped (with reason) | Skipped (with reason) | Yes |
| `requires-live-dylan` | Needs the Dylan environment specifically | No | Skipped | Yes |

## Where Tags Are Applied

### YAML Test Suites

Each test suite and individual test in `tests/powerapps/suites/` carries a `readiness` field:

```yaml
readiness: local  # or requires-live-dylan
```

Individual tests can override:

```yaml
- id: INT-03
  title: optional planner sync
  readiness: requires-live-connector
```

### Smoke Scenarios

Smoke scenarios in `tests/smoke/smoke-scenarios.md` are tagged by convention:

| Scenario | Tag | Reason |
| --- | --- | --- |
| SMK-01 through SMK-08 | `local` | Core governance lifecycle |
| SMK-09 | `requires-live-connector` | Needs Planner |
| SMK-10 | `requires-live-connector` | Needs Teams |
| SMK-11 | `local` | Manual input, no AI Builder |
| SMK-12 | `requires-live-connector` | Needs AI Builder |
| SMK-13 | `local` (simulated) / `requires-live-connector` (live) | Integration log patterns |
| SMK-14 | `local` | Human review gate, simulated AI output |
| SMK-15 | `local` | Dataverse views, seed data |
| SMK-16 | `local` | Data-quality checks |
| SMK-17 | `local` | Feature switchboard |

## How Skipping Works

### Local Runs

When running locally, the runbook or test harness checks the tag:

```powershell
if ($test.readiness -eq "requires-live-connector") {
    Write-Warning "SKIP: $($test.id) - $($test.name) (requires-live-connector)"
    $summary.skipped++
    continue
}
```

Skip reasons are recorded in the evidence output.

### CI Runs

CI runs have two modes:

1. **PR validation:** Only `local` tests execute. `requires-live-connector` tests are skipped with a CI-appropriate reason.
2. **Full validation (scheduled or dispatch):** All tests execute if all connectors are available. Runs using `SOLUTION_CHECKER_ENABLED=true` can also run connector-dependent tests if credentials exist.

## Adding a New Connector Tag

When a new connector-dependent test is added:

1. Add the tag to the test metadata in the YAML suite or smoke scenario table.
2. Add the connector name to the skip reason for clarity.
3. Update the connector dependency matrix in `docs/testing/test-strategy.md`.
4. If the test requires a new connector category, add it to the tag taxonomy above.

## Verifying Tag Coverage

Run the following to confirm all test suites have consistent tagging:

```powershell
$suites = Get-ChildItem tests/powerapps/suites/*.yaml
foreach ($suite in $suites) {
    $content = Get-Content $suite.FullName -Raw
    if ($content -notmatch 'readiness:\s*(local|requires-live-dylan|requires-live-connector)') {
        Write-Warning "$($suite.Name) has no readiness tag"
    }
}
```

## Connector Dependency Matrix

| Test Suite | Connector Dependencies | Tag |
| --- | --- | --- |
| governance-body | None | `local` |
| meeting-agenda | None | `local` |
| minutes-decisions-actions | None | `local` |
| integrations | Planner, Teams, SharePoint, AI Builder, Outlook | `requires-live-connector` |
| feature-switchboard | None | `local` |
| app-activation | None | `local` |
| flow-activation | None (contract check) | `local` |
| import-validation | None | `local` |
| configuration-wizard | None | `local` |
| review-gate | None | `local` |
| reporting | None | `local` |
