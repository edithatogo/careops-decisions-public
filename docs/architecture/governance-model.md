# Governance Model

## Governance Body Hierarchy

The schema uses a single `dft_GovernanceBody` table for all forum types, with self-referencing parent-child hierarchy via `dft_parentgovernancebodyid`. This keeps the model simple while supporting complex reporting structures.

### Body Types

| Type | Usage |
| --- | --- |
| Board | Highest governance body with formal authority |
| Council | Strategic or cross-functional advisory body |
| Committee | Standing committee with defined ToR |
| Subcommittee | Committee-appointed subgroup |
| Working Group | Time-limited task-focused group |
| Project Board | Project governance body |
| Executive Forum | Operational decision forum |
| Advisory Panel | External or expert advisory group |
| Task Force | Temporary problem-specific group |

### Hierarchy Example

```
Clinical Governance Council (board)
  ├── Medication Safety Committee (committee)
  │     └── High-Risk Medicines Working Group (working group)
  ├── Clinical Quality Committee (committee)
  └── Digital Health Steering Group (project_board)
Executive Team (executive_forum)
```

## Membership Roles

| Role | Voting | Description |
| --- | --- | --- |
| chair | Yes | Presides over meetings, approves minutes, chairs decisions |
| deputy_chair | Yes | Acts in chair's absence |
| secretary | No | Manages papers, minutes, agenda, and administrative workflow |
| member | Yes | Full committee member with voting rights |
| alternate | Varies | Designated substitute for an absent member; voting rights match the member replaced |
| observer | No | Attends for information; no voting rights |
| guest | No | Invited attendee for specific agenda items |
| presenter | No | Presents papers or agenda items; not a permanent member |

### Alternate and Proxy Pattern

- An alternate is a named person (separate membership record with `isAlternate = true`) who stands in for a specific member.
- A proxy is a meeting-level assignment (`Attendee.isProxy = true`) recorded per meeting when a member designates another attendee to vote on their behalf.
- Alternate membership records can have `votingRights` matching the member they replace, or `false` if observing only.

## Terms of Reference Lifecycle

### States

| State | Meaning |
| --- | --- |
| draft | Being prepared; not yet effective |
| current | Active mandate for the governance body |
| superseded | Replaced by a newer version; retained for audit |
| archived | Historical record; body may be retired |

### Versioning Rules

- Each ToR belongs to one governance body (`dft_governancebodyid`).
- Version uniqueness is enforced by the alternate key on `(dft_governancebodyid, dft_versionnumber)`.
- Only one ToR should be `current` per governance body at any time (application-enforced, not schema-enforced).
- `dft_effectivefrom` is required; `dft_effectiveto` is set when superseded or archived.

### Timeline Example

```
Version 1.0: effective 2025-01-01 → superseded 2025-07-01 (superseded)
Version 2.0: effective 2025-07-01 → (current)
Review due: 2026-07-01
```

## Quorum Calculation

Quorum is expressed as a pair:
- `dft_quorumnumber`: the minimum count required (e.g. 5)
- `dft_quorumdenominator`: the basis for the count

| Denominator | Calculation |
| --- | --- |
| total_members | quorumNumber of all current members must be present |
| voting_members | quorumNumber of voting-eligible members must be present |
| appointed_members | quorumNumber of formally appointed members must be present |
| eligible_attendees | quorumNumber of invited or eligible attendees must be present |

Application logic should check `dft_quorumnumber` against the count of `Attendee` records where `attendanceStatus = 'attended'` and `votingEligible = true` (for voting_members denominator) or just `attendanceStatus = 'attended'` (for total_members denominator).

## Conflict of Interest

Declarations are captured on the `dft_ConflictOfInterest` table:

- `dft_conflicttype`: declared, actual, perceived
- `dft_handlingdecision`: noted, recused, approved to participate, escalated
- Linked to meeting, optional agenda item, and the declaring person

Conflicts can be declared at any time and should be minuted as part of the meeting record.

## Access Rules

The `dft_AccessRule` table records intended access intent (not live enforcement):

- Scope: body-wide, meeting-only, agenda-item-only, document-only, role-based, external-shared
- Rule type: allow, deny, review, exception
- Classification: public, internal, confidential, restricted, legal

These records inform future Dataverse role mapping, Entra group membership, and SharePoint permission decisions but are not a live security engine.
