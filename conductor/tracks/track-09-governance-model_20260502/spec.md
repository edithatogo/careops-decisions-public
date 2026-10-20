# Track 09: Governance Model

## Goal

Model governance bodies, terms of reference, membership, authority, quorum, and reporting lines in a hierarchy that supports NSW Health committee structures.

## Scope

- Boards, committees, subcommittees, working groups, and parent-child governance body hierarchy.
- Terms of Reference: versioned mandate, effective date range, review cycle, quorum rules, and approval status.
- Membership roles: chair, deputy chair, secretary, member, alternate, observer, guest, presenter.
- Quorum calculation: numerator (minimum count) paired with denominator basis (total members, voting members, appointed members, eligible attendees).
- Alternate and proxy membership patterns including voting-rights delegation.
- Conflict of interest declarations linked to meetings, agenda items, or individuals.
- Access rules expressing intended scope and classification per governance body, meeting, or document.

## Acceptance Criteria

- Governance concepts are represented in the Dataverse model with one GovernanceBody table supporting all forum types.
- A realistic governance hierarchy can be seeded: parent body, multiple committees, subcommittees, and working groups.
- Terms of Reference support version history: multiple versions per body with effective date range, review due date, and superseded/archived states.
- Quorum can be expressed as a number and denominator type for each ToR.
- Membership supports all roles (chair, secretariat, member, alternate, observer) with start/end dates, voting rights, and alternate flags.
- Committee hierarchy and org hierarchy are related but stored in separate tables.

## Out of Scope

- Dynamic role-based access enforcement (future Dataverse or Entra path).
- HR system sync for membership (future enterprise path).
