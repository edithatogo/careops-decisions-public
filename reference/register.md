# Shared credentialing register foundation

Status: executable in-memory reference with synthetic tests. Not a database,
Power App, authenticated API, migrated register or permission to practise.
Portfolio issue: edithatogo/careops#160.

## Implemented semantics

The immutable Register appends verified human-decision revisions, preserving
history. A new revision must explicitly supersede the previous decision for that
record. Replay is idempotent; altered reuse of a decision ID is rejected. An
expected version detects concurrent changes in the reference. An existing
record cannot silently move to a different practitioner, profession or facility.
UTC-normalised instants govern replay and effective-time projection. Future
changes do not activate early; a later revocation cannot be undone by an older
scheduled revision becoming effective. There is no agent dependency.

Each record is a practitioner/profession/facility scope, not a single universal
credentialed flag. Conditions and supervision remain visible in operational
views. A read envelope states the as-of time and data-currentness status even
when no rows are returned. Stale data never becomes automatic approval or
revocation. The last recorded decision state remains visible beside uncertainty.
An application red/amber/green preparation result cannot create a register entry.

## Role-specific access

- Operational users: scope, facility, profession, dates, conditions, supervision
  and status; no decision/authority document references.
- Credentialing and audit users: the same permitted rows plus decision and
  authority references; references do not grant access to their documents.
- Practitioners: their own permitted records only.

All views also require explicit profession and facility access. There are no
wildcard or title-derived permissions. Source documents, committee papers,
complaint material, raw AI findings and confidential reasons are not in the
projection model. Detailed reviewer evidence belongs behind separate case-level
permissions and procedural-fairness controls. Do not interpret an empty result
as proof of no authority when currentness is unverified.

## Required deployment adapter and user interface

The internal model trusts an authenticated adapter, not request JSON. Do not let
users or agents construct Viewer grants, freshness leases, history or verified
human-decision flags. Verify the signed decision, decision-time delegation,
case/version hash and lawful disclosure rules before append. Build history only
through validated appends. A durable Dataverse or approved equivalent adapter
must supply atomic concurrency, append-only audit, permissions, retention/legal
hold, encryption, backups and tested restore. Never rely on client-side filters.

Provide one plain-language search/workbench, configurable profession and facility
filters, current scope and limitations, and a details panel for entitled users.
Use text labels and accessible icons as well as colour; support keyboard and
screen-reader use and test with clinical, administrative, nursing, allied-health,
dental, rural/remote and credentialing users. Reuse verified practitioner details
rather than ask for another form. Broad operational access is not public-internet
publication. Temporary coverage, notification and release controls need their own
approved pathways. Authoritative register writes remain disabled until approval.

## Acceptance before replacement

The reference tests cover replay, altered duplicates, concurrency, supersession,
identity boundaries, chronology, future events, expiry, suspension/revocation,
role and row restrictions and stale/empty data. Integration qualification must
also cover forged receipts/claims, authorised correction, multi-site scope,
longitudinal migration reconciliation, durable failures/retries and restore.
Maintain original documents and decision-time evidence in approved records
storage. Human operational acceptance and clinical/legal/security sign-off are
separate from source CI. Do not cancel the legacy service until export rights,
full reconciliation, support, rollback and a funded cutover decision are complete.
