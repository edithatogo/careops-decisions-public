# Enterprise Upgrade Path

## Purpose

Track 07 defines the future Azure, Entra, Graph, Teams extension, Copilot, and Power BI/Fabric path without making those services required for the MVP. The core Dataverse model remains the system of record; enterprise services add automation, identity alignment, analytics, and operating controls around it.

## MVP Boundary

The MVP must remain usable with Dataverse, Power Apps, Power Automate, and standard Microsoft 365 connectors. These are design-only until tenant access, admin approvals, connector references, and governance sign-off exist:

- Entra app registrations and service principals.
- Microsoft Graph calendar, Teams meeting, transcript, attendance, group, user, and file APIs.
- Azure Functions, Logic Apps, Key Vault, Application Insights, and managed identities.
- Teams meeting extensions, bots, and app-only tab automation.
- Copilot Studio, custom connectors, or HTTP with Azure AD.
- Power BI/Fabric semantic models and enterprise report workspaces.

## App Registration And Permissions

Create separate registrations for delegated user actions and app-only background automation. Do not reuse a maker's personal connection as the enterprise integration identity.

| Workload | Permission Shape | Notes |
| --- | --- | --- |
| Calendar and meeting sync | Delegated first; app-only only after approval | Used for stronger attendee response handling and event reconciliation. |
| Teams meetings and attendance | Graph application permissions likely required | Requires explicit admin consent and clear retention rules. |
| Transcript retrieval | Graph transcript permissions | Manual transcript import remains the fallback and source-compatible path. |
| Group and user sync | Directory read permissions | Cache stable Entra object IDs into `dft_OrgPerson` and related governance records. |
| SharePoint file metadata | Sites selected or equivalent constrained access | Store drive item IDs and retention metadata against paper/minute records. |
| Background jobs | Managed identity or service principal | Use certificate or managed identity secrets, not user passwords. |

All permissions must pass the Future Azure / Entra Gate in `docs/readiness/implementation-readiness-gates.md` before implementation.

## Graph Upgrade Workloads

1. Calendar events: reconcile meeting dates, attendees, responses, and external event IDs.
2. Teams meetings: create online meeting metadata, link meeting chat/channel context, and capture attendance reports.
3. Transcripts: retrieve transcript files or text when the meeting policy and permissions allow it.
4. Identity: refresh people, managers, departments, and Entra object IDs into Dataverse.
5. Groups: map Entra groups to governance bodies, access rules, and Dataverse teams.
6. Files: resolve SharePoint drive items, versions, classifications, and retention metadata.

Every Graph write or sync writes a `dft_IntegrationLog` row with source record, target system, external ID, status, correlation ID, retry count, and error details.

## Azure Runtime Pattern

Use Azure only when Power Automate and standard connectors are insufficient.

- Azure Functions or Logic Apps for app-only Graph jobs, transcript retrieval, and batch reconciliation.
- Managed identity where supported; otherwise certificate-backed service principal credentials in Key Vault.
- Application Insights for operational telemetry and correlation IDs.
- Queue-based retry for throttled Graph operations.
- No direct Azure dependency in canvas app screens; app commands continue to write Dataverse state and read integration logs.

## Access Control Mapping

Enterprise access aligns four layers without making any one non-Dataverse system authoritative:

| Layer | Source | Dataverse Contract |
| --- | --- | --- |
| Entra groups | Security and membership groups | Cached group IDs and membership refresh metadata. |
| Dataverse teams and roles | Environment security | Roles from `docs/security/security-role-matrix.yaml`. |
| SharePoint permissions | File access | Paper/minute access intent and external item IDs. |
| Teams/Planner membership | Collaboration access | Integration log visibility and exception reporting. |

Manual app access rules remain the fallback when group sync is unavailable or stale.

## Reporting Upgrade

Dataverse views and app dashboards remain the first reporting surface. Power BI/Fabric becomes the enterprise layer after stable Dataverse data and security roles exist.

- Use Dataverse tables as facts and dimensions, not Planner or SharePoint as primary facts.
- Apply row-level security from Dataverse role, team, and access-rule concepts.
- Keep metric definitions aligned with `docs/architecture/governance-reporting.md`.
- Do not publish a Fabric/Power BI semantic model until the source tables, relationships, and retention rules are stable.

## Implementation Gates

Before enterprise implementation starts:

1. Tenant admin approvals are recorded.
2. App registrations and Graph permissions are approved.
3. Azure RBAC and subscription boundaries are known.
4. Service principal or managed identity ownership is assigned.
5. DLP, retention, monitoring, and support impacts are reviewed.
6. Entra group-to-Dataverse team mapping is documented.
7. Service principals have least-privilege Dataverse roles.

Until those gates pass, Track 07 is complete as a design boundary and must not introduce live Azure, Entra, Graph, or Power BI runtime dependencies.
