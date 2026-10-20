# ADR 0001: Dataverse As System Of Record

## Status

Accepted.

## Context

Decisions Workspace needs structured governance records, auditable relationships, security roles, and future reporting. SharePoint and Planner are useful collaboration systems, but they should not be the authoritative register for meetings, decisions, actions, minutes, or audit logs.

## Decision

Use Dataverse as the canonical system of record for governance bodies, terms of reference, meetings, attendees, agendas, papers, minutes, transcripts, decisions, actions, votes, conflicts, AI prompt runs, access rules, and integration logs.

## Consequences

- Planner, Outlook, Teams, SharePoint, AI Builder, Graph, and Azure integrations must write external IDs and sync state back to Dataverse.
- Reporting and security design should start from Dataverse tables and relationships.
- Migration from Dataverse for Teams to full Dataverse should preserve table concepts.
