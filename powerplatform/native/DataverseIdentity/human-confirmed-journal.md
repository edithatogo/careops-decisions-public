# Compose authenticated human confirmation and delegation with journal staging

25 September 2026. N03.04-N03.06 after Decisions#36 at
3978c179396bbab244e158982cb57f571a2612e3. Source only; no registered write API.

HumanConfirmedJournal.Stage now joins existing native caller resolution, native
register/owner reads, the real delegation kernel, the exact owner-bound intent
and the existing journal/outbox in ONE caller-scoped service and ambient transaction.
CallerResolver.ResolveBinding returns the service it actually verified, rather
than opening a second or privileged connection. The old caller-only API and its
identity tests remain supported. This does not depend on another CareOps module.

The core rejects application users, impersonation, unsupported pipeline contexts,
unbound targets, canonical-person/organization mismatches, changed confirmation
content, wrong source/release/function/occupancy references, different native
practitioner/profession/facility and unknown ownership. Team metadata must identify
an owner-capable nondefault team; an access team or a broad default business-unit
team is not silently substituted. Owner IDs are sourced from separately approved
access configuration, not chosen by clinicians. Native roles/Create/Assign rights
still apply and are not inferred from a team type.

Only the exact recorded outcome, policy-required function and pathway confirmed
by the human source are accepted. DelegationGate rechecks the released profile,
occupancy, conflict, independent recommendation and conditions with server time
after preparation reads. Missing or expired authority prevents staging; it does
not revoke any existing scope or generate an adverse practitioner finding.
JournalWithOutbox retains its exact-old-receipt/no-write handling, root CAS and
same-service event staging. The return is staged/old-receipt, NOT proof of commit.
All errors escape for native rollback; no retry, compensation or message occurs.

## Important trust boundary: source adapter is still required

HumanDecisionConfirmation, Profile, Release and Authority.Request are immutable
HOST snapshots, not authenticated public inputs. Their constructors do NOT verify
that a person acted, a source is current, or an outcome/function mapping is approved.
No parser, IPlugin or public API accepting those objects is added here. The protected
native adapter must resolve them from authoritative records under approved access,
verify the original human action/current source/approved outcome rule, and prevent
concurrent withdrawal, role changes or profile rebinding through native version or
transaction controls. DO NOT build them from an applicant payload or an agent's
claim. This slice tests cross-component binding; it does not replace that adapter.

The confirmation digest must come from the original protected human decision,
not be recalculated after accepting edited conditions, owners or case versions.
Canonical dft_actorref and dft_practitionerref are lowercase D-format dft_orgperson
GUIDs in this new writer path, never systemuser IDs, names or fuzzy aliases. Legacy
source identifiers require explicit approved mapping before this path is used.
The required function/outcome/pathway is resolved by the policy adapter, not fixed
in code or inferred from traffic-light preparation. Verified outcome policy and
ownership configuration remain editable through independently approved releases.

This core performs current-authority checks even on a retry. If authority or the
confirmation has expired since a successful original write, use the existing
caller-authorised recorded-receipt view rather than asking for a new clinical
decision, relaxing checks or replaying the event. Automatic client-side routing
for that distinction remains to be implemented. The human decision stays one step.

## Evidence and remaining acceptance

The integration tests execute actual CallerResolver/DelegationGate/JournalWithOutbox
and SDK objects using a synthetic service. They check denied paths make no writes,
exact retries keep original records, different owner teams are assigned at Create,
late Target mutation cannot redirect records, and errors after staging escape.
They cannot prove Dataverse rollback, concurrent source stability, native lookup
and timestamp/null representation, role security, prohibited direct Create or
administrative bypass. Those are mandatory sandbox tests, not passed by source CI.

Only a same-module Authority project reference is added. No external dependency
versions change. Graph changes require reviewed native lock files and final locked
restore; existing strict test, complete-library coverage and no-skips gates remain.
No-admin Lists/Flow/browser pilots remain separate and unchanged. No extra queue,
form, technical team selection, recurring review or second credentialing approval
is added. No tenant, new Entra app, permission, practitioner record, scope, paid
service or CGov action. N03 stays open until the protected adapter, deployment and
native/operational acceptance are complete.

Microsoft guidance checked25 September2026:
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/team
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/scalable-customization-design/database-transactions
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/impersonate-a-user
