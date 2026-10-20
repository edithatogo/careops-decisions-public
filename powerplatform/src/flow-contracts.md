# Flow Contracts

## Shared Rules

- Each flow must read the source row or draft record first, then decide whether work is needed.
- Optional connector work must be gated by the resolved meeting feature switchboard and the target environment binding.
- If a connector is missing, disabled, or not requested for the current meeting, the flow should log `Skipped` and exit instead of failing or silently falling back to uncontrolled behavior.
- Teams, Planner, Outlook, SharePoint, Office 365 Users, and AI Builder remain environment-gated integrations; meeting-level overrides can narrow them, but they cannot force a connector that is unavailable in the tenant.
- Human review is required before any AI-generated minutes, decisions, actions, or published summaries are treated as authoritative.

## Planner Task Sync

- Trigger: `dft_ActionItem` created or marked ready for sync.
- Inputs: action title, description, due date, owner, committee, meeting, priority.
- Behavior: create Planner task only if no `PlannerTaskId` exists, the meeting switchboard still requests Planner sync, and the Planner binding has been explicitly configured. Missing bindings and disabled meetings are logged as skipped work.
- Binding: use `decisions_PlannerPlanId` in deployment settings; in a local dev space, the DMS Workplan plan id `fictional-planner-plan` is the optional fallback candidate only.
- Output: Planner task ID, sync timestamp, status, integration log.

## Teams Summary Post

- Trigger: agenda approved, minutes finalized, or action digest scheduled.
- Inputs: meeting, governance body, channel ID, summary type.
- Behavior: post a Teams channel message and write success/failure to `dft_IntegrationLog`, but only after the team and channel bindings are present, the meeting switchboard allows posting for that meeting, and the source row is eligible. SharePoint links stay deferred unless a future document-link path is explicitly enabled.
- Binding: `decisions_TeamsTeamId` and `decisions_TeamsChannelId` stay environment-driven; the DMS Executive Team group id `11111111-1111-4111-8111-111111111111` is a local dev-space candidate, and the SharePoint site `https://example.invalid/sites/decisions` is a deferred candidate for future links only.

## Outlook Invite

- Trigger: meeting invite requested or attendee list changed.
- Inputs: subject, body, start/end, location/Teams link, required attendees, optional attendees.
- Behavior: create/update event where connector permissions allow and the meeting-level invite request is enabled; log external event ID and skip cleanly when the Outlook binding is missing or the invite is not requested. Do not add Teams or Planner binding to this flow unless a later business requirement needs it.

## Office 365 Users Hierarchy Sync

- Trigger: manual refresh or scheduled sync.
- Inputs: user principal name or committee member list.
- Behavior: resolve profile, manager, and direct reports where permitted; cache in Dataverse with last synced timestamp. Missing manager lookups are warnings, not hard failures, and a missing Office 365 Users connector should be recorded as a skipped integration rather than a runtime failure.

## Transcript To Draft Minutes

- Trigger: transcript text submitted or future Graph transcript available.
- Inputs: transcript, agenda, attendee list, committee context, terms of reference.
- Behavior: AI Builder prompt drafts minutes, decision candidates, action candidates, risks, and unresolved items only when the feature flag and prompt binding are present.
- Output: draft records requiring human review, plus an integration log row that records the review gate and the fact that nothing is publishable yet.
- Guardrails: never publish directly from AI output; keep SharePoint, Teams, and Planner bindings deferred until the draft has been reviewed and approved in Dataverse, and log skipped work when AI Builder is disabled or unavailable.
