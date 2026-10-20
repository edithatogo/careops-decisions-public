# Native Dataverse register: module delivery boundary

Status: target design, not implemented or tenant-approved. 23 September 2026.
Programme: https://github.com/edithatogo/careops/issues/160

## Production decision

Use full Microsoft Dataverse for structured credentialing/register records and
service-owned SharePoint libraries for controlled evidence. Model-driven Power
Apps supplies the role-scoped register and governance workbench. Small,
solution-aware Power Automate workers handle coordination. Copilot is optional.
Do not deploy SQLite or Dataverse for Teams as the mission-critical credentialing
store. The existing SQLite implementation remains a synthetic local test adapter;
it has never been approved for real practitioner records. Preserve its regression
fixtures but implement the production boundary with native platform capabilities.

The programme's architecture, recovery distinction and modularity contract are:
- https://github.com/edithatogo/careops/blob/main/docs/credentialing/native-microsoft-platform.md
- https://github.com/edithatogo/careops/blob/main/docs/standalone-module-contract.md

These links are developer documentation, not runtime service dependencies.

## Translate invariants, not the SQLite storage design

| Existing invariant | Native implementation to build | Required native evidence |
| --- | --- | --- |
| Verified human decision and current authority | Restricted Custom API/plug-in with authenticated principal, exact evidence/profile versions and acting/conflict checks | Forged receipt, stale delegation and direct CRUD bypass rejected |
| Explicit supersession and no revived revocation | Append-only business decision history with separately maintained current projection and constrained mutation permissions | Historical, future-effective and replayed approvals cannot erase/revive later decisions |
| Optimistic concurrency and exact retries | Conditional updates/row version, stable command key and exact-content idempotency | Simultaneous native requests, lost response and altered retry tests |
| Atomic decision/history/event write | Reviewed Dataverse server-side transaction boundary and durable outbox | Injected intermediate failure leaves no partial authoritative record or external effect |
| Profession/facility/own-record views | Authenticated server-side purpose/role access and tested row/column controls | Direct API, search, export and UI have consistent access boundaries |
| Currentness separate from recorded decision | Explicit source watermark/verification state and expiry rules | Failed source does not manufacture approval/revocation or conceal limitations |
| Persistence and recovery | Native Dataverse backups plus separately governed SharePoint recovery and reconciliation | Restored decisions/documents agree; flows quarantined; no duplicate letters or lost revocation |

A stored Boolean, hash, hidden form field or possession of a record ID is not
authorisation. Restrict direct writes and exports; test privileged and non-UI
access. Built-in auditing complements, but does not replace, business history or
read/export logging where required. Application logic and privileges must protect
against bypass; simply creating a Custom API does not do so.

## Standalone and integrated operation

The Decisions core must install and run its documented governance purpose without
Approve, Process, Atlas, Copilot or the parent repository. It needs a controlled
local case-entry path and can optionally accept a versioned external packet.
The credentialing-register capability remains Decisions-owned. Approve and other
consumers connect through separately installable versioned adapters; core
solutions must not contain sibling-owned Dataverse lookups or mandatory flows.

Each enabled pathway retains required peer/committee/delegate acts whether its
input originates locally, from Approve or another approved system. No absence of
an integration grants permission to bypass governance. Shared environments share
outage and restore boundaries even when packages install independently.

## Roadmap execution in this module

Primary owning track: N03, native register; shared responsibilities in N01
packaging, N02 authority, N04 evidence, N07 workbench, N08 governance, N09 adapters,
N10 assurance, N11 migration and N12 supported operation. Use the programme index
at https://github.com/edithatogo/careops/blob/main/conductor/native-m365-roadmap.md
once merged. Track state is not a claim of native implementation.

For each native slice, link its small Decisions PR and executed tests back to the
corresponding task; do not maintain contradictory copies of the completion state.
Start with the entity/ownership and native write-boundary parity matrix, followed
by native sandbox tests, projections and one accessible renewal workflow. Do not
add another staff form or duplicate approval queue to demonstrate integration.

## Explicit remaining gates

Current signed local delegation verification; approved environment/licensing and
residency; native tables, plug-ins and permissions; standalone solution installation;
SharePoint access and retention; recovery/backlog drills against agreed RPO/RTO;
privacy/cyber/clinical/support acceptance; user-task evidence; reconciled migration.
No native deployment, backup configuration, live data import, agent activation or
CGov cancellation occurs with this documentation change.

## Microsoft primary documentation

- https://learn.microsoft.com/en-us/power-apps/teams/data-platform-compare
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/perform-conditional-operations-using-web-api
- https://learn.microsoft.com/en-us/power-platform/admin/backup-restore-environments
- https://learn.microsoft.com/en-us/power-platform/alm/organize-solutions
