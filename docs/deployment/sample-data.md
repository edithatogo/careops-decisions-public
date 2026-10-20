# Sample Data

The MVP sample data lives at `powerplatform/src/seed/sample-governance-data.json`. It gives smoke tests and demos a repeatable governance graph without using real meeting or personal data.

## Contents

- One DMS governance body.
- One Executive Team committee.
- One current terms of reference record.
- Four members.
- Two org relationships.
- Eight feature definitions.
- Eight meeting-series feature defaults.
- Three meeting-level feature overrides.
- One meeting series.
- One scheduled meeting.
- Three agenda items.
- Two meeting papers.
- Two minutes records.
- Two decisions.
- Two actions.
- Three access rules.
- Six integration binding placeholders.

## Generate An Import Plan

The seed helper is non-destructive. It validates the sample JSON and writes a deterministic Dataverse upsert plan under `artifacts/seed/`. The plan also surfaces binding placeholders for Teams, Planner, SharePoint, and AI Builder so the local setup stays explicit without writing live connector data.

The feature switchboard data follows the documented inheritance model: repository and environment defaults are represented by the seed contract, meeting-series rows establish the baseline for recurring meetings, and the May 2026 meeting adds explicit overrides for AI minutes, Planner sync, and Teams posts.

The same sample-data package is also the place where feature definitions and meeting-level overrides are documented for the per-meeting feature switchboard, so disabled paths stay visible as explicit skip states during smoke review.

Dry run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -DryRun
```

Write the plan:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1
```

Default output:

```text
artifacts/seed/sample-governance-import-plan.json
```

Write the plan plus a Dataverse payload bundle:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -WritePayloads
```

Validate the generated import-plan and payload-bundle contract without live Dataverse writes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SeedImportPlan.ps1
```

Payload output:

```text
artifacts/seed/sample-governance-dataverse-payloads.json
```

## Import Policy

- Do not use real personal, patient, confidential, or production meeting data in this file.
- Keep sample email addresses under `example.invalid`.
- Use deterministic keys so repeated imports can upsert instead of duplicating data.
- Treat Planner, Teams, SharePoint, and AI Builder values as documented examples only.
- Treat the generated plan and payload bundle as the maker/API contract until a governed Dataverse data import path is approved.
- The payload bundle contains field payloads and lookup-resolution metadata only; it does not resolve Dataverse row IDs or write records.
