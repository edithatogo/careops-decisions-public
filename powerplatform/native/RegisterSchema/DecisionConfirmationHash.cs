using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Exact domain-separated confirmation digest. Integrity binding only, not a signature or human approval.</summary>
    public static class DecisionConfirmationHash
    {
        /// <summary>BinaryWriter little-endian format with typed values and ordinal field order.</summary>
        public const string Version = "careops-decision-confirmation-v1";

        /// <summary>
        /// Bind organization, whole immutable decision, record, predecessor, expected version/sequence and both owners.
        /// The protected confirmation UI/source must retain this exact digest for the existing human decision.
        /// No unowned fixture, case-only hash, fresh recalculation after editing or agent assertion can stand in for it.
        /// </summary>
        public static string Compute(Guid organization, DecisionAppend intent)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            // The sealed constructor binds both owners together; neither can subsequently be changed.
            if (organization == Guid.Empty || !intent.EventOwnerId.HasValue)
                throw new ArgumentException("An organization and fully owner-bound immutable decision are required.");
            Entity row = intent.ToEntity();
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, new UTF8Encoding(false, true), true))
                {
                    writer.Write(Version); writer.Write(organization.ToByteArray());
                    writer.Write(row.LogicalName); writer.Write(row.Id.ToByteArray());
                    writer.Write(intent.ExpectedVersion); writer.Write(intent.ExpectedSequence);
                    writer.Write(intent.EventOwnerId.Value.ToByteArray()); writer.Write(row.Attributes.Count);
                    foreach (var pair in row.Attributes.OrderBy(x => x.Key, StringComparer.Ordinal))
                    {
                        writer.Write(pair.Key);
                        if (pair.Value is string text) { writer.Write((byte)0); writer.Write(text); }
                        else if (pair.Value is int number) { writer.Write((byte)1); writer.Write(number); }
                        else if (pair.Value is DateTime instant) { writer.Write((byte)2); writer.Write(instant.Ticks); }
                        else if (pair.Value is EntityReference reference)
                        { writer.Write((byte)3); writer.Write(reference.LogicalName); writer.Write(reference.Id.ToByteArray()); }
                        else
                        {
                            // DecisionAppend's sealed allowlist validates every value; the only remaining type is OptionSetValue.
                            writer.Write((byte)4); writer.Write(((OptionSetValue)pair.Value).Value);
                        }
                    }
                }
                using (var algorithm = SHA256.Create())
                    return BitConverter.ToString(algorithm.ComputeHash(buffer.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
