# Acceptance Evidence Template

Capture acceptance evidence for each smoke test run, deployment verification, or manual test pass. Store results under `artifacts/deployment/` using the naming convention below.

## File Naming Convention

```
evidence-{yyyyMMdd}-{profile}-{tester-initials}.{ext}
```

Examples:
- `evidence-20260510-development-dm.md`
- `evidence-20260510-development-dm.json`

## Evidence Block (per scenario or test)

```markdown
## {Test ID}: {Test Name}

- **Date:** {YYYY-MM-DD HH:mm}
- **Tester:** {Name}
- **Environment:** {URL or name}
- **Solution Version:** {X.X.X.X}
- **Result:** {PASS | FAIL | SKIP}
- **Skip reason (if SKIP):** {Why the test was skipped}

### Evidence
{Screenshot link, record URL, or command output excerpt}

### Notes
{Any observations, defects, or follow-up items}
```

## Evidence JSON Schema

For structured output that can be consumed by CI or reporting:

```json
{
  "evidence": {
    "timestamp": "2026-05-10T14:30:00Z",
    "environment": "development",
    "environmentUrl": "https://environment.example.invalid/",
    "solutionVersion": "1.0.0.0",
    "tester": "Developer Name",
    "profileName": "development"
  },
  "scenarios": [
    {
      "id": "SMK-01",
      "name": "Governance body setup",
      "result": "PASS",
      "skipReason": null,
      "evidenceLink": "https://example.invalid/screenshot.png",
      "notes": "Governance body created and visible in list view."
    }
  ],
  "summary": {
    "total": 17,
    "passed": 15,
    "failed": 0,
    "skipped": 2,
    "skipReasons": [
      "Planner connector not configured in target environment"
    ]
  }
}
```

## Minimum Required Metadata

Every evidence file (Markdown or JSON) must include:

| Field | Required | Description |
| --- | --- | --- |
| Timestamp | Yes | ISO 8601 date/time of the test run |
| Environment | Yes | Target environment name or URL |
| Solution version | Yes | Version string from `Solution.xml` |
| Tester | Yes | Person who ran the tests |
| Profile name | Yes | ALM profile used (local, development, test, enterprise) |
| Scenario results | Yes | Each scenario with PASS/FAIL/SKIP |

## CI Evidence Capture

When run in CI (GitHub Actions), the evidence JSON should be emitted as a workflow artifact:

```yaml
- name: Capture acceptance evidence
  shell: pwsh
  run: |
    $evidence = @{
      timestamp = (Get-Date -Format "o")
      environment = $env:POWER_PLATFORM_ENVIRONMENT_URL
      solutionVersion = "1.0.0.0"
      tester = "github-actions"
      profileName = "ci"
      summary = @{
        total = 0
        passed = 0
        failed = 0
        skipped = 0
      }
    }
    $evidence | ConvertTo-Json -Depth 10 | Out-File artifacts/deployment/evidence-ci.json
```

This block can be added to the validate.yml workflow after the solution checker step. The evidence artifact can then be uploaded alongside the solution artifact.

## Storage Rules

- Do not commit evidence files to the repository
- Store under `artifacts/deployment/` (gitignored)
- Retain per-environment (CI keeps evidence for 30 days in workflow artifacts)
- Evidence containing tenant URLs, connection IDs, or user tokens must not be shared outside the team
