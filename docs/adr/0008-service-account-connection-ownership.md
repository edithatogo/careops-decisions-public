# ADR 0008: Service Account And Connection Ownership Strategy

## Status

Accepted.

## Context

Power Automate flows require connection references to Outlook, Teams,
Planner, SharePoint, Office 365 Users, and AI Builder. If a connection is
owned by a personal user account, the flow breaks when that user leaves the
organisation or their password changes. For shared environments, the app
needs a connection ownership model that survives individual departures.

## Decision

Use a named development connection for local development and testing. Use
dedicated service accounts or shared mailbox-based connections for shared
test and production environments where tenant policy allows.

The order of preference for connection ownership is:

1. Service account with a dedicated Power Platform license (preferred for
   production).
2. Shared mailbox with application permissions (where supported by the
   connector).
3. Named user account with documented ownership and rotation plan (MVP only;
   not for production).

## Consequences

- Service accounts must have Power Platform licenses and the minimum
  Dataverse permissions (Service Account role) needed for their flows.
- Connection references must be updated in deployment settings when
  migrating from development connections to service account connections.
- Service accounts must be documented in the service-account-strategy.md
  with owner, purpose, and review date.
- Until service accounts are created, MVP flows use named development
  connections and are flagged as pre-production.
- The `dft_IntegrationLog.dft_actor` field records which connection or
  service account executed each operation.
