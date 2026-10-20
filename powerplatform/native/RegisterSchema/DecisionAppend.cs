using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Immutable storage intent from a protected decision handler, NOT evidence of authorisation.</summary>
    public sealed partial class DecisionAppend
    {
        private static readonly string[] References = { "dft_actorref", "dft_function", "dft_profileversionref",
            "dft_decisionevidenceref", "dft_authorityevidenceref", "dft_scopeversionref" };
        private static readonly string[] Hashes = { "dft_profilehash", "dft_casehash" };
        private static readonly string[] Narratives = { "dft_scopetext", "dft_conditionstext", "dft_supervisiontext" };
        private static readonly string[] Instants = { "dft_validfrom", "dft_validuntil", "dft_effectiveat", "dft_recordedat" };
        private static readonly string[] StateFields = { "dft_recordedstate" };
        private static readonly string[] Allowed = References.Concat(Hashes).Concat(Narratives).Concat(Instants).Concat(StateFields).ToArray();
        private readonly Entity snapshot;

        /// <summary>
        /// Freeze the original synthetic storage intent. Production composition must use the explicit-owner
        /// overload and a protected authority/confirmation boundary; this constructor grants no permission.
        /// </summary>
        public DecisionAppend(Entity payload, Guid recordId, string expectedVersion, int expectedSequence, Guid? previousDecision)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.LogicalName != RegisterSchemaPlan.DecisionTable || payload.Id == Guid.Empty || recordId == Guid.Empty)
                throw new ArgumentException("Exact decision and register identities required.");
            if (!ulong.TryParse(expectedVersion, NumberStyles.None, CultureInfo.InvariantCulture, out ulong version) ||
                version == 0 || expectedVersion != version.ToString(CultureInfo.InvariantCulture))
                throw new ArgumentException("Canonical positive native row version required.");
            if (expectedSequence < 0 || expectedSequence == int.MaxValue ||
                (expectedSequence == 0) != !previousDecision.HasValue || previousDecision == Guid.Empty || previousDecision == payload.Id)
                throw new ArgumentException("A bounded sequence and its matching predecessor are required.");
            if (payload.Attributes.Count != Allowed.Length || Allowed.Any(name => !payload.Attributes.ContainsKey(name)))
                throw new ArgumentException("A complete exact decision payload is required; extra fields are not allowed.");
            RecordId = recordId; DecisionId = payload.Id; ExpectedVersion = expectedVersion;
            ExpectedSequence = expectedSequence; PreviousDecision = previousDecision;
            snapshot = new Entity(RegisterSchemaPlan.DecisionTable, payload.Id);
            foreach (string name in References) snapshot[name] = Text(payload, name, 256, false);
            foreach (string name in Hashes)
            {
                string hash = Text(payload, name, 64, false);
                if (hash.Length != 64 || hash.Any(c => !"0123456789abcdef".Contains(c)))
                    throw new ArgumentException("An exact lowercase SHA-256 reference is required.");
                snapshot[name] = hash;
            }
            foreach (string name in Narratives) snapshot[name] = Text(payload, name, 32768, name != "dft_scopetext");
            foreach (string name in Instants)
            {
                if (!(payload[name] is DateTime instant) || instant.Kind != DateTimeKind.Utc)
                    throw new ArgumentException("Decision instants must be explicit UTC values.");
                snapshot[name] = instant;
            }
            if ((DateTime)snapshot["dft_validfrom"] >= (DateTime)snapshot["dft_validuntil"])
                throw new ArgumentException("A nonempty scope validity interval is required.");
            if (!(payload["dft_recordedstate"] is OptionSetValue state) ||
                state.Value < RegisterSchemaPlan.Unverified + 1 || state.Value > RegisterSchemaPlan.Unverified + 4)
                throw new ArgumentException("An explicit supported recorded outcome is required, never an unverified default.");
            snapshot["dft_recordedstate"] = new OptionSetValue(state.Value);
            snapshot["dft_name"] = payload.Id.ToString("D");
            snapshot["dft_decisionkey"] = payload.Id.ToString("N");
            snapshot["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, recordId);
            snapshot["dft_sequence"] = expectedSequence + 1;
            if (previousDecision.HasValue) snapshot["dft_supersedes"] = new EntityReference(RegisterSchemaPlan.DecisionTable, previousDecision.Value);
        }

        /// <summary>Existing register record, never implicitly created.</summary>
        public Guid RecordId { get; }
        /// <summary>Stable idempotency identity retained across exact retries.</summary>
        public Guid DecisionId { get; }
        /// <summary>Expected native row version, not a business timestamp.</summary>
        public string ExpectedVersion { get; }
        /// <summary>Last recorded sequence observed by the protected handler.</summary>
        public int ExpectedSequence { get; }
        /// <summary>Exact preceding decision, absent only for sequence zero.</summary>
        public Guid? PreviousDecision { get; }
        /// <summary>New sequence; never a calculation of current scope.</summary>
        public int Sequence => ExpectedSequence + 1;
        /// <summary>Fresh allowlist used for an exact replay read.</summary>
        public string[] Columns => snapshot.Attributes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        /// <summary>Fresh native entity; mutating it cannot alter the frozen storage intent.</summary>
        public Entity ToEntity()
        {
            var copy = new Entity(snapshot.LogicalName, snapshot.Id);
            foreach (var pair in snapshot.Attributes) copy[pair.Key] = CopyValue(pair.Value);
            return copy;
        }

        /// <summary>Compare every persisted business field. A match is an old receipt, never permission to reapply it.</summary>
        public bool Matches(Entity existing)
        {
            if (existing == null || existing.LogicalName != snapshot.LogicalName || existing.Id != snapshot.Id) return false;
            int primaryColumns = 0;
            if (existing.Attributes.TryGetValue(RegisterSchemaPlan.DecisionTable + "id", out object identity))
            {
                if (!(identity is Guid id) || id != snapshot.Id) return false;
                primaryColumns = 1;
            }
            if (existing.Attributes.Count != snapshot.Attributes.Count + primaryColumns) return false;
            return snapshot.Attributes.All(pair => existing.Attributes.TryGetValue(pair.Key, out object value) && Same(pair.Value, value));
        }

        private static object CopyValue(object value)
        {
            if (value is OptionSetValue state) return new OptionSetValue(state.Value);
            if (value is EntityReference reference) return new EntityReference(reference.LogicalName, reference.Id);
            return value;
        }

        private static bool Same(object expected, object actual)
        {
            if (actual == null || expected.GetType() != actual.GetType()) return false;
            if (expected is EntityReference a)
            {
                var b = (EntityReference)actual;
                return a.Id == b.Id && a.LogicalName == b.LogicalName && b.KeyAttributes.Count == 0;
            }
            if (expected is DateTime date) return date == (DateTime)actual && ((DateTime)actual).Kind == DateTimeKind.Utc;
            return expected.Equals(actual);
        }

        private static string Text(Entity payload, string name, int maximum, bool emptyAllowed)
        {
            if (!(payload[name] is string value) || value.Length > maximum || (!emptyAllowed && string.IsNullOrWhiteSpace(value)))
                throw new ArgumentException("A bounded explicit text value is required, including empty restrictions where verified.");
            new UTF8Encoding(false, true).GetByteCount(value);
            return value;
        }
    }
}
