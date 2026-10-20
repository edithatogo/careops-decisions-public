# Smoke Scenarios

These scenarios validate the repo-first Power Platform MVP after solution import and before wider user testing. Record the environment, solution version, tester, date, and evidence links for each run.

## Prerequisites

- The managed or unmanaged solution imports without missing dependencies.
- Deployment settings resolve environment variables and connection references for Dataverse, Teams, Planner, SharePoint, Office 365 Users, Outlook, and AI Builder where enabled.
- Test users include a committee owner, chair, secretary, member, governance auditor, optional guest attendee, and integration/service account.
- A Teams channel, Planner plan, and SharePoint document location are available only for connector-backed scenarios. Local smoke runs can skip those scenarios with a recorded reason.

The core smoke lifecycle is: create governance body, schedule meeting, add agenda and papers, generate minutes draft, approve minutes, record decisions, record actions, and optionally sync Planner.

## Core Scenarios

| ID | Scenario | Steps | Expected result |
| --- | --- | --- | --- |
| SMK-01 | Governance body setup | Create a governance body with name, purpose, reporting line, terms of reference link, owner, secretary, and active status. | Body saves, appears in list/search, audit fields populate, and related meeting creation is available. |
| SMK-02 | Membership and attendees | Add members with roles, voting rights, start/end dates, and invite an external or non-voting attendee where supported. | Members and attendees are visible on the body and meeting; inactive members do not appear as default attendees. |
| SMK-03 | Meeting creation | Create a meeting for the governance body with date/time, location or Teams meeting link, chair, secretary, and quorum target. | Meeting is linked to the body, has the correct default attendees, and status starts as draft or scheduled. |
| SMK-04 | Agenda build | Add agenda items with owner, sequence, time allocation, purpose, and decision/action flags. Reorder one item. | Agenda sequence persists, totals are sensible, and decision/action prompts are available for flagged items. |
| SMK-05 | Papers | Attach or link papers to agenda items from the configured document location. Mark one paper confidential. | Papers are linked to the right agenda item, confidential flag is retained, and missing paper metadata is surfaced. |
| SMK-06 | Minutes capture and approval | During or after the meeting, record attendance, apologies, conflicts, discussion notes, draft minutes, and chair approval. | Minutes draft is linked to meeting and agenda items; attendance, conflicts, and approval state are retained for review. |
| SMK-07 | Decisions | Record a decision with decision text, owner/body, effective date, status, and source agenda item. | Decision receives a unique identifier, is searchable, and is traceable back to meeting, minutes, and agenda item. |
| SMK-08 | Actions | Create actions from agenda items and decisions with owner, due date, priority, status, and follow-up meeting. | Actions display on the meeting and owner views; overdue/due-soon states calculate correctly. |
| SMK-09 | Planner sync | Sync one action to Planner, update title/due date/status, then refresh from Planner if supported. If Planner is not enabled, record the skip reason. | Planner task ID is stored, sync status is logged, and conflicts or connector failures are visible when enabled. |
| SMK-10 | Teams posts | Publish meeting notice, agenda-ready post, minutes-ready post, or action reminder to the configured Teams channel. | Channel message posts with a useful link; post ID, timestamp, target channel, and result are logged. |
| SMK-11 | Transcript-to-minutes manual input | Paste a transcript or meeting notes into the minutes workflow without AI Builder. | Manual transcript text is stored or referenced, summary fields can be edited, and no AI dependency is required. |
| SMK-12 | Transcript-to-minutes AI Builder | Run the AI Builder summarisation path when licensed and configured. Review and edit generated minutes. | AI output is marked as draft, editable before approval, and prompt/version/result metadata is logged. |
| SMK-13 | Integration logs | Trigger at least one success and one handled failure for Planner, Teams, document, and AI/logging flows. | Integration log captures integration name, correlation ID, source record, request summary, result, error detail if any, and retry status. |
| SMK-14 | AI human review gate | Run or simulate AI transcript minutes, then attempt to publish minutes or Teams summaries before review. Complete reviewer accept/edit/reject workflow. | Publishing is blocked before review; accepted or edited output records reviewer, review time, prompt/run evidence, and approval state. |
| SMK-15 | Governance dashboards | Open secretary, chair, member, and governance audit dashboard views using seeded records. Confirm the meeting pipeline, decision register, overdue actions, access exceptions, integration errors, and AI review queue are populated from Dataverse views. | Each dashboard shows the expected records only for the signed-in role, including missing agenda, pending minutes, overdue actions, failed integrations, access exceptions, and AI review queue. |
| SMK-16 | Reporting data-quality checks | Seed or simulate missing owners, overdue reviews, stale integration status, unapproved minutes with publish URL, and an action marked Planner sync succeeded without `dft_plannertaskid`. Open the governance audit dashboard and data-quality views. | Each exception appears in the matching reporting view and metric, without requiring Power BI/Fabric or live Planner/Teams/SharePoint connectors. |
| SMK-17 | Per-meeting feature switchboard | Review feature definitions for a meeting series default, a meeting-level override, and an intentionally disabled capability. | The effective feature state, override reason, and skipped or suppressed outcome are visible for the disabled feature. |

## Exit Criteria

- All core scenarios pass or have triaged defects with owner, severity, and workaround.
- No smoke blocker remains for governance bodies, meetings, agendas, attendees, papers, minutes, decisions, or actions.
- Planner, Teams, transcript/minutes, and integration log scenarios either pass in the target environment or are explicitly marked not enabled with a configuration reason.
- AI-generated material cannot be published unless human review evidence exists.
- Governance reporting surfaces and data-quality checks are populated from Dataverse views and respect role or membership access.
- Per-meeting feature switchboard defaults, overrides, and disabled states are visible in the same smoke review flow.
