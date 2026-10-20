# Test Strategy

This repository uses lightweight, repo-first validation before tenant-specific Power Platform testing. The aim is to keep ALM, solution structure, and core governance workflows testable even when a live environment is unavailable.

## Test Layers

| Layer | Purpose | Command or artifact | Required for |
| --- | --- | --- | --- |
| Layout validation | Confirm expected repository folders and handoff files exist. | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmLayout.ps1` | Every PR or Conductor integration pass. |
| Solution pack/unpack | Prove source-controlled solution files can round-trip through Power Platform CLI. | `pac solution pack --folder powerplatform/solutions/DecisionsWorkspace --zipfile artifacts/solutions/DecisionsWorkspace.zip` and `pac solution unpack --zipfile artifacts/solutions/DecisionsWorkspace.zip --folder artifacts/roundtrip/DecisionsWorkspace --packagetype Unmanaged` | Before import/export work is considered stable. |
| Static solution checks | Catch Power Platform issues where environment access permits. | `pac solution check --path artifacts/solutions/DecisionsWorkspace.zip` | Before managed release candidates. |
| App tests | Exercise canvas-app behavior using `pac test run` and Power Apps Test Engine once app test files exist. | `pac test run --test-plan-file tests/powerapps/DecisionsWorkspace.testplan.yaml` | Before user acceptance testing. |
| Smoke tests | Validate end-to-end governance meeting flows. | `tests/smoke/smoke-scenarios.md` | Every deployment to a shared dev/test environment. |

## Test-AlmLayout.ps1 Expectations

`scripts\Test-AlmLayout.ps1` should fail fast when required repo surfaces are missing. Expected checks:

- Documentation: `README.md`, `product.md`, `tech-stack.md`, `workflow.md`, `docs/source-map.md`, `docs/tooling-inventory.md`, and `docs/testing/test-strategy.md`.
- Conductor handoff: `conductor/README.md`, `conductor/status.md`, and track folders when implementation tracks are created.
- Power Platform ALM: `powerplatform/solutions/DecisionsWorkspace`, `powerplatform/settings/deployment-settings.sample.json`, and an artifacts folder or documented output path for packed solutions.
- Tests: `tests/smoke/smoke-scenarios.md` and, once app tests exist, `tests/powerapps`.
- The script should report missing paths with actionable messages and return a non-zero exit code on failure.

## Power Platform CLI Workflow

Use `pac auth list` and `pac auth select` before commands that require a tenant. Keep solution source under `powerplatform/solutions/DecisionsWorkspace`.

Pack:

```powershell
pac solution pack `
  --folder powerplatform/solutions/DecisionsWorkspace `
  --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --packagetype Unmanaged
```

Unpack round-trip check:

```powershell
pac solution unpack `
  --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --folder artifacts/roundtrip/DecisionsWorkspace `
  --packagetype Unmanaged
```

Import with settings:

```powershell
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --settings-file powerplatform/settings/deployment-settings.sample.json `
  --publish-changes
```

## Power Apps Test Engine Skeleton

Create app tests under `tests/powerapps` when the canvas app is available:

```text
tests/powerapps/
  DecisionsWorkspace.testplan.yaml
  suites/
    governance-body.yaml
    meeting-agenda.yaml
    minutes-decisions-actions.yaml
    integrations.yaml
```

Initial automated coverage should mirror the smoke path: create governance body, create meeting, build agenda, add attendees and papers, capture minutes, record decisions/actions, and verify integration log records. Keep connector-heavy cases tagged so they can be skipped when Planner, Teams, SharePoint, or AI Builder are not configured.

## Manual Smoke Coverage

Manual smoke testing remains the release gate until app test files are stable. Cover governance bodies, meetings, agendas, attendees, papers, minutes, decisions, actions, Planner sync, Teams posts, transcript-to-minutes with manual input and AI Builder, and integration logs.

## Evidence

For each test run, keep a short run note with environment, solution version, tester, timestamp, command output location, scenario results, known defects, and screenshots or record links where useful. Do not commit tenant secrets, user tokens, transcripts containing real sensitive data, or exported production data.
