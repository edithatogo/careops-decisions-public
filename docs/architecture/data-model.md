# Data Model

## Core Entities

The Dataverse model should keep governance records normalized and traceable from committee mandate through meeting evidence and follow-up actions.

| Entity | Purpose |
| --- | --- |
| Organization Unit | Internal division, program, service, or portfolio used for reporting and hierarchy. |
| Governance Body | Board, council, steering group, executive forum, or parent decision body. |
| Committee | Committee, subcommittee, working group, project board, or recurring operational forum. |
| Terms of Reference | Versioned mandate, authority, quorum, membership, review cycle, and reporting line for a governance body or committee. |
| Membership | Person-to-committee role, appointment term, voting rights, proxy rules, and active status. |
| Meeting Series | Recurring meeting pattern, owner, channel, calendar policy, and default templates. |
| Meeting | Individual meeting instance with status, agenda, invite, Teams meeting, papers, minutes, transcript, decisions, and actions. |
| Agenda Item | Ordered item with presenter, time allocation, purpose, paper links, decisions sought, and minute text. |
| Paper | Registered document or link with classification, version, owner, agenda item, and SharePoint location. |
| Attendee | Expected and actual attendee record with role, response, attendance status, apology, proxy, and voting eligibility. |
| Minute | Draft, reviewed, approved, and published meeting record tied to agenda items and decisions. |
| Decision | Formal resolution, outcome, authority, vote result, owner, due date, and escalation path. |
| Action | Follow-up task linked to Planner where applicable, with owner, due date, status, and evidence. |
| Transcript | Teams transcript or imported text with meeting link, classification, processing status, and retention status. |
| AI Builder Prompt | Prompt template, version, input type, output schema, approval status, and usage constraints. |
| AI Builder Run | Prompt execution tied to meeting, minutes, transcript, or paper, including model output and review status. |
| Integration Log | Operational audit trail for connector and flow activity. |

## Key Relationships

- Organization Unit has many Governance Bodies and Committees.
- Governance Body can have child Governance Bodies and Committees.
- Governance Body or Committee has many Terms of Reference versions; one version is current.
- Committee has many Membership records and Meeting Series records.
- Meeting Series has many Meetings.
- Meeting has many Agenda Items, Attendees, Papers, Minutes, Decisions, Actions, Transcripts, AI Builder Runs, and Integration Logs.
- Agenda Item can have many Papers, Decisions, Actions, and minute entries.
- Decision and Action can escalate to a parent Committee or Governance Body.
- Action can link to a Planner task while remaining authoritative in Dataverse.

## MVP Table Choices

For Dataverse for Teams, keep the first table set small and avoid complex plugins:

- Governance Body
- Committee
- Terms of Reference
- Membership
- Meeting
- Agenda Item
- Paper
- Attendee
- Minute
- Decision
- Action
- Transcript
- AI Builder Prompt
- AI Builder Run
- Integration Log

If Dataverse for Teams capacity or security is insufficient, the same schema should move to full Dataverse without changing user-facing concepts.

## Identity and Hierarchy

People fields should store the Office 365 Users identity available to the MVP: display name, mail, user principal name, job title, department, manager mail, and object ID where available. The Office 365 Users manager chain can support chair escalation, action reporting, and default approver suggestions.

The later Graph path should refresh identity and hierarchy into Dataverse using Entra object IDs as stable keys. Do not depend on display names for joins.

## Status and Classification

Important status fields:

- Meeting: planned, invite sent, in progress, minutes drafting, minutes approved, published, cancelled.
- Paper: draft, submitted, accepted, superseded, withdrawn.
- Minute: draft, chair review, approved, published.
- Decision: proposed, resolved, deferred, rescinded, superseded.
- Action: open, in progress, blocked, completed, overdue, closed.
- Transcript: not expected, requested, available, processed, redacted, retained, deleted.
- AI Builder Run: queued, completed, human reviewed, rejected, error.

Classification should apply to papers, minutes, transcripts, decisions, prompts, and AI outputs: public, internal, confidential, restricted, legal, or other local categories.
