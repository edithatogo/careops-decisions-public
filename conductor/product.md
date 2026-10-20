# Product Definition

## Product

Decisions Workspace is a repository-first Power Platform governance and
committee-management system. It is intended to support governance bodies,
meetings, attendees, papers, minutes, decisions, actions, transcripts, feature
controls, and operational reporting.

## Delivery Model

- Dataverse is the intended system of record.
- Power Apps provides the meeting and governance user experience.
- Power Automate provides bounded Microsoft 365 integrations.
- PowerShell, solution source, contracts, and documentation in this repository
  define the reproducible ALM surface.
- The hosted `main` branch and completed hosted checks are the authoritative
  repository baseline.

## Current Evidence Boundary

Hosted `main` currently proves a validated repository scaffold and packable
solution surface. Local, uncommitted claims concerning a Dylan v1.0.0 export,
import, publication, or live configuration remain provisional until their
source changes pass repository validation, packaging, review, and hosted CI.

Teams, SharePoint, Planner, Microsoft Graph, AI Builder, solution checker, and
tenant activation are environment-, credential-, licence-, permission-, or
human-approval-gated. Repository completion must never be represented as live
activation without a corresponding external receipt.

## Users

- Committee secretariats and administrators
- Chairs and governance leads
- Committee members and invited participants
- Platform, security, records, and support teams
- Solo maintainers responsible for repository and ALM quality

## Outcomes

1. Provide auditable, consistent governance-meeting workflows.
2. Keep solution source, contracts, validation, and operational guidance
   reproducible from the repository.
3. Preserve human review for minutes, AI-assisted outputs, approvals, and
   consequential governance records.
4. Make readiness and deployment claims evidence-based and fail-closed.

## Non-Goals

- Treating scaffolds, dry runs, or local files as proof of tenant deployment
- Bypassing organisational permissions, licensing, security, records, or
  change-control requirements
- Requiring teams, CODEOWNERS, or mandatory human approvals for routine
  repository maintenance by a solo maintainer
