# ADR 0005: Graph, Azure, And Entra Deferred Until Access

## Status

Accepted.

## Context

Future capabilities need Graph, Azure, and Entra access for transcripts, attendance, directory groups, app registrations, managed identities, and service principals. Current implementation should work in a Power Platform development space without those privileges.

## Decision

Design Graph, Azure, and Entra integration now, but do not introduce live dependencies until tenant access, permissions, and approvals exist.

## Consequences

- MVP identity and hierarchy use Office 365 Users connector where available.
- Azure/Entra tracks are design-gated.
- Dataverse fields can store future object IDs and sync metadata without requiring live Graph implementation.
