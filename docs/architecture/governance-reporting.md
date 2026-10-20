# Governance And Reporting

## Purpose

Governance and reporting are first-class requirements for Decisions Workspace. The system should not only run meetings; it should prove that governance bodies are operating within their terms of reference, decisions are traceable, actions are followed through, and sensitive records are controlled.

## Governance Controls

- Terms of reference: current and historical mandate, authority, quorum, membership rules, reporting line, review cycle, document retention, and escalation path.
- Governance hierarchy: parent body, committee, subcommittee, working group, owner, chair, secretary, and reporting relationship.
- Membership and attendance: role, voting rights, term, attendance response, actual attendance, apologies, substitutes, and quorum effect.
- Decision register: decision number, source meeting, agenda item, authority, owner, status, implementation target, review date, and linked actions.
- Action register: accountable owner, due date, priority, Planner task reference, status, escalation route, and completion evidence.
- Conflict register: declaration, meeting or agenda item, affected person, handling decision, and audit status.
- Access rules: intended visibility and classification for governance body, meeting, paper, minutes, transcript, AI output, and published records.
- AI governance: prompt version, input classification, output schema, run status, human review, publication approval, and exception log.
- Integration audit: source system, target system, operation, record type, correlation ID, retry count, actor, status, and error message.

## Reporting Layers

1. Dataverse operational views
   - Immediate MVP reporting for secretariat and chairs.
   - Views for the meeting pipeline, agenda readiness, missing papers, minutes review, decision register, overdue actions, access exceptions, integration errors, AI review queue, and data-quality exceptions.

2. Power Apps dashboards
   - Lightweight role-specific summaries inside the app.
   - Chair dashboard, secretary dashboard, member action list, auditor review queue.

3. Power BI or Fabric
   - Deferred until full Dataverse access, workspace governance, stable sample data, and the security model are available.
   - Semantic model over Dataverse tables with row-level security aligned to governance role and committee membership.
   - Future-facing trend analysis can reuse the same contracts for hierarchy coverage, feature-switch adoption, connector health, and review latency once the Dataverse layer is stable.

4. Future Azure analytics
   - Optional data lake or Fabric shortcuts for historical trend analysis only when tenant architecture permits.
   - No dependency for MVP.

## Core Metrics

- Governance body health: active bodies, expired terms of reference, missing owner, missing secretary, review overdue.
- Meeting performance: scheduled meetings, cancelled meetings, quorum achieved, attendance rate, agenda completion, paper submission timeliness.
- Decision performance: decisions made, deferred, implemented, overdue review, linked actions completed.
- Action performance: open actions, overdue actions, average age, completion rate, owner workload, escalation count.
- Records performance: minutes draft age, approval age, publication age, missing papers, missing transcript review, retention status.
- AI performance: prompt runs, human review outcomes, accepted/rejected suggestions, exception count, classification breaches.
- Integration performance: successful syncs, failed syncs, retries, stale external IDs, unresolved attendees, Planner task drift.
- Access and audit: restricted records, access-rule changes, conflict declarations, audit review items.

## MVP Metric Dictionary

| Metric | Source | Definition | Primary dashboard |
| --- | --- | --- | --- |
| `meetingsScheduledNext30Days` | `dft_Meeting` | Count of draft, planned, or invited meetings with `dft_startdatetime` in the next 30 days. | Secretary Workbench |
| `meetingsMissingAgendaNext30Days` | `dft_Meeting`, `dft_AgendaItem` | Count of next-30-day meetings with no active agenda item. | Secretary Workbench |
| `minutesPendingReviewCount` | `dft_Minutes` | Count of minutes in draft or pending-review status. | Secretary Workbench |
| `minutesAwaitingChairApproval` | `dft_Minutes` | Count of minutes pending approval with no `dft_approvedon` timestamp. | Chair Review |
| `unapprovedMinutesWithPublishUrlCount` | `dft_Minutes` | Count of minutes where `dft_status` is not approved, published, or archived but `dft_publishedurl` is populated. | Secretary Workbench |
| `openDecisionCount` | `dft_Decision` | Count of proposed or deferred decisions. | Chair Review |
| `decisionsOverdueReviewCount` | `dft_Decision` | Count of open decisions with `dft_reviewdueon` before today. | Chair Review |
| `overdueActionCount` | `dft_ActionItem` | Count of non-completed action items with `dft_duedate` before today. | Chair Review |
| `syncedActionsMissingPlannerTaskId` | `dft_ActionItem` | Count of actions where `dft_plannersyncstatus` is succeeded but `dft_plannertaskid` is blank. | Member Actions |
| `governanceHierarchyBodyCount` | `dft_GovernanceBody` | Count of governance bodies represented in the hierarchy overview. | Governance Configuration Health |
| `featureSwitchTemplateCount` | `dft_FeatureTemplate` | Count of contextual feature templates across governance bodies and meeting series. | Governance Configuration Health |
| `meetingFeatureOverrideCount` | `dft_MeetingFeatureOverride` | Count of explicit meeting-level feature overrides. | Governance Configuration Health |
| `connectorHealthEventCount` | `dft_IntegrationLog` | Count of recent connector and flow activity rows across Dataverse and external systems. | Governance Configuration Health |
| `activeAccessExceptionCount` | `dft_AccessRule` | Count of exception access rules or restricted/legal access rules. | Governance Audit |
| `openIntegrationErrorCount` | `dft_IntegrationLog` | Count of integration logs in failed or retried status. | Governance Audit |
| `staleIntegrationStatusCount` | `dft_IntegrationLog` | Count of integration logs still queued or retried after 24 hours. | Governance Audit |
| `aiRunsPendingReview` | `dft_AIBuilderRun` | Count of completed AI Builder runs where `dft_humanreviewstatus` still requires review. | Governance Audit |
| `recordsMissingOwnerCount` | `dft_GovernanceBody`, `dft_Decision`, `dft_ActionItem`, `dft_Paper` | Count of records missing the relevant owner lookup. | Governance Audit |
| `overdueReviewCount` | `dft_Decision`, `dft_TermsOfReference` | Count of decisions or terms-of-reference records with review due dates before today. | Governance Audit |

## Data-Quality Rules

| Check | Severity | Rule | Response |
| --- | --- | --- | --- |
| Missing owners | Warning | Governance bodies missing `dft_ownerpersonid`, or decisions, actions, and papers missing `dft_ownerid`. | Assign an accountable person before relying on escalation, workload, or owner reporting. |
| Overdue reviews | Warning | Decisions or terms of reference with review due dates before today. | Record review outcome, update due date with approval evidence, or close the record. |
| Stale integration status | Warning | Integration logs still queued or retried after 24 hours. | Replay, fail, or close the integration log with correlation ID evidence. |
| Unapproved minutes with publish URL | Error | Minutes not approved, published, or archived where `dft_publishedurl` is populated. | Remove the URL or complete approval and human review before publication. |
| Actions synced without Planner task ID | Error | `dft_plannersyncstatus` is succeeded but `dft_plannertaskid` is blank. | Reconcile the Planner task ID or reset the sync status to retry or failed. |

## Security And Privacy

- Reports must not expose restricted papers, closed minutes, transcripts, or AI outputs to users who cannot access the source records.
- Dataverse security roles and teams should be the primary security boundary for MVP reporting.
- Power BI row-level security should mirror Dataverse role and committee membership in the enterprise path.
- Aggregated metrics can be broader than record-level access only if counts cannot identify sensitive records.

## Acceptance Criteria

- MVP includes Dataverse views for chair, secretary, member, and auditor workflows.
- App map includes dashboard surfaces for operational governance reporting.
- Power BI/Fabric remains deferred and does not block Dataverse dashboard delivery.
- Reporting is included in deployment validation and smoke testing.
- Report access is mapped to security roles and committee membership.
- Governance configuration reporting must show hierarchy, feature-switch adoption, connector health, access exceptions, and review gates without depending on Power BI/Fabric.

## Source Artifacts

- `powerplatform/src/reporting/dataverse-dashboards.yaml` defines the MVP dashboard and view contract.
- `powerplatform/src/reporting/README.md` documents the Dataverse-first reporting order.
- `tests/smoke/smoke-scenarios.md` includes dashboard and audit reporting smoke coverage.

## Future Power BI / Fabric Model Boundaries And RLS Assumptions

### Model Boundary

The Power BI / Fabric semantic model will be defined over Dataverse tables
exported via Synapse Link, Fabric shortcuts, or a scheduled dataflow.
The model boundaries are:

| Star Schema Role | Dataverse Table | Grain |
| ---------------- | --------------- | ----- |
| Fact | `dft_Meeting` | Per meeting |
| Fact | `dft_Attendee` | Per attendee per meeting |
| Fact | `dft_AgendaItem` | Per agenda item per meeting |
| Fact | `dft_Minutes` | Per version per meeting |
| Fact | `dft_Decision` | Per decision per meeting |
| Fact | `dft_ActionItem` | Per action per meeting |
| Fact | `dft_Vote` | Per vote per meeting |
| Fact | `dft_VoteResponse` | Per response per vote |
| Fact | `dft_AIBuilderRun` | Per run per meeting |
| Fact | `dft_IntegrationLog` | Per log entry |
| Fact | `dft_ConflictOfInterest` | Per declaration per meeting |
| Dimension | `dft_GovernanceBody` | Per body |
| Dimension | `dft_TermsOfReference` | Per version per body |
| Dimension | `dft_CommitteeMembership` | Per membership per body per person |
| Dimension | `dft_OrgPerson` | Per person |
| Dimension | `dft_MeetingSeries` | Per series |
| Dimension | `dft_FeatureDefinition` | Per capability |
| Dimension | `dft_AccessRule` | Per rule |
| Bridge | `dft_MeetingFeatureOverride` | Per override per meeting |
| Bridge | `dft_FeatureTemplate` | Per template per body or series |

### RLS Assumptions

Row-level security in Power BI must mirror the Dataverse security model:

1. **Secretary role**: Can see all data for assigned governance bodies.
   Filter by body membership.
2. **Chair role**: Can see all meeting, decision, action, and minutes
   data for assigned bodies. Cannot see integration logs or AI prompt
   templates.
3. **Member role**: Can see data for bodies they belong to. Filter by
   `dft_CommitteeMembership` with `dft_membershipstatus=Active`.
4. **Auditor role**: Can see all data. No filter applied.
5. **Restricted Reader role**: Can see only records explicitly granted
   via `dft_AccessRule`. Requires dynamic RLS based on access rule
   evaluation.
6. **Aggregated metrics**: Counts without record-level detail can be
   broader than individual record access, provided individual records
   cannot be inferred from the aggregate.

RLS implementation uses the Power BI `USERNAME()` or `USERPRINCIPALNAME()`
function matched against `dft_OrgPerson.dft_workemail` to determine the
person, then joins to `dft_CommitteeMembership` and `dft_AccessRule` to
resolve the role and scope.

### Workspace Readiness Requirements

Before Power BI / Fabric is implemented:

1. Dataverse security roles and access teams are implemented and tested.
2. Stable sample data with at least 3 governance bodies, 3 meeting series,
   10 meetings, and 50 decisions/actions is available.
3. Workspace governance is established: who can publish reports, who can
   manage permissions, and how reports are certified.
4. The Dataverse-to-Fabric export path is configured and tested with a
   small data subset.
5. RLS roles are modelled in Power BI and tested against secretary, chair,
   member, auditor, and restricted reader test accounts.

### Deferred Items

- **Fabric lakehouse or shortcuts**: Deferred until the organisation has
  Fabric capacity and data governance is established.
- **Graph-backed enterprise analytics**: Deferred until Graph access,
  admin consent, and app registration are approved.
- **Real-time dashboard refresh**: Deferred; use daily or scheduled
  refresh for MVP reporting.

## MVP Activation Tasks

1. Create Dataverse views from the reporting contract for secretary, chair, member, and auditor workflows.
2. Add dashboard navigation to the model-driven or canvas app shell.
3. Seed at least one record for each operational view: missing agenda, draft minutes, late paper, open action, failed integration, and AI review queue.
4. Validate dashboard access with a secretary, chair, member, and auditor test account.
5. Keep Power BI/Fabric deferred until the Dataverse security and sample data model are stable.
