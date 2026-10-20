# Disabled native registration definition for the identity guard

25 September 2026. N03.04/N03.10 source after Decisions#32 at
3f27a63f666ab7ba7552315dd19374f5ef914870. No tenant has been changed.

RegisterIdentityStepDefinition.Build prepares one native SDK CreateRequest for a
DISABLED Update/PostOperation step and its two mandatory native images. Stable
step/image IDs and an explicit reviewed execution order are inputs. The handler
must be exactly ProtectRegisterIdentity. The Update message and its table filter
must match the register table, be server-capable and permit custom processing.
Unknown or unbound rows do not fall back to a broad registration. The factory
validates metadata shape, not native provenance or permission to install it.

The two image aliases/columns derive from the actual handler, so an installer
cannot accidentally request all fields or omit one of the four identity fields.
The step is synchronous, server-only, and has no impersonating user or filtering
attributes. No new SDK package or sibling module dependency is required.
CanBeBypassed=false is requested as metadata, not claimed as proof that an
administrator cannot disable or bypass processing. Validate actual platform and
privilege behavior during the approved native security exercises.

This is NOT an installer, import receipt or functioning protection in a tenant.
Constructing an SDK object neither executes a request nor activates a step.
The explicit disabled state is intentional: a valid build is not a claim that
native protection is enabled. The controlled deployment must verify assembly
provenance, message/filter origin, existing collisions and rank ordering; create
and inspect metadata in an approved sandbox; add components to the owning solution;
and separately approve enablement only after native tests. Do not silently enable
a step, delete conflicting steps or overwrite a previously qualified registration.
A production release must not proceed with this required guard left disabled.

The factory returns mutable SDK transport objects as required by the platform,
but every call builds fresh objects and does not retain mutable input rows.
Tests cover wrong/missing handler/message/filter, IDs, execution order, immutable
source inputs, exact minimal images, disabled defaults and SDK data-contract
round trips. They do not prove native deep-create support/state handling, actual
image registration, transaction rollback, UpdateMultiple/Upsert or bypass behavior.
An environment refusing a required setting must stop qualification, not cause the
setting or corresponding safety assertion to be removed.

No runtime per-case metadata lookup, additional clinician action or parallel
approval queue is introduced. No-admin Lists/Flow and fixed browser previews
remain independent and unchanged. Current signed delegations, the complete
human-decision-bound writer, creation/deletion/retention controls and the controlled
identity-correction route still need implementation/acceptance. Parent N03 stays
open; only the reviewed source slice can close.

Primary Microsoft metadata documentation checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstep
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstepimage
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessagefilter
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
