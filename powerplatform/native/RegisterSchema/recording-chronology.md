# Recording chronology in the native journal

24 September 2026. N03.03/N03.04 source follow-through after Decisions#21
(acc135465bb0007f03256267c12da1628c515f98). Parent track remains open.

A new journal entry now checks the previous entry's recorded instant before its
first write. The value must be present, an explicit UTC DateTime, and no later
than the new recorded instant. Equal instants are allowed: per-record sequence
is the tie-breaker, avoiding invented clock precision. Missing, access-filtered,
malformed, non-UTC or reversed chronology stops staging without changing any row.

Recording chronology is not effective-date policy. A decision may legitimately
record a past or future legal effect where the approved pathway permits it; the
storage adapter neither invents that permission nor clamps effect to recording.
Exact historical retries remain read-only and do not read/rewrite the current
predecessor, scope, limitations or supervision. Existing source tests for those
invariants remain intact; new tests cover missing/null/wrong-kind and one-tick
boundaries, equal timestamps and independent effective dates.

The future protected host must set recording time from a trusted server source
and bind the original value for exact retries. This adapter's check is not clock
authentication and cannot verify a caller-supplied timestamp. No timestamp is
silently rounded, timezone-converted or replaced with the wall clock. Native
storage fidelity, date ranges and precision require an approved sandbox test.
The no-admin synthetic pilot remains independent and unchanged.

The existing CI compiles .NET Framework source, executes SDK/fake-service tests,
and retains whole-library 100% line/branch coverage and locked dependencies.
No local .NET execution or actual Dataverse rollback is claimed. The protected
endpoint, native security, source verification and operational acceptance remain
open. A failure must escape the native transaction; there is no retry, message
send, corrective write or compensation within it.

Primary Microsoft references checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/scalable-customization-design/database-transactions
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/behavior-format-date-time-attribute
