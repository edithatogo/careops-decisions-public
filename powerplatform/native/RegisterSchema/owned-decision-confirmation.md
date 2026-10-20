# Owner-bound journal creation and exact human-confirmation content

25 September 2026. N03.04-N03.06 source after Decisions#35 at
beeb0fd1ba7062e7005c61d2bddd71e39c766558. No tenant deployment.

The explicit-owner DecisionAppend overload freezes separate decision and event
owning-team identifiers before creation. The existing journal appender persists
ownerid with the decision; JournalWithOutbox does the same for the event in the
same transaction-bound service. It never creates then reassigns an immutable row,
never silently assigns evidence to the acting clinician, and never copies the
operational register's audience to the confidential history by assumption.
The two owners may differ; approved access design selects them, not a clinician.
The protected host still must resolve/verify actual owner-capable teams, local
access policy, caller permissions and approved ownership configuration.

The older unowned constructor remains solely for the existing synthetic storage
fixtures. It cannot obtain a confirmation digest and is not permitted in the new
protected composition. No remotely exposed write endpoint exists in this slice;
low-level staging is not itself authentication or permission to use either shape.
Exact owned retries compare ownerid too. A changed/missing owner is a reconciliation
failure, never a reason to assign, repair, resubmit or repeat a human decision.
No old unowned history is silently promoted to the new owner-bound shape.

DecisionConfirmationHash.Compute binds the organization, complete frozen decision,
record, exact predecessor, expected root version/sequence and BOTH owner IDs. A
case/evidence hash alone does not protect a changed scope, removed condition or
changed recipient team. This is a domain-separated content digest, NOT a signature,
proof of human action or delegated authority. A protected confirmation record must
retain the digest of the exact content shown/confirmed in the existing decision
step. Recomputing a digest after changing fields cannot substitute for that record.
Digest ownership fields come from separately approved access configuration; staff
must not be required to choose technical team IDs during every case review.

## Native-v1 binary contract

Uses .NET BinaryWriter little-endian integers/GUID.ToByteArray and strict UTF-8
strings with its 7-bit length prefix. Header: version string, organization GUID,
entity logical name and decision GUID, expected native version string, expected
sequence Int32, event owner GUID and field count Int32. Every sorted ordinal
field writes its name then tag/value:0 string,1 Int32,2 UTC ticks Int64,3 logical
name plus GUID for a reference,4 Int32 option. The decision's ownerid, parent,
sequence and optional predecessor are included as fields. Every value is validated
and defensively frozen by DecisionAppend. Independent fixture:911 bytes, digest
4ec6ae54b5b62f30e3ee44f0d81122eb9b574734782c300983bd592ccd00895e.

Tests cover every payload field, both owners, environments, dates at tick precision,
sequence/predecessor/version changes, stable ordering/culture, immutable snapshots,
actual composed SDK staging and read-only replay. Existing tests and locked/full
coverage gates remain. This is SDK/fake-service qualification, not native ownership,
Assign privilege, storage precision or rollback evidence. The actual confirmation
source, current delegated authority and public protected writer remain to be bound.

No new staff queue, approval, form, platform connection, app registration or tenant
setting. No-admin Lists/Flow/browser pilots stay independent. Full Dataverse remains
the target; current signed CHHHS instruments and N03 native acceptance remain open.

Microsoft source checked25 September2026:
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-access-teams-owner-teams-collaborate-share-information
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/scalable-customization-design/database-transactions
