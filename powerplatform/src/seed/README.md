# Seed Data

Seed data is kept source-first so smoke tests and demo imports can be repeated across developer environments.

The checked-in `sample-governance-data.json` currently seeds:

- Three governance bodies.
- Four committees and working groups, including a nested high-risk medicines group.
- Six terms-of-reference records covering current, superseded, and archived states.
- Thirteen committee members across chair, deputy chair, secretariat, member, alternate, and observer roles.
- Two cached org relationships.
- Eight feature definitions for voting, AI minutes, transcript import, Planner sync, Teams posts, SharePoint papers/minutes, Outlook invites, and org hierarchy.
- Eight meeting-series defaults for the Executive Team monthly series.
- Three explicit meeting overrides for the May 2026 meeting.
- One meeting series.
- One meeting.
- Three agenda items.
- Two meeting papers.
- Three minutes records covering draft, approved, and published versions.
- Seven attendee records covering accepted attendees, an apology, proxy attendance, a non-voting observer, and an external guest.
- One manual transcript record.
- One approved AI Builder prompt and one completed AI Builder run pending human review.
- Two decisions.
- Two actions.
- Three access rules.
- Six integration binding placeholders for Teams, Planner, SharePoint, and AI Builder.

The checked-in `medication-safety-sample-data.json` seeds a synthetic Medication Safety Committee setup:

- Medication Safety governance body and Medication Safety Committee.
- Draft terms of reference.
- Chair, Renee as secretariat, pharmacy, nursing, quality, and medical member placeholders.
- Monthly meeting series and a planned 20 May 2026 meeting.
- Agenda items for attendance, prior actions, medication incident trends, high-risk medicines, medication reconciliation, and policy/formulary updates.
- Placeholder papers, draft minutes, decisions, actions, invited attendees, and a transcript placeholder.
- Feature defaults and meeting overrides that keep AI minutes, Planner sync, and Teams posts disabled until the real NSW sources and target systems are confirmed.

## Fixture Packs

Three fixture packs provide repeatable seed datasets at different scales:

- **Small** (`fixture-small.json`): single committee, one meeting, minimal membership — use for smoke testing the app.
- **Medium** (`fixture-medium.json`): multi-level governance hierarchy with parent bodies, child committees, subcommittees, working groups, and multiple meetings — use for full integration testing.
- **Edge-case** (`fixture-edge.json`): retired bodies, cancelled meetings, superseded decisions, overdue actions, expired memberships, proxy attendees — use for boundary testing.

See `docs/operations/fixture-packs.md` for full record counts and documentation for each pack.

Use `scripts/Import-SeedData.ps1` to upsert the sample graph or any fixture pack into Dataverse. The helper uses stable natural keys where they exist, and deterministic seed keys where the sample JSON does not expose a natural alternate key.

For the current repo-first MVP, the helper writes a deterministic import plan rather than performing live Dataverse writes. This keeps demo setup repeatable before service accounts, connection ownership, and data-import governance are available.

Use `-WritePayloads` to also emit a Dataverse payload bundle with field payloads and lookup-resolution metadata. The helper still does not resolve row IDs or write live records.

The feature switchboard records follow the inheritance chain described in `docs/architecture/per-meeting-feature-switchboard.md`: feature definitions first, then meeting-series defaults, then meeting-level overrides.

The binding placeholders in the sample file are documentation-friendly examples only. They are not secrets, and they are not written as live connector bindings.

The seed set also stays aligned with the per-meeting feature switchboard design: feature definitions and meeting-level overrides are documented in the repo artifacts, while disabled features remain explicit skip paths instead of hidden fallbacks.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -WritePayloads
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/medication-safety-sample-data.json -OutputPath artifacts/seed/medication-safety-import-plan.json -PayloadPath artifacts/seed/medication-safety-dataverse-payloads.json -WritePayloads
```

The generated plan path is `artifacts/seed/sample-governance-import-plan.json`.
The generated payload bundle path is `artifacts/seed/sample-governance-dataverse-payloads.json`.

Do not include real personal, patient, confidential, or production meeting data.
