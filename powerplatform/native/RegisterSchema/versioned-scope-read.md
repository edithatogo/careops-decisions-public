# Native version-guarded recorded-scope read

24 September 2026. N03.07/N03.08 source, stacked on Decisions#24 at
394845ecad88fc9967163da9808ad4bc2e7cfb9c. Full N03 remains in progress.

VersionedScopeReader.Read is a lower-level SDK adapter for a protected direct
read handler. It checks execution context and WhoAmI consistency, reads the root
marker, retrieves a bounded complete journal, computes the recorded-scope view,
and reads the root marker again. Both native row version and relevant scalar
fields must match. Changed or invalid markers discard the result, without retry,
write, fallback to cached scope or escalation to a privileged identity.

The history query uses exact record ID, the reducer's minimum field allowlist,
ascending sequence and primary-key order, and TopCount 257 to detect the current
256-entry engineering bound. It does not filter away future, expired or adverse
entries. Missing/restricted rows and fields, MoreRecords, total-count overflow
and incomplete sequences fail, not an apparently clean older history. No batch,
multiple-page unbounded loop, unrelated table or actor/case evidence is read.

The first marker and computed view are frozen before the second read so reused
mutable SDK objects cannot alter the comparison retrospectively. The output
contains its checked native root version and the immutable recorded-data view.
Recorded state, conditions and supervision remain separate from source currentness.
A successful read is not permission to practise, credential verification or a
promise that nothing changes after return. Evaluation time is supplied by the
trusted host; do not accept arbitrary client time as the current server time.

## Consistency and security boundary

Double-reading a version marker detects concurrent protected journal/root changes.
It is NOT snapshot isolation or proof against unauthorized direct edits to journal
rows. Its safety depends on immutable history, atomic append/head updates, native
version behavior and prevention of direct CRUD bypasses. Those remain actual
Dataverse qualification gates. The supplied service must originate from the
current platform service factory under the authenticated caller. WhoAmI equality
alone does not prove a human identity, approved role or transaction participation.

The final IPlugin/Custom API must invoke the existing caller resolver, enforce
its read contract and return generic failures without disclosing whether an
inaccessible record exists. This slice neither registers an endpoint nor grants
roles. Partial caller access must not cause an elevated re-read to make the result
complete. Appropriate operational projection permissions and source-reference
protection need local approval and native access/export tests.

No caching, materialized update, future-state scheduler, flow, notifications or
new staff queue is installed here. An on-demand read evaluates all time boundaries
at its host instant. Any future cache must invalidate on journal/source changes
AND time boundaries. The no-admin Lists/connector-free Flow pilot is unchanged,
independent and still synthetic. No Entra app registration or tenant change occurs.

The existing locked .NET test workflow checks exact SDK request shape, relevant
marker changes, reused objects, invalid contexts, missing history and faults at
every service stage. Fake-service tests prove these mechanics, not actual tenant
permission or concurrency behavior. No threshold or dependency is changed.

Microsoft primary sources:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/queryexpression/page-results
