# Transactional local register storage

Status: executable local/synthetic adapter; not approved for real practitioner
records or tenant deployment. Continues [CareOps #160](https://github.com/edithatogo/careops/issues/160)
and builds on the strict codec in [Decisions #10](https://github.com/edithatogo/careops-decisions-public/pull/10).

## What changed

`register_store.py` persists the existing register, rather than inventing a second
decision model. It uses Python's standard-library SQLite interface; no runtime
dependency is added. Existing authority and decision checks are not replaced.

- `SQLiteRegisterStore.initialize(path)` exclusively creates a new local store.
- `SQLiteRegisterStore(path).load()` opens an existing store and validates history.
- `append(revision, expected_version=n)` commits one verified decision atomically.
- `view(viewer, at, current_until=lease)` reuses the existing role-scoped projection.

Ordinary opens cannot create a missing database. Initialization cannot overwrite
an existing file. A failed initialization is not silently repaired or retried:
the host must investigate the incomplete file. Paths and directory permissions
are trusted host configuration, not applicant or agent input.

## Transaction and history controls

Each operation opens its own connection and closes it deterministically. Writes
use `BEGIN IMMEDIATE` and explicit commit/rollback with full synchronous writes;
reads validate one transactional snapshot. The event and its checkpoint commit
together. Contention fails visibly with no hidden retry loop. After a verified
re-read, the host can retry with the current version. The host must keep that
technical work out of the practitioner's evidence-correction queue.

A lost response can be retried with the exact same decision without creating a
duplicate. An old replay cannot undo a later revocation. Altered reuse of a
decision identifier and unsafe supersession are rejected. UTC timestamps,
conditions, supervision and complete decision history survive reopening.

Strict JSON, sequence validation, a chained SHA-256 digest and a count/head
checkpoint detect malformed or inconsistent stored history. Unknown schemas,
missing terminal events and mismatched identifiers fail closed; there is no
implicit migration or destructive recovery.

The digest is an integrity check, NOT an authenticated signature. Someone able
to rewrite the database and checkpoint can rewrite the chain. Whole-file rollback
to an older internally consistent copy also requires external trusted generation/
checkpoint evidence to detect. Storage success never refreshes source-currentness
or proves legal authority. Stale views retain the recorded decision state and
report currentness uncertainty rather than manufacturing revocation.

## User and agent boundaries

There is no new form, queue, notification, approval or mandatory staff task.
The register is a reusable component behind the intended single workbench.
Raw `load()` history is for the protected service, not a general viewer endpoint.
Viewer roles, profession/facility access, currentness leases and exact-decision-
bound human authority receipts must come from authenticated services. Never let
an applicant, browser or Copilot agent supply those trusted claims directly.
Operational views omit decision/authority references while retaining scope,
limitations and supervision. Case papers and agentic findings are not stored here.

Only synthetic records are used by tests. Local database, journal and Python
qualification outputs are excluded by `.gitignore`; that is accidental-staging
protection, not a security boundary against forced commits or data disclosure.

## Qualification

23 new test methods exercise real local SQLite, not a mocked persistence service:
restart and round trips; two simultaneous writers and optimistic conflicts; write
locks and failed commits; atomic rollback after checkpoint failure; interrupted
uncommitted changes; exact/altered replay; revocation preservation; corrupt/missing
history; incompatible files; schema/type checks; SQL metacharacters; and existing
profession/facility/role/currentness boundaries.

Local Python 3.13.5: the new store's 77 statements and 18 branches are all covered.
Together with the codec, 36 new methods pass and cover 133 statements/40 branches.
The two-writer regression also passed 100 consecutive local repetitions. Existing
strict lint, mypy, full-reference 100% statement/branch coverage on Python 3.13 and
3.14, and repository Validate must pass on the final head before merging. No
threshold or existing test is relaxed. These results are not a clinical safety,
independent security, fault-tolerant hosting or production readiness assessment.

## Production work still required

This local adapter establishes a tested persistence contract, not a choice of
CHHHS production database. It currently replays all history on every operation;
indexed projections, capacity testing and production transaction design remain.
A production deployment needs approved hosting and identity/access enforcement,
exact-decision authority receipts, encryption/key management, retention controls,
verified backup/restore and rollback detection, audited source-currentness,
operational support, accessibility-tested workbench, and reconciled migration.
No live connection, register activation, roster/access write, agent deployment,
contract cancellation or CGov cutover occurs in this change.

## Primary technical references

- [Python sqlite3 transaction control](https://docs.python.org/3.13/library/sqlite3.html#transaction-control)
- [SQLite transaction semantics](https://www.sqlite.org/lang_transaction.html)
