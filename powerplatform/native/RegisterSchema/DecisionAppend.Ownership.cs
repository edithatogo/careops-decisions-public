using System;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    public sealed partial class DecisionAppend
    {
        /// <summary>
        /// Freeze explicit, separately approved owning teams BEFORE creation. The protected host must verify
        /// both native teams and applicable access policy; GUIDs alone do not establish that approval.
        /// Decision evidence and event delivery can have different owners. No default to the acting clinician.
        /// </summary>
        public DecisionAppend(Entity payload, Guid recordId, string expectedVersion, int expectedSequence,
            Guid? previousDecision, Guid decisionOwnerId, Guid eventOwnerId)
            : this(payload, recordId, expectedVersion, expectedSequence, previousDecision)
        {
            if (decisionOwnerId == Guid.Empty || eventOwnerId == Guid.Empty)
                throw new ArgumentException("Explicit approved decision and event owning-team identifiers are required.");
            DecisionOwnerId = decisionOwnerId; EventOwnerId = eventOwnerId;
            snapshot["ownerid"] = new EntityReference("team", decisionOwnerId);
        }

        /// <summary>Null only for the earlier synthetic unowned storage adapter, never an operational fallback.</summary>
        public Guid? DecisionOwnerId { get; }
        /// <summary>Event owner frozen alongside the complete decision; not a mutable delivery checkpoint.</summary>
        public Guid? EventOwnerId { get; }
    }
}
