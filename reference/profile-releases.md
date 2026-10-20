# Editable delegation profiles with exact-version approval

Status: executable, synthetic release-control reference. No live delegation,
tenant configuration, legal instrument or practitioner decision is changed.
Related work: edithatogo/careops#160; `reference/delegations.py` and
`config/chhhs-delegations.candidate.json` remain the starting points.

## Implemented control

An administrator can prepare a new Profile with function, role, profession,
facility, pathway, conditions and effective dates as data. `publish_profile`
accepts only an unapproved draft, a separately verified approval receipt and a
valid nonempty authority configuration. The approver must differ from the editor.
The canonical digest binds approval to the exact editable content, including
conditions and committee independence. Changing any bound field needs another
approval. Canonically identical set ordering or timezone representation does
not manufacture a new substantive revision.

Publication creates a new immutable in-memory snapshot, leaving the draft and
previous release unchanged. At each decision, `check_released_authority`
revalidates the release content/receipt, source reference, verification interval,
approval metadata and the existing decision-time authority predicate. This
rechecks role occupancy, conflicts, function, profession, facility, pathway and
conditions. A stale or altered release stops that action, not previously granted
scope or an independently authorised human safety pathway.

A valid source hash is not proof that an instrument is genuine, current or
applicable. Hashes bind content; they are not signatures or legal delegations.
The protected adapter must authenticate the approver, establish their authority
to approve configuration, verify the actual instrument and bind all receipt
fields to the exact profile and source before setting `verified`. A client,
agent or ordinary editor cannot supply a self-asserted verified receipt.

## CHHHS configuration

The existing candidate JSON indexes section 43 of the available CHHHS manual,
effective 29 October 2024, without publishing the internal document. It retains
separate functions for committee appointments, individual scope, temporary
scope, adverse changes and the post-appeal decision. It is not an executable
grant. The current controlled manual, separate credentialing instrument and
relevant profession/procedure mappings must be reconciled before activation.
The candidate's visual table-condition verification remains outstanding.

Do not infer EDMS/CMO equivalence, cross-profession powers or onward delegation
from a title or organisational chart. Approved role mappings and verified acting
occupancy are separate evidence. Map the ordinary independence condition and any
authorised immediate-safety exception as distinct human pathways. A generic
urgent checkbox or agent finding cannot override the ordinary condition.

## Administration and operating requirements

Provide a restricted form with a plain-language change comparison, instrument
reference, proposed dates and a separate approver. Preserve the approved content,
test results and approval receipt in the controlled record. Only changed
configuration needs this approval; ordinary cases do not gain a second approval
step. Editors maintain data, not source code or agent prompts.

This module is not an administration screen, authentication service, persistent
release registry or cryptographic verifier. The hosting service must enforce
schema validation, signed/verified receipt binding, canonical identity matching,
atomic version updates, release selection/revocation and protected audit storage.
It must reject replayed approvals for altered content and stale concurrent edits.
Do not let callers choose historical releases to regain withdrawn authority.
Explicitly approved rollback is a new controlled event; it cannot silently
restore withdrawn powers. Live deployment remains separately authorised.

Before operational use test forged sessions/receipts, revoked releases, title
changes, acting-date boundaries, self-approval, conflicting grants and recovery.
User testing must include the staff who maintain delegations and those who rely
on them. Repository CI and synthetic coverage are not a legal or clinical
certification. The all-professions register and check workers stay independently
usable when an optional AI preparation service is unavailable.
