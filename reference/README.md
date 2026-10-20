# Credentialing delegation reference

Status: executable synthetic reference, not a production authority service.
Portfolio issue: edithatogo/careops#160. Existing governance contracts and tenant
settings are unchanged. Runtime connectors, JSON schema validation, identity and
approval verification, persistence and administration screens are separate work.

`delegations.py` checks an already verified authority configuration at decision
time. It is independently callable without AI, intake, reporting or register UI.
The result permits one function; it neither decides scope nor records approval.

## CHHHS starting point

`config/chhhs-delegations.candidate.json` indexes section 43 of the available
CHHHS HR sub-delegations manual reference copy, effective 29 October 2024.
The six functions distinguish committee appointments, individual and temporary
scope, adverse decisions, and scope after appeal. The source also identifies a
separate credentialing instrument. The candidate has no executable grants and is
inactive. It cannot certify the current controlled instrument. Table condition
spans require visual verification; no source PDF or operational information is
published here. Band 4 role candidates must be mapped to the appropriate
profession and function, never assumed to grant every listed officer every power.

For implementation reconcile the current signed instruments, current manual,
July 2026 medical/dental procedure, relevant profession schedules and committee
terms. The reference copy records the post-appeal function at HSCE level. Do not
route appeals to the ordinary delegate without current authority. The documented
immediate-safety suspension exception needs its own authorised human pathway and
subsequent formal review; a generic urgent flag must not override independence.

The upstream Queensland directive requires HHS delegations to be reflected in
the HHS HR delegation manual or instruments as amended:
https://www.health.qld.gov.au/system-governance/policies-standards/health-service-directives/credentialing-and-defining-the-scope-of-clinical-practice

## Editable, but governed

The administration workbench must support draft, reviewed, approved, effective,
superseded and revoked revisions. Editors cannot approve their own revision.
Record authority instrument, clause, source hash, scope, approver, reason,
effective interval, test evidence and release hash in protected storage.
Versioned grants, role mappings, conditions, profession/facility/pathway scope
and acting periods are data, not source-code constants. Editing a row does not
create legal authority. Authentic approval must be bound to the exact version.

Evaluate identity, position occupancy, delegation and applicable conditions again
at the moment of decision. An Entra group, job title, workflow assignment or
committee recommendation is not a delegation. EDMS-to-CMO title continuity must
be confirmed, not inferred. No wildcard matching or automatic onward delegation
is implemented. A permitted grant must match exactly once. Unknown conflict or
required committee-participation evidence fails closed for the action.

Protected adapters must validate raw inputs and obtain verification flags,
occupancy and conditions from authenticated trusted services. Do not accept
these internal dataclasses or flags directly from a user, agent or public API.
An expired/ambiguous grant stops the automated action and requests an authorised
human resolution. It does not retrospectively cancel prior decisions or prevent
an independently authorised urgent patient-safety process.

## Tests and operational gates

The new small pure kernel has strict type/lint CI, 100% statement and branch
coverage, negative authority, missing evidence, self-approval, conflict, timing,
acting, duplicate, independence, condition and revision tests. Fixtures are
synthetic. Passing source tests is not legal sign-off or a live authority service.
Runtime tests must additionally cover forged session/approval evidence, draft
activation, concurrent revision, rollback, title mapping, onward-delegation
limits and the approved emergency pathway. Retain prior decision-time authority
snapshots rather than retrospectively evaluating history under today's rules.
