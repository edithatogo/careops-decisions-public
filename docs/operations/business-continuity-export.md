# Business-Continuity Export Paths

## Principle

Dataverse is the system of record. External systems (SharePoint, Teams,
Planner, Outlook) hold copies but are not authoritative. Business-continuity
export must always capture Dataverse data first, then synchronised external
artifacts where available.

## Export Priorities

| Priority | Package | Contents | Format | Trigger |
| -------- | ------- | -------- | ------ | ------- |
| 1 | Meeting Pack | Meeting, Agenda Items, Papers (links or text), Minutes, Decisions, Actions | PDF and/or Dataverse export | End of minutes approval cycle |
| 2 | Decision Register | All decisions with status, authority, owner, effective date, review date | CSV or PDF | Monthly or per request |
| 3 | Action Register | All open and overdue actions with owner, due date, status, Planner task link | CSV | Weekly before chair review |
| 4 | Governance Register | Bodies, terms of reference, committee members, membership history | CSV or PDF | Quarterly |
| 5 | AI Audit Log | AI Builder runs, prompt versions, human review outcomes | CSV | Quarterly for audit |
| 6 | Integration Audit | IntegrationLog rows for failed or retried operations | CSV | On incident |

## Export Methods

### Dataverse Native Export (MVP)

1. Use Dataverse Advanced Find or Power Apps grid export to CSV for any
   table. Available to users with Read permission on the table.
2. Add the relevant view filter (meeting, body, date range) before export.
3. Attach the CSV to the meeting record as a note attachment for audit trail.

### Power Automate Export (Automated)

When flow access is available, schedule a flow to:

1. Query Dataverse records with a date-range filter.
2. Generate a CSV or use the Office 365 Outlook connector to email the
   export to the secretary.
3. Log the export in `dft_IntegrationLog` with operation="BusinessContinuityExport".

### Future Azure / Data Export

When Azure is available:

1. Use Azure Data Factory, Synapse Link, or Fabric shortcuts to export
   Dataverse data to a data lake.
2. Enable Dataverse change tracking for incremental export.
3. Schedule weekly full exports and daily incremental exports.

## Meeting Pack Assembly

For a single meeting, the secretary can assemble a meeting pack by:

1. Export meeting record and agenda items from Dataverse to a Word or PDF
   document (manual copy or future flow-generated).
2. Attach papers from SharePoint (download from the SharePoint URL stored
   in `dft_Paper.dft_sharepointurl`).
3. Export the minutes from Dataverse (use `dft_Minutes.dft_drafttext` or
   download from `dft_PublishedUrl`).
4. Export the decision register for the meeting from Dataverse.
5. Export the action register for the meeting from Dataverse.
6. Compile into a single PDF or ZIP file stored in SharePoint.

## Restoration Path

If Dataverse data is lost or corrupted:

1. Restore from the most recent Dataverse backup (Dataverse for Teams or
   full Dataverse provides point-in-time restore within the retention
   window).
2. Re-import exported CSV files for records created after the last backup.
3. Re-link external IDs (Outlook event, Planner task, SharePoint file) by
   running sync flows.
4. If external IDs were also lost, follow the Stale External IDs runbook
   to recreate links.

## Recovery Point Objective

| Data Class | RPO | Method |
| ---------- | --- | ------ |
| Meeting records (current month) | 24 hours | Dataverse autosave + daily export |
| Decision and action registers | 24 hours | Dataverse autosave + daily export |
| Archived meeting packs | Last export | Monthly backup export |
| Integration logs | 7 days | Dataverse autosave |

## Recovery Time Objective

| Scenario | RTO | Method |
| -------- | --- | ------ |
| Single record corruption | 1 hour | Dataverse point-in-time restore or manual re-entry |
| Table corruption | 4 hours | Dataverse backup restore + CSV re-import |
| Full environment loss | 24 hours | Solution re-import + seed data + CSV re-import |


## Selective SharePoint library columns — 19 October 2026

User-selected implementation direction: reuse library columns for artifact identity, provenance, filtering and routing where the output already belongs in SharePoint. Reuse existing columns/content types and exact internal names before adding fields. This extends the structured-document preference; it is not proof of automatic Word-body generation.

Priority: When implementing existing meeting-pack/continuity exports, retain authoritative Dataverse meeting/record IDs, source snapshot, document kind, reviewed revision and output revision on SharePoint copies. Dataverse remains authoritative; columns cannot confer approval or change decisions/actions.

Acceptance: map each property to its existing authoritative source and verify its actual type/internal name; read back the affected saved values and file identity. Update only changed values, guard modification-trigger loops and retries, and retain a recoverable preimage. Preserve human edits, original/issued history and exact reviewed revisions. Add no duplicate register, notifications, approval workflow, permission change, licence or runtime. Avoid putting private/internal-only values into properties that may travel inside a downloaded Office file.

Metadata readback and visible body rendering are separate checks. The Clinical trial stored a changed property while Word for the web still displayed the old body value. Do not claim scalar body refresh, narrative/repeating sections or generated Word/PDF fidelity from metadata success. Qualify those behaviours only for the selected document shape; retain working/manual outputs on a specific gap. Record one result in existing evidence, not another readiness pack.

Reference: https://learn.microsoft.com/sharepoint/dev/business-apps/power-automate/sharepoint-connector-actions-triggers
