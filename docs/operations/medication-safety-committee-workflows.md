# Medication Safety Committee Workflows

## Source Rule

Real Medical Safety or Medication Safety Committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exported files. Gmail is not a valid source for these records. Sample data and validation evidence must remain synthetic and must not be copied from live NSW records.

The current agent session exposes Gmail but not a callable Outlook Email tool. Until NSW Outlook or SharePoint access is available, sample records must remain synthetic or redacted.

## Operating Model

Initial operating assumption:

- Chair: synthetic Medication Safety chair record.
- Secretariat: synthetic Medication Safety secretariat record.
- Committee: Medication Safety Committee, or Medical Safety Committee if that is the formal source name.
- System of record: Dataverse in Decisions Workspace.
- Collaboration surfaces: Teams for access, SharePoint for files, Planner for approved actions, Outlook for invites and attendee evidence.

Confirm the formal committee name, parent governance body, quorum, voting rights, terms of reference, membership list, and document repository before importing real records.

## Committee Setup Records

Create or confirm:

- Governance body.
- Committee record.
- Meeting series.
- Terms of reference with effective date and review date.
- Membership records for chair, secretariat, voting members, observers, and alternates.
- Access rules for chair, secretariat, members, observers, and administrators.
- Feature defaults for the meeting series.

Recommended initial feature defaults:

| Feature | Default | Reason |
| --- | --- | --- |
| Outlook invites | Enabled | Meeting invite and attendee evidence should be captured. |
| SharePoint papers and minutes | Enabled | Agenda, papers, minutes, and approved packs need document storage. |
| Transcript import | Enabled | Manual transcript or notes import supports draft minutes. |
| AI minutes | Disabled | Enable only after prompt governance and capacity are confirmed. |
| Planner sync | Disabled | Enable after a target Planner plan is confirmed. |
| Teams posts | Disabled | Enable after the Teams tab and channel notification rules are accepted. |
| Voting | Committee-specific | Enable if formal voting or motions are required. |
| Org hierarchy | Enabled | Supports reporting lines and escalation paths. |

## Meeting Workflow

Pre-meeting:

- Outlook invite sync creates or updates the meeting record.
- Attendees, apologies, required attendees, optional attendees, and organizer are recorded.
- Secretariat adds agenda items and decision sought text.
- Papers are registered with SharePoint links and classification.
- Conflicts are requested and recorded.
- Chair reviews agenda, papers, decision items, quorum, and unresolved prior actions.

During meeting:

- Attendance is confirmed.
- Quorum and conflicts are recorded.
- Agenda items are discussed.
- Decisions, deferrals, and action owners are captured.
- Transcript or notes are retained only if approved for the meeting.

Post-meeting:

- Secretariat drafts minutes manually or with AI Builder if enabled.
- AI-generated content is reviewed and edited before it becomes official.
- Chair approves minutes, decisions, and actions.
- Approved minutes are published to SharePoint if enabled.
- Teams summary is posted only if enabled.
- Planner tasks are created only for approved actions if enabled.
- Integration logs capture success, skipped features, and errors.

## AI Builder Workflow

AI Builder can be used for:

- Drafting agenda-structured minutes.
- Extracting candidate decisions.
- Extracting candidate actions.
- Flagging unresolved items and missing evidence.
- Preparing a review checklist for the secretariat and the chair.

AI Builder cannot approve or publish records. The human review gate remains mandatory.

The AI Builder Minutes Pattern architecture document ([ai-builder-minutes-pattern.md](../architecture/ai-builder-minutes-pattern.md)) defines the prompt contract, input classification, run logging, draft creation, human review gate, publication approval, prompt naming, versioning, and audit trail requirements.

The AI Human Review Gates document ([ai-human-review-gates.md](../architecture/ai-human-review-gates.md)) defines the complete gate model, required Dataverse evidence tables (`dft_AIBuilderPrompt`, `dft_AIBuilderRun`, `dft_Minutes`, `dft_Decision`, `dft_ActionItem`, `dft_IntegrationLog`), reviewer responsibilities, implementation controls, and acceptance criteria.

## Sample Records

Until real NSW-source documents are available, use synthetic records:

- Synthetic Medication Safety Committee meeting series.
- Synthetic agenda with medication incident review, formulary safety update, policy review, and action follow-up.
- Synthetic draft and approved minutes.
- Synthetic decision and action records.
- Synthetic attendees, chair, and secretariat records.
- Synthetic SharePoint links using example.invalid or placeholder paths.
- Synthetic integration log rows that capture import validation evidence and the real-data source gate.

The repo sample lives at `powerplatform/src/seed/medication-safety-sample-data.json`. It is intentionally marked as synthetic and includes placeholder attendees, agenda, papers, draft and approved minutes, decisions, actions, feature settings, access rules, integration bindings, AI Builder prompt and run records, and import-validation integration logs for the May 2026 Medication Safety Committee setup.

When real records become available, import only the minimum required fields and keep original documents in the approved NSW repository.

For local validation, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-MedicationSafetySampleToDataverse.ps1 -WhatIfOnly
```

That path validates the synthetic seed contract, prints the record counts, and does not attempt a Dataverse token exchange.

## Reporting

Useful operating dashboards:

- Open actions by owner and due date.
- Overdue medication safety actions.
- Decisions awaiting implementation or review.
- Meetings missing papers, minutes, attendees, or quorum status.
- AI runs awaiting human review.
- Terms of reference due for review.
- Integration failures by target system.

## Access And Privacy

- Use role-based access for chair, secretariat, members, observers, and administrators.
- Do not expose confidential papers or transcripts in Teams posts.
- Store sensitive documents in SharePoint with appropriate permissions.
- Keep Dataverse as the register and SharePoint as the document store.
- Record source, import reviewer, and approval status for any real committee record imported from NSW systems.
