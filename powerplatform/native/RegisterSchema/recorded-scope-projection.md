# Recorded-scope projection, without automatic clearance

24 September 2026. N03.03/N03.08 source slice, after Decisions#23 at
70a091ba26232618b15ade3a33eb3d5152ec4c3d. No deployment or policy change.

RecordedScopeProjection.Project checks a complete ordered history for one stable
record and selects the last SEQUENCE whose recorded and effective instants have
arrived. It does not sort by effective date or assume the latest recorded pointer
is already effective. An older future grant cannot revive after a later revocation.
An expired selected grant cannot fall back to an earlier unexpired grant. A later
explicit grant may supersede a recorded restriction only as an already recorded
human decision; this code does not authorise that decision.

The reducer validates complete contiguous sequences, exact record/predecessor IDs,
recording chronology, supported non-default outcomes, explicit UTC dates and all
scope/conditions/supervision values. Unknown or access-filtered restrictions are
not inferred to mean none. Empty explicit restriction text remains distinct from
missing text. Real native empty/null and timestamp fidelity still need sandbox
qualification and an approved storage representation, not a permissive fallback.

Input is bounded to 256 entries per record as an engineering workload limit.
MoreRecords, count mismatch, missing history, broken chains and unrecognised data
fail visibly; no truncated result is presented as complete. Larger histories need
separately qualified bounded pagination/checkpoint support, not a clinical cap.
This function accepts no service client and makes no reads, writes or messages.

Output keeps recorded status and source currentness SEPARATE. An expired/missing
verification interval cannot hide a known recorded revocation or fabricate a new
revocation. The interpretation always remains recorded-data-only-not-practice-
clearance. Dates use inclusive starts/exclusive ends. No clock, timezone, legal
duration, professional competence or service capability is inferred.

The result freezes a minimum-column snapshot and returns fresh copies of mutable
SDK values. Extra attributes, formatted values, actor/case/authority papers and
related entities never pass through. It is not a signature, credential check,
read authorisation or a materialized register update. A future host must use
caller-scoped data, verify complete consistent history against the root version,
keep immutable history protected and obtain current source evidence. Partial
caller visibility cannot be treated as the complete history.

Native source tests use the existing locked SDK test project, covering the whole
library with warnings-as-errors, nonempty/no-skip tests and 100% line/branch
coverage. No local .NET or actual Dataverse execution is claimed. Native protected
reads/writes, Power Apps presentation, scheduled changes, storage fidelity and
clinical/cyber/operational acceptance remain open. No new staff queue or approval
step, no Entra app registration, no live record change. The no-admin Lists/Flow
rehearsal remains independent and unchanged.

Microsoft primary guidance checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-retrieve
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/queryexpression/page-results
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
