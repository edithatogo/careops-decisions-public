# Integration Map

## Integration Principles

Dataverse is the authoritative record for governance state. Microsoft 365 systems provide collaboration, documents, calendars, tasks, identity, transcripts, and notifications. Flows should be idempotent, log every run, and store external IDs back on Dataverse records.

## MVP Connectors

| System | MVP Use | Dataverse Anchor |
| --- | --- | --- |
| Office 365 Outlook | Create and update meeting invites, track responses where available, send reminders and approvals. | Meeting, Attendee, Integration Log |
| Microsoft Teams | Host the embedded app, post meeting notices, link channels, and later associate Teams meetings. | Committee, Meeting, Integration Log |
| SharePoint | Store papers, approved minutes, exports, and evidence files with registered links and classification. | Paper, Minute, Transcript, Integration Log |
| Planner | Create and update follow-up tasks while keeping Dataverse as the action register. | Action, Decision, Integration Log |
| Office 365 Users | Resolve people, mail, job title, department, manager, and reporting chain hints. | Membership, Attendee, Action, Decision |
| AI Builder | Summarize transcripts, draft minutes, extract decisions/actions, classify papers, and compare outputs against review state. | AI Builder Prompt, AI Builder Run, Minute, Decision, Action |

## Later Graph and Azure Path

Microsoft Graph should be introduced when standard connectors cannot provide reliable meeting, transcript, identity, group, or file access. Candidate workloads:

- Create and update Outlook calendar events with stronger attendee state handling.
- Create Teams online meetings and retrieve meeting metadata.
- Retrieve Teams transcripts, recordings metadata, chat references, and attendance reports where permitted.
- Sync Entra users, groups, managers, and committee security groups.
- Resolve SharePoint drive items with stable IDs and retention metadata.
- Support app-only background jobs through Azure Functions or Logic Apps.

Graph integration should use Entra app registration, least-privilege permissions, admin consent, managed secrets or certificates, retry policies, throttling handling, and integration logging.

The canonical Track 07 enterprise upgrade boundary is `docs/architecture/enterprise-upgrade-path.md`. That document is the handoff point for app registrations, Graph permissions, Azure runtime shape, Entra group mapping, Teams extension work, and Power BI/Fabric reporting gates. The MVP must not depend on those services before the Future Azure / Entra Gate passes.

## Meeting Lifecycle

1. Secretariat creates a Meeting from a Meeting Series.
2. Outlook invite is created or updated, and the external event ID is stored on the Meeting.
3. Teams channel or meeting post is created when required, with the message ID stored.
4. Attendees are resolved through Office 365 Users and linked to Membership where possible.
5. Papers are registered against agenda items and stored or linked in SharePoint.
6. Planner tasks are created for pre-meeting preparation or post-meeting actions.
7. Minutes, decisions, and actions are drafted in the app and optionally assisted by AI Builder prompts.
8. Transcript text is imported or later retrieved through Graph, classified, processed, and linked to AI Builder runs.
9. Chair approval publishes minutes, decisions, actions, and notifications.
10. All connector activity writes Integration Log rows.

## AI Builder Prompt Governance

Prompt templates should be versioned Dataverse records. Each prompt must define purpose, input records, expected output schema, human reviewer role, data classification limits, and whether it may use transcript, paper, or minute text.

AI Builder outputs are draft assistance only until reviewed. Decisions, actions, and minutes become official only when a permitted human role approves them. Prompt runs should store input references, output references, review status, reviewer, timestamp, and errors.

## Access and Audit

Flows should run with connection references owned by service or environment accounts where possible, not individual maker accounts. Access-sensitive operations should log actor, connector identity, target system, record, classification, and outcome.

The later enterprise model should align Teams membership, Entra groups, Dataverse security roles, SharePoint permissions, and Planner access. Where these systems cannot be perfectly aligned in the MVP, Dataverse should still record intended access and classification so gaps are visible.
