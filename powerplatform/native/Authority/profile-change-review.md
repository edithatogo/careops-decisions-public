# Deterministic delegation change review

24 September 2026. N02.06 source slice; workbench input for N07.
Predecessor: Decisions#15, merge 6498e8e75080e7079d2ce91e7bd0efaa9b8b46fd.

`ProfileChanges.Compare(before, after)` prepares an exact field-level comparison
for the existing configuration approval. It is not an extra approval queue or an
AI summary. The operator should see what changed in the same workbench in which
they review the full proposed profile. No new action is added to individual
credentialing applications.

The comparison binds both complete profile digests and covers every editable
profile/grant field. Whole grant additions and removals include every field,
including empty condition sets. Null means the grant is absent; an empty set
means the grant exists without values in that field. A removed condition must not
be omitted because its new value is empty. Exact grant identity and field are
separate values, not slash-delimited paths. Render them as encoded text, never
HTML or navigation instructions. The result retains immutable copies.

Ordinal set reordering and equivalent timezone offsets do not create spurious
changes. Different organization/profile IDs are rejected. A proposed lower
revision can be displayed for diagnosis but is not permission to roll back or
reuse an earlier release. The native v1 hash binds full content, not just this
change list. Reapproval must use the full after-profile and its current verified
source; this comparison neither authenticates the editor/approver nor approves
any changed authority. No human identity or source record is inferred.

Changes are exact facts, not a red/amber/green risk score. Do not classify a new
condition as safe or a deleted condition as harmful solely from the textual delta.
A human with the required authority interprets the change against the source.
The approval UI must still show currentness, source, applicable role/function,
conditions and a link to the whole profile, even for a short comparison.

The existing native-authority project and locked test workflow compile and test
this component. Every property, all add/remove/change/no-change paths and the
whole authority library remain subject to 100% line/branch coverage. Tests use
synthetic values only. No new package or network dependency is added.

Not built by this slice: Power Apps comparison screen, editable Dataverse tables,
protected read/write API, signed CHHHS instrument reconciliation or native tenant
execution. No operational authority, case record or app registration is changed.
The no-admin Lists/Flow pilot is untouched and independently usable within its
published permission and synthetic-data boundary. Parent N02 remains open.
