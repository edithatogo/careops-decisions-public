# Support Roles And Escalation Paths

## Support Roles

| Role | Responsibility | Depth |
| ---- | -------------- | ----- |
| Secretary | First-line operational support. Responds to flow failures, data-quality exceptions, approval blockers, unresolved attendees, and stale syncs. | Can reset flow triggers, update record status, create or correct person records, and re-run sync flows from the app. |
| Chair | Escalation for stuck approvals and AI review exceptions. | Can approve or reject minutes and decisions. Can reassign reviewers. |
| Governance Administrator | Second-line support for access-rule errors, persistent flow failures, stale external IDs, and solution-level issues. | Has Dataverse system customizer privileges. Can create or modify environment variables, connection references, and security roles. |
| Power Platform Admin | Third-line platform support for DLP, environment management, solution import/export, tenant-level permissions, and connector registration. | Full Power Platform admin privileges. Can create environments, modify DLP policies, manage capacity, and assign Dataverse system admin. |
| SharePoint Owner | Support for SharePoint document library setup, permission changes, and file recovery. | Can create or modify SharePoint sites, libraries, permissions, and external sharing settings. |
| Planner Plan Owner | Support for Planner plan creation, task management, and permission changes. | Can create or modify Planner plans and adjust member permissions. |
| Auditor | Quarterly support for access-rule review, stale-permission detection, and compliance monitoring. | Read-only access to all records. Can request changes but cannot implement them. |

## Escalation Paths

### Tier 1: Secretary (Self-Service / App-Level)

- Reset flow trigger conditions for a stuck sync.
- Correct or add person records for unresolved attendees.
- Re-run a sync flow from the feature switchboard.
- Manually advance a stuck minutes or decision approval after verbal
  confirmation.
- Export meeting packs for business continuity.
- Identify and report stale external IDs.

### Tier 2: Governance Administrator (Dataverse / Solution-Level)

- Create or update environment variables and connection references.
- Modify security role assignments for users.
- Investigate persistent flow failures using run history and IntegrationLog.
- Update access rules and trigger data-quality remediation.
- Import or export solutions.
- Grant or revoke access team memberships.

### Tier 3: Power Platform Admin (Environment / Tenant-Level)

- Create or modify DLP policies.
- Restore Dataverse from backup.
- Assign or revoke Dataverse system admin roles.
- Enable or disable connectors at the environment level.
- Resolve environment-level throttling or capacity issues.
- Manage tenant-level service account creation.

### Tier 4: External Vendors / Support

- Microsoft Power Platform support for platform bugs or outages.
- Microsoft 365 support for SharePoint, Teams, Planner, or Exchange issues.
- Graph or Entra support for permission or authentication issues (deferred).

## Escalation Flow

```
User reports issue
  -> Secretary triages (Tier 1)
     -> Resolved: log in IntegrationLog, notify user
     -> Not resolved: open issue for Governance Administrator (Tier 2)
        -> Resolved: log in IntegrationLog, notify secretary and user
        -> Not resolved: escalate to Power Platform Admin (Tier 3)
           -> Resolved: log in IntegrationLog, notify chain
           -> Not resolved: contact Microsoft Support (Tier 4)
```

## Escalation Response Times

| Severity | Definition | Tier 1 | Tier 2 | Tier 3 |
| -------- | ---------- | ------ | ------ | ------ |
| P1 | Meeting pack cannot be produced; minutes approval is blocked for multiple bodies | 2 hours | 4 hours | 8 hours |
| P2 | Single meeting pack affected; data-quality error for one record | 4 hours | 8 hours | 24 hours |
| P3 | Stale sync, unresolved attendee, or warning-level data quality | Next business day | 2 business days | 5 business days |
| P4 | Governance configuration health warning, quarterly audit finding | 5 business days | 10 business days | Per priority review |
