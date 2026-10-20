# Schema Implementation Notes

## Goal

These schema files define a source-controlled Dataverse scaffold for the Decisions Workspace governance meeting system. They are intended to be readable, diff-friendly, and usable before any live Dataverse environment exists.

## File Roles

- `powerplatform/src/schema/dataverse-tables.yaml`: table catalog, primary fields, important columns, and table-level MVP/future notes.
- `powerplatform/src/schema/relationships.yaml`: lookup relationships and cascade intent.
- `powerplatform/src/schema/choices.yaml`: reusable choice sets for status, classification, roles, and lifecycle values.
- `docs/architecture/schema-implementation-notes.md`: implementation guidance and release boundary notes.

## Design Rules

- Use the `dft` publisher prefix consistently.
- Keep one governance body table for the MVP rather than splitting boards, committees, councils, and working groups into separate tables.
- Store stable IDs, not display names, for joins where the source system provides one.
- Keep Dataverse as the authoritative record for meetings, minutes, decisions, actions, and logs, even when Outlook, Teams, Planner, or SharePoint hold mirrored data.
- Use choice sets for lifecycle and classification values instead of hard-coding literals in app logic.

## MVP Shape

- `dft_GovernanceBody` covers the governance hierarchy.
- `dft_TermsOfReference` holds versioned mandate and quorum rules.
- `dft_CommitteeMembership`, `dft_OrgPerson`, and `dft_OrgRelationship` support identity and reporting cache data.
- `dft_MeetingSeries`, `dft_Meeting`, `dft_Attendee`, `dft_AgendaItem`, `dft_Paper`, `dft_Minutes`, `dft_Transcript`, `dft_Decision`, `dft_ActionItem`, `dft_Vote`, `dft_VoteResponse`, `dft_ConflictOfInterest`, `dft_AccessRule`, `dft_AIBuilderPrompt`, `dft_AIBuilderRun`, and `dft_IntegrationLog` cover the meeting lifecycle and audit trail.
- SharePoint, Planner, Teams, Outlook, and AI Builder are integration surfaces, not the source of truth.

## Future Expansion

- Replace connector-only identity caching with Graph or HR-driven sync when the environment supports it.
- Add richer security mapping when the solution moves to full Dataverse.
- Expand votes, AI Builder telemetry, and access rules only if the governance process needs more complexity.
- Keep the logical names and choice codes stable so the schema can grow without forcing user-facing renames.

## Implementation Order

1. Create or import the tables in dependency order: people and governance first, then meetings, then downstream records.
2. Add the choice sets before building app forms so the UX and schema share the same values.
3. Apply the lookup relationships.
4. Add environment variables and connection references later, once a real solution export exists.
5. Validate that every generated flow and screen uses the same logical names and status values as the schema.

## Notes On Migration

- The MVP should work in Dataverse for Teams.
- The same table model should carry forward to standard Dataverse without changing the domain language.
- Security and reporting should become stricter in the enterprise path, but the table names and relationships should not need to change.
