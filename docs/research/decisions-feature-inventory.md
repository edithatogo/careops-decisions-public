# Decisions Feature Inventory

Date: 2026-05-02
Scope: public feature mapping for a Power Platform governance meeting system inspired by Decisions for Microsoft Teams. This is not a clone spec; it identifies meeting-management capabilities worth modelling with Microsoft 365 and Power Platform primitives.

## Canonical public sources

- Decisions product site: https://www.meetingdecisions.com/
- Microsoft Marketplace listing WA104381880: https://marketplace.microsoft.com/en-us/product/office/WA104381880
- Microsoft 365 App Certification entry: https://learn.microsoft.com/en-us/microsoft-365-app-certification/teams/decisions
- Power Apps in Teams overview: https://learn.microsoft.com/en-us/power-apps/teams/create-apps-overview
- Microsoft Teams Power Apps templates repo: https://github.com/microsoft/teams-powerapps-app-templates
- Power Apps samples repo: https://github.com/microsoft/PowerApps-Samples
- Power Platform toolkits overview: https://learn.microsoft.com/en-us/power-platform/guidance/toolkits/
- Creator Kit: https://www.microsoft.com/en-us/power-platform/blog/power-apps/introducing-the-creator-kit-efficiently-create-performant-fluent-ui-based-power-apps/
- CoE Starter Kit: https://learn.microsoft.com/en-us/power-platform/guidance/coe/overview
- Power Apps Test Engine: https://learn.microsoft.com/en-us/power-platform/release-plan/2025wave1/data-platform/test-power-apps-test-engine

## Public feature map

| Public Decisions capability | Evidence source | Governance meeting interpretation | Power Platform fit |
| --- | --- | --- | --- |
| Structured agenda builder with objectives, presenters, attachments, and agenda templates | Product site and Marketplace | Create a governed meeting record with agenda items, owners, expected decisions, pre-read links, and reusable agenda patterns | Dataverse tables for Meeting, Agenda Item, Attachment Link, Meeting Type Template; Teams tab/personal app surface |
| Pre-meeting collaboration, topic suggestions, and agenda approval workflow | Product site and Marketplace | Allow members to propose agenda items, route chair/secretariat approval, and freeze the agenda before circulation | Power Apps canvas/model-driven app plus Power Automate approval/status flows |
| Meeting book export to PDF or PowerPoint | Marketplace | Produce a board/committee pack from agenda, pre-reads, and prior actions | Reference as future output pattern; implement first with Word/PDF templates or SharePoint document generation |
| In-meeting side panel, real-time agenda management, and time tracker | Product site and Marketplace | Run the meeting from a Teams-embedded app, track current agenda item, expected duration, and overruns | Reuse Teams embedding patterns; avoid custom in-meeting extension unless Teams app development is in scope |
| Secure voting | Product site and Marketplace | Capture motions, approvals, abstentions, conflicts, and voting outcomes with auditable metadata | Dataverse vote table with role/security model; Power Apps UI; consider Microsoft Forms only for low-risk informal polling |
| One-click meeting minutes for Word and OneNote | Marketplace and product site | Generate structured minutes from agenda, decisions, votes, and actions | Word template generation and SharePoint storage; OneNote optional and should not be system of record |
| AI summaries, recaps, and key-takeaway extraction | Product site and Marketplace | Assist minute drafting, action extraction, and summary preparation | Reference as later capability; require tenant data-boundary, audit, and human approval design before implementation |
| Decisions and action items synced to Planner and To Do | Product site and Marketplace | Convert meeting outcomes into trackable actions with accountable owners and due dates | Power Automate connectors to Planner/To Do; keep Dataverse as canonical action register if auditability matters |
| Automated decision log | Product site and Marketplace | Maintain searchable register of decisions, authority, meeting source, implementation status, and review date | Core Dataverse table and primary differentiator for governance use case |
| Engagement score, attendee feedback, hand raising, speaker equity cues | Product site and Marketplace | Optional governance hygiene metrics: attendance, preparedness, meeting effectiveness, unresolved concerns | Defer or keep lightweight; likely less important than decision/action audit trail for MVP |
| Deep Microsoft 365 integration and Teams-native experience | Product site and Marketplace | Users should work in Teams, SharePoint, Planner, Outlook/Calendar, and Microsoft 365 identity | Power Apps in Teams and Dataverse for Teams can accelerate MVP; full Dataverse may be better for enterprise governance |
| Security/compliance posture, tenant-data claims, ISO/SOC references | Product site and certification page | Meeting data handling must be explicit: system of record, geographic storage, identities, retention, access | Use Microsoft 365 tenant-native storage where possible; document data residency, retention, DLP, and certification gaps |

## MVP feature recommendation

Build the first release around governance control rather than broad meeting productivity:

1. Meeting register with committee/board/project meeting types.
2. Agenda builder with item owners, objectives, pre-read links, and agenda approval.
3. Meeting run sheet with agenda status, time allocation, motions, decisions, and actions.
4. Decision log as the canonical outcome register.
5. Action register with Planner/To Do sync as secondary workflow integration.
6. Minutes generation from structured records into SharePoint/Word.
7. Basic security roles: secretariat, chair, member, observer, administrator.

Defer AI summaries, engagement scoring, polished meeting-book exports, and custom Teams in-meeting extensions until the data model, audit trail, permissions, and minutes workflow are stable.

## Data model implications

Minimum Dataverse entities:

- Meeting Type / Committee
- Meeting
- Agenda Item
- Agenda Template
- Participant / Role Assignment
- Motion / Vote
- Decision
- Action Item
- Pre-read / Attachment Link
- Minutes Document
- Feedback / Retrospective Note

The decision log should not be a derived text artifact. It should be a first-class table linked back to meeting, agenda item, motion/vote, authority, owner, status, review date, and source minutes.

## Compliance notes

The Microsoft 365 certification page is publisher-attested and reports Decisions as Teams-supported, Azure-hosted, and processing/storing Microsoft Graph object IDs, with substantive meeting content remaining in the tenant according to the publisher response. Treat this as a benchmark for the kind of data-handling disclosure expected, not as evidence that a custom Power Platform app automatically inherits the same posture.

For this workspace, document these decisions early:

- Whether Dataverse for Teams is sufficient or full Dataverse is required.
- Which system is authoritative for decisions and actions.
- Whether AI features can process meeting content.
- Retention and deletion obligations for minutes, votes, and decision logs.
- Whether external participants can access meeting packs or only receive exported documents.
