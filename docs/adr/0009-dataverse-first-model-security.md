# ADR 0009: Dataverse-First Model And Security

## Status

Accepted.

## Context

Decisions Workspace manages governance meeting records that span multiple
classification levels (public, internal, confidential, restricted, legal).
Security must work before any Teams, SharePoint, or Entra integration is
available. The MVP must run on Dataverse for Teams or full Dataverse without
requiring Entra groups, Graph permissions, or SharePoint advanced security.

## Decision

Design the data model and security model Dataverse-first. All tables,
relationships, security roles, access teams, and access rules are defined
in Dataverse. Entra groups, SharePoint permissions, and Graph security
are deferred enterprise additions that should map onto the Dataverse model,
not replace it.

Key design principles:

1. **Security roles define base privileges** per role (Administrator,
   Secretary, Chair, Member, Author, Restricted Reader, Auditor, Guest
   Presenter, Service Account). These are the primary security boundary
   for MVP.

2. **Access teams provide committee-scoped access.** When a person is added
   as a member of a governance body, an access team membership grants them
   the Member role scoped to that body. This replaces per-user role
   assignment for committee members.

3. **Access rules record intended access but are not enforcement.** The
   `dft_AccessRule` table documents who should see what, but enforcement
   requires Dataverse security roles and access teams. Full rule enforcement
   requires Entra groups and SharePoint permissions.

4. **Record classification is metadata, not enforcement.** The
   `dft_classification` column on meetings, papers, minutes, and transcripts
   records the intended access level. Dataverse security roles determine
   actual access.

## Consequences

- MVP security works without Entra, Graph, or SharePoint advanced security.
- Security role matrix must be implemented in Dataverse before shared test
  deployment.
- Access team auto-provisioning must be configured when CommitteeMembership
  records are created.
- Future Entra groups can map to Dataverse teams for easier membership
  management, but the core model does not depend on Entra.
- Access rules are design documents until the enterprise path provides
  enforcement tooling.
