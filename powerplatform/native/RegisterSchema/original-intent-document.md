# Persist the original confirmed intent, not a later reconstruction

25 September 2026. N03.04-N03.06 source after Decisions#39 at
ad5a0ccf0bd1a02f094732553e36b556c2a30c68. No tenant change.

DecisionIntentDocument serializes the original owner-bound DecisionAppend into
an explicitly versioned, bounded document. Restore requires the independently
retained original DecisionConfirmationHash and expected organization. It reconstructs
the SAME sealed intent and compares its full digest, rather than using a case-only
hash or recomputing a fresh confirmation after a field changed. Both owners,
record/decision/predecessor IDs, expected native version/sequence, all clinical
content and exact UTC instants are preserved. This is a persistence codec for the
protected source adapter, NOT a public request parser or evidence of human action.

The host must read the original digest from a protected confirmation record. If
an attacker can replace both the document and its expected digest, this codec
cannot establish authenticity. It accepts no actor-role override, source-verified
Boolean, arbitrary SDK type or object deserializer. Duplicate/missing/unknown/case-
variant properties, noncanonical identifiers, numeric coercion, invalid UTF-16,
non-UTC/changed timestamps and oversized/deep documents are rejected. Errors do
not return clinical snippets or inner parser messages. Normal JSON formatting and
property order are immaterial; exact decoded business content is not normalised.

Maximum document size is 1 MiB UTF-8. Existing sealed field limits bound even fully
escaped Unicode output below that limit. Explicit empty restrictions remain empty;
null/missing restrictions cannot silently mean unrestricted scope. Stored JSON
avoids relying on a Dataverse DateTime column to preserve every original timestamp
fraction, but actual native field/storage fidelity still requires tenant tests.
The original digest format is unchanged. No package or workflow gate is changed.

Next source adapter work must persist/read this document, the ORIGINAL confirmation,
canonical identities and immutable source/version references through protected
native records. Approved write-only/capture routes, field/row security, original
human confirmation and transactional source-withdrawal checks remain necessary.
This code does not create records, authorise arbitrary snapshots, register an API,
change source status or invoke the writer. It adds no user form or approval step.

Six useful Copilot tasks were assigned in parallel for this delivery. GitHub
reported insufficient GitHub AI Credits before sessions could start. No output or
independent review from those agents is claimed. Work continued directly; no
billing or budget settings were changed. Those tasks remain explicitly blocked,
not reported as completed agent work.

Tests execute the actual codec and sealed native intent/hash classes, including
original-digest round trips, all fields, identity/owner changes, Unicode, exact times,
parser ambiguity and bounds. They do not prove Dataverse, source authenticity,
clinical authority or operational acceptance. The no-admin M365/browser pilots
remain independent and unchanged; full N03 remains in progress.

Primary Microsoft sources checked 25 September 2026:
- https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonwriter
- https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocumentoptions
- https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement.enumerateobject
