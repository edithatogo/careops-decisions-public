# Power Platform Architecture

## Architecture Direction

Decisions Workspace is a repo-first Power Platform governance meeting system. Source-controlled solution assets, docs, tests, and deployment scripts are the system of record; environment configuration is derived from the repo where possible.

The MVP should run with either Dataverse for Teams or standard Dataverse, embedded in Microsoft Teams as a canvas app with Power Automate flows. It should avoid tenant-wide Azure or Entra app registration assumptions. The later enterprise path moves the same domain model to full Dataverse, Entra security groups, Microsoft Graph, Azure-hosted integration services, and richer reporting.

## MVP Platform

- Power Apps canvas app embedded in Teams for committee secretariats, chairs, members, and administrators.
- Dataverse for Teams or Dataverse tables for governance bodies, committees, terms of reference, meetings, attendees, papers, minutes, decisions, actions, transcripts, prompts, and integration logs.
- Power Automate cloud flows for invitations, reminders, Teams notifications, Planner tasks, SharePoint file registration, AI Builder processing, and audit logging.
- Standard connectors first: Microsoft Teams, Office 365 Outlook, Office 365 Users, Planner, SharePoint, and AI Builder.
- Repo-managed Power Platform solution files under `powerplatform`, with CLI validation in `scripts` and `tests`.

## Later Enterprise Platform

- Full Dataverse environments with managed solutions, environment variables, connection references, role-based security, solution checker, and deployment gates.
- Entra ID security groups for administrators, secretariats, chairs, committee members, guests, and read-only auditors.
- Microsoft Graph for calendar events, Teams meetings, transcripts, users, groups, photos, mailbox data, files, and org hierarchy where connector limits become blockers.
- Azure Functions or Logic Apps for Graph workloads that need application permissions, durable retries, or centralized throttling.
- Power BI or Fabric reporting over Dataverse governance, action, attendance, decision, and compliance history.

## Governance Hierarchy

The model supports a reporting hierarchy:

- Organization or program area.
- Governance body, board, council, steering group, or executive forum.
- Committee, subcommittee, working group, project board, or recurring operational forum.
- Meeting series and individual meetings.
- Agenda items, papers, decisions, actions, risks, votes, minutes, and evidence.

Each governance body and committee has a current terms of reference record defining purpose, authority, quorum, membership rules, review cycle, escalation path, document retention expectations, and parent body.

## Access Controls

MVP access is enforced through Dataverse table permissions, Teams membership where applicable, and app-level role checks. The minimum roles are:

- Governance administrator: manage configuration, hierarchy, templates, logs, and access.
- Secretariat: create meetings, invite attendees, manage papers, run minutes, publish actions, and correct records.
- Chair: review agenda, approve minutes, confirm decisions, and escalate actions.
- Member: view assigned committees, papers, minutes, decisions, and assigned actions.
- Guest or presenter: limited access to specific meetings or papers.
- Auditor: read-only access to approved records and logs.

Sensitive records such as confidential papers, closed minutes, transcript text, and AI prompt outputs should have explicit classification fields and access rules. The later full Dataverse path should map these roles to Entra groups and Dataverse security teams.

## Logging

Every integration flow should write an integration log entry with source system, target system, record type, related Dataverse row, actor, correlation ID, status, error message, retry count, and timestamp. Logs should cover invite creation, attendee sync, Teams posts, Planner task creation, SharePoint file registration, transcript ingestion, AI Builder prompt execution, and access-sensitive operations.
