# Track 10 Plan

1. Define meeting-record states and version rules in schema (done).
2. Define paper and minutes metadata in schema (done).
3. Define decision/action register views in schema (done).
4. Document the minutes state machine with allowed transitions (this track).
5. Document decision and action register patterns (this track).
6. Document document storage abstraction (Dataverse-first vs SharePoint path).
7. Document pack generation requirements for agenda packs, paper packs, and minutes packs.

## Dependencies

- Track 03 (Dataverse schema YAML foundation).
- Track 09 (governance body model for meeting-to-body linkage).

## Validation

- Smoke test an agenda, paper, minute, decision, action, and approval state transition via seed data.
- Verify that draft, approved, and published minutes can coexist for the same meeting.
- Verify decision and action records link back to meeting and agenda item.
