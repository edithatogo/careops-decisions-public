# Monitoring Requirements

## Scope

Monitoring covers Dataverse record health, Power Automate flow execution,
connector sync state, AI Builder usage, and access-rule drift. MVP monitoring
is dashboard-driven, not automated alerting. Future Azure Monitor or
Application Insights integration is noted for the enterprise path.

## MVP Monitoring Approach

All MVP monitoring uses Dataverse views and Power Apps dashboards as
documented in `powerplatform/src/reporting/dataverse-dashboards.yaml`.
A human operator (secretary, chair, auditor, or administrator) reviews the
relevant dashboard on each login or on a scheduled cadence.

## Monitored Dimensions

| Dimension | What To Watch | Dashboard | Cadence | Owner |
| --------- | ------------- | --------- | ------- | ----- |
| Flow health | Failed runs, retries, stale queued items | Governance Audit / Connector Health | Daily | Secretary |
| Sync staleness | IntegrationLog queued or retried >24h | Governance Audit / Stale Integration | Daily | Secretary |
| Approval progress | Minutes and decisions stuck in review | Chair Review | Weekly | Chair |
| AI review queue | Completed AI runs pending human review | Governance Audit / AI Review Queue | Weekly | Chair or Secretary |
| Data quality | Missing owners, overdue reviews | Governance Audit | Weekly | Secretary |
| Access exceptions | Unexpected allow or missing access rules | Governance Audit / Access Exceptions | Monthly | Auditor |
| Action drift | Actions synced without Planner task ID | Member Actions | Weekly | Secretary |
| Feature switchboard | Disabled capabilities that should be enabled | Governance Configuration Health | Monthly | Administrator |
| Governance body health | Expired terms, missing owners, overdue reviews | Governance Configuration Health | Monthly | Administrator |
| Person data | Unresolved attendees, stale OrgPerson records | Secretary Workbench | Per meeting | Secretary |

## Alerting (Future)

When Azure Monitor or Application Insights is available, implement alerts for:

1. **Flow failure rate > 5% in 1 hour**: Triggers when more than 5% of
   Power Automate runs fail within a rolling hour window.
2. **Stale sync > 24 hours**: Triggers when any IntegrationLog row with a
   high-severity operation (Planner sync, document registration, minutes
   publication) remains in Queued or Retried status for more than 24 hours.
3. **AI output published without review**: Triggers when an AIBuilderRun
   transitions to published state without passing through human review.
4. **Access rule created with unrestricted scope**: Triggers when a new
   `dft_AccessRule` is created with `dft_scope=body_wide` and
   `dft_ruletype=Allow`.
5. **New solution component deployed**: Triggers when a managed solution
   import adds or updates components in production.

## Operational Alert Fields

Every operational alert, whether represented as a Dataverse dashboard row,
manual support ticket, or future Azure Monitor alert, must capture these
fields before it is treated as actionable:

| Field | Required | Purpose |
| ----- | -------- | ------- |
| Owner | Yes | Named role or person responsible for triage and closure. |
| Severity | Yes | Operational priority using the support severity model. |
| Evidence | Yes | Link or reference to the failing flow run, IntegrationLog row, dashboard view, record, or exported file. |
| Resolution | Yes | Closure summary, mitigation, rollback, or follow-up action. |
| Source record | Yes | Dataverse row, flow run, or external system object that triggered the alert. |
| Correlation ID | If available | Flow run ID, IntegrationLog ID, Planner task ID, SharePoint file ID, or Graph object ID. |
| Detected on | Yes | Date and time the alert was first observed. |
| Due by | Yes | Target response or closure date derived from the Monitoring Dashboard SLA. |

## Monitoring Dashboard SLA

| Priority | Response Time | Owner |
| -------- | ------------- | ----- |
| Flow failures blocking a meeting pack | 4 business hours | Secretary |
| AI output published without review | 1 business day | Governance Administrator |
| Access rule drift (over-permission) | 2 business days | Auditor |
| Data-quality warnings | 5 business days | Secretary |
| Stale integration status | Next business day | Secretary |
| Governance configuration drift | Next month review | Administrator |

## Implementation Notes

- MVP dashboards do not send push notifications. Users must navigate to the
  app and review their dashboard.
- The `dft_IntegrationLog` table is the single source of truth for
  connector and flow health monitoring. All flows must write a log row for
  each operation.
- Monitoring dashboards are seeded with sample data during deployment.
  Production dashboards contain live data after the first meeting cycle.
