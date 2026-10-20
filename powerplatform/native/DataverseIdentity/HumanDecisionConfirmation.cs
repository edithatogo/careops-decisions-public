using System;
using System.Linq;
using System.Text;
using CareOps.Decisions.RegisterSchema;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>
    /// Immutable HOST-resolved confirmation and outcome-rule snapshot. Construction is not proof of human action.
    /// Never deserialize this type, Profile, Release or Authority.Request from public API, agent or form inputs.
    /// The missing protected source adapter must resolve current records and prevent concurrent withdrawal/rebinding.
    /// </summary>
    public sealed class HumanDecisionConfirmation
    {
        /// <summary>Freeze the protected source's exact action evidence and approved outcome-to-function mapping.</summary>
        public HumanDecisionConfirmation(Guid organization, Guid actorPerson, string evidenceReference, string digest,
            int recordedState, string requiredFunction, string pathway, DateTimeOffset confirmedAt, DateTimeOffset currentUntil, bool withdrawn)
        {
            if (organization == Guid.Empty || actorPerson == Guid.Empty || confirmedAt >= currentUntil)
                throw new ArgumentException("Canonical confirmation identity and a nonempty currentness interval are required.");
            if (digest == null || digest.Length != 64 || digest.Any(c => !"0123456789abcdef".Contains(c)))
                throw new ArgumentException("An exact lowercase confirmation SHA-256 is required.");
            if (recordedState < RegisterSchemaPlan.Unverified + 1 || recordedState > RegisterSchemaPlan.Unverified + 4)
                throw new ArgumentException("A supported explicit outcome is required.");
            Organization = organization; ActorPerson = actorPerson; EvidenceReference = Identifier(evidenceReference);
            Digest = digest; RecordedState = recordedState; RequiredFunction = Identifier(requiredFunction);
            Pathway = Identifier(pathway); ConfirmedAt = confirmedAt; CurrentUntil = currentUntil; Withdrawn = withdrawn;
        }
        /// <summary>Owning organization, not the caller's assertion.</summary>
        public Guid Organization { get; }
        /// <summary>Canonical human person who confirmed the exact action, not systemuser or agent identity.</summary>
        public Guid ActorPerson { get; }
        /// <summary>Version-specific protected human-action evidence reference.</summary>
        public string EvidenceReference { get; }
        /// <summary>Digest captured at confirmation, not recalculated from later edited data.</summary>
        public string Digest { get; }
        /// <summary>Outcome selected in the existing human decision.</summary>
        public int RecordedState { get; }
        /// <summary>Required function resolved from the approved outcome/pathway policy, not selected by an applicant.</summary>
        public string RequiredFunction { get; }
        /// <summary>Exact approved pathway for that decision.</summary>
        public string Pathway { get; }
        /// <summary>Confirmed source time, inclusive.</summary>
        public DateTimeOffset ConfirmedAt { get; }
        /// <summary>Exclusive source currentness boundary.</summary>
        public DateTimeOffset CurrentUntil { get; }
        /// <summary>Latest protected source withdrawal state.</summary>
        public bool Withdrawn { get; }
        private static string Identifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value != value.Trim() || value.Any(char.IsControl))
                throw new ArgumentException("A bounded exact confirmation reference is required.");
            new UTF8Encoding(false, true).GetByteCount(value); return value;
        }
    }
}
