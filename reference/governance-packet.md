# Fictional governance packet reference

Status: source-only reference contract. It does not implement a meeting app,
record store, approval, document generator, or external provider.

`governance_packet.py` checks agenda, minutes, decision and action identity links
for one synthetic meeting packet. A decision must point to an agenda item and an
exact minutes revision; an action may link to a decision only when both belong
to the same agenda item. Duplicate IDs and missing or cross-meeting links fail
closed. The validator is pure and retains the original immutable records.

The minutes selector protects a currently human-edited revision from a newer
candidate. It returns the selected immutable value and a disposition for a
caller to display; it performs no update. The optional snapshot contract checks
only provider identity, contract version, meeting identity and snapshot
freshness. It ignores source content. The retry classifier compares an
idempotency key and content digest and reports a duplicate or conflict without
performing a write or external effect.

The synthetic tests cover absent, missing, incompatible and stale optional
snapshots, exact duplicate retries, conflicting reuse, invalid links, duplicate
identities and human-edit preservation. No clinical, personnel, patient,
committee or tenant records are included. These checks do not establish native
Power Apps readiness, SharePoint output, approval authority, persistence,
interoperability with Clinical Hub, or operational acceptance.
