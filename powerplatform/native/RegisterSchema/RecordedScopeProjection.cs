using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Recorded-history interpretation, never credentialing authority or permission to practise.</summary>
    public enum RecordedScopeStatus
    {
        /// <summary>Missing/default result; never successful clearance.</summary>
        Unverified = 0,
        /// <summary>No recorded decision has become effective at the requested instant.</summary>
        NoEffectiveDecision,
        /// <summary>A grant is recorded within its validity interval; other controls still apply.</summary>
        ScopeRecorded,
        /// <summary>The selected grant's validity interval has not started.</summary>
        GrantNotYetValid,
        /// <summary>The selected grant has expired; do not fall back to an older grant.</summary>
        GrantExpired,
        /// <summary>A suspension is recorded.</summary>
        Suspended,
        /// <summary>A revocation is recorded.</summary>
        Revoked,
        /// <summary>A refusal is recorded.</summary>
        Refused
    }

    /// <summary>Immutable, minimum-necessary projection of one complete recorded history.</summary>
    public sealed class RecordedScopeView
    {
        private readonly Entity? selected;
        internal RecordedScopeView(Guid record, int latestSequence, DateTime at, bool current, Entity? row, RecordedScopeStatus status)
        {
            RecordId = record; LatestSequence = latestSequence; AsOfUtc = at;
            CurrentnessVerified = current; selected = row; Status = status;
        }
        /// <summary>Stable register record.</summary>
        public Guid RecordId { get; }
        /// <summary>Full history length checked, not the selected effective sequence.</summary>
        public int LatestSequence { get; }
        /// <summary>Instant at which recorded history was interpreted.</summary>
        public DateTime AsOfUtc { get; }
        /// <summary>Source verification interval covers this instant; not practice clearance.</summary>
        public bool CurrentnessVerified { get; }
        /// <summary>Recorded state remains visible even when currentness cannot be verified.</summary>
        public RecordedScopeStatus Status { get; }
        /// <summary>Selected effective sequence, zero when none. May be below the latest sequence.</summary>
        public int SelectedSequence => selected == null ? 0 : (int)selected["dft_sequence"];
        /// <summary>Every read must preserve this interpretation, including in exports.</summary>
        public string Disposition { get; } = "recorded-data-only-not-practice-clearance";
        /// <summary>Fresh allowlisted SDK row; no reasons, authority papers, identities or formatted values are copied.</summary>
        public Entity? ToEntity()
        {
            if (selected == null) return null;
            var copy = new Entity(selected.LogicalName, selected.Id);
            foreach (var pair in selected.Attributes)
            {
                if (pair.Value is EntityReference reference) copy[pair.Key] = new EntityReference(reference.LogicalName, reference.Id);
                else if (pair.Value is OptionSetValue state) copy[pair.Key] = new OptionSetValue(state.Value);
                else copy[pair.Key] = pair.Value; // Remaining values were frozen as strings, UTC dates and integers.
            }
            return copy;
        }
    }

    /// <summary>Pure bounded reducer. Host must separately verify identity, access and a complete consistent read.</summary>
    public static class RecordedScopeProjection
    {
        /// <summary>Engineering work bound, not a clinical limit; longer histories need separately qualified paging.</summary>
        public const int MaximumHistory = 256;
        private static readonly string[] Fields = { "dft_record", "dft_sequence", "dft_supersedes", "dft_scopeversionref",
            "dft_scopetext", "dft_conditionstext", "dft_supervisiontext", "dft_recordedstate",
            "dft_validfrom", "dft_validuntil", "dft_effectiveat", "dft_recordedat" };
        /// <summary>Fresh minimum-column list for the native history query.</summary>
        public static string[] Columns => (string[])Fields.Clone();

        /// <summary>Check the complete ordered chain and select the last recorded AND effective revision, not the latest timestamp.</summary>
        public static RecordedScopeView Project(Guid record, int expectedSequence, Guid? latestDecision,
            EntityCollection history, DateTime atUtc, DateTime? sourceCheckedAtUtc, DateTime? sourceCurrentUntilUtc)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            RequireUtc(atUtc);
            if (sourceCheckedAtUtc.HasValue) RequireUtc(sourceCheckedAtUtc.Value);
            if (sourceCurrentUntilUtc.HasValue) RequireUtc(sourceCurrentUntilUtc.Value);
            if (record == Guid.Empty || expectedSequence < 0 || expectedSequence > MaximumHistory ||
                (expectedSequence == 0) != !latestDecision.HasValue || latestDecision == Guid.Empty ||
                history.MoreRecords || history.TotalRecordCountLimitExceeded || history.Entities.Count != expectedSequence)
                throw InvalidHistory();
            var ids = new HashSet<Guid>();
            Guid? previous = null; DateTime? previousRecorded = null; Entity? selected = null;
            int sequence = 0;
            foreach (Entity row in history.Entities)
            {
                sequence++;
                if (row == null || row.LogicalName != RegisterSchemaPlan.DecisionTable || row.Id == Guid.Empty || !ids.Add(row.Id) ||
                    Read<int>(row, "dft_sequence") != sequence || !ReferenceIs(row, "dft_record", RegisterSchemaPlan.RecordTable, record))
                    throw InvalidHistory();
                if (previous.HasValue)
                {
                    if (!ReferenceIs(row, "dft_supersedes", RegisterSchemaPlan.DecisionTable, previous.Value)) throw InvalidHistory();
                }
                else if (row.Attributes.TryGetValue("dft_supersedes", out object prior) && prior != null) throw InvalidHistory();
                DateTime recorded = Instant(row, "dft_recordedat");
                if (previousRecorded.HasValue && recorded < previousRecorded.Value) throw InvalidHistory();
                DateTime effect = Instant(row, "dft_effectiveat");
                DateTime from = Instant(row, "dft_validfrom"); DateTime until = Instant(row, "dft_validuntil");
                int state = Read<OptionSetValue>(row, "dft_recordedstate").Value;
                if (from >= until || state < RegisterSchemaPlan.Unverified + 1 || state > RegisterSchemaPlan.Unverified + 4)
                    throw InvalidHistory();
                // Validate every entry, including not-yet-effective entries. Unknown restrictions never mean none.
                var snapshot = new Entity(RegisterSchemaPlan.DecisionTable, row.Id);
                snapshot["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, record);
                snapshot["dft_sequence"] = sequence; snapshot["dft_recordedstate"] = new OptionSetValue(state);
                snapshot["dft_scopeversionref"] = Text(row, "dft_scopeversionref", 256, false);
                snapshot["dft_scopetext"] = Text(row, "dft_scopetext", 32768, false);
                snapshot["dft_conditionstext"] = Text(row, "dft_conditionstext", 32768, true);
                snapshot["dft_supervisiontext"] = Text(row, "dft_supervisiontext", 32768, true);
                snapshot["dft_validfrom"] = from; snapshot["dft_validuntil"] = until;
                snapshot["dft_recordedat"] = recorded; snapshot["dft_effectiveat"] = effect;
                if (recorded <= atUtc && effect <= atUtc) selected = snapshot;
                previous = row.Id; previousRecorded = recorded;
            }
            // Empty IDs are rejected above; null can occur only for the verified empty chain.
            if (previous.GetValueOrDefault() != latestDecision.GetValueOrDefault()) throw InvalidHistory();
            bool current = sourceCheckedAtUtc.HasValue && sourceCurrentUntilUtc.HasValue &&
                sourceCheckedAtUtc.Value <= atUtc && atUtc < sourceCurrentUntilUtc.Value;
            return new RecordedScopeView(record, expectedSequence, atUtc, current, selected, Status(selected, atUtc));
        }

        private static RecordedScopeStatus Status(Entity? selected, DateTime at)
        {
            if (selected == null) return RecordedScopeStatus.NoEffectiveDecision;
            int state = ((OptionSetValue)selected["dft_recordedstate"]).Value;
            if (state == RegisterSchemaPlan.Unverified + 2) return RecordedScopeStatus.Suspended;
            if (state == RegisterSchemaPlan.Unverified + 3) return RecordedScopeStatus.Revoked;
            if (state == RegisterSchemaPlan.Unverified + 4) return RecordedScopeStatus.Refused;
            if (at < (DateTime)selected["dft_validfrom"]) return RecordedScopeStatus.GrantNotYetValid;
            if (at >= (DateTime)selected["dft_validuntil"]) return RecordedScopeStatus.GrantExpired;
            return RecordedScopeStatus.ScopeRecorded;
        }
        private static T Read<T>(Entity row, string name)
        {
            if (!row.Attributes.TryGetValue(name, out object value) || value == null || value.GetType() != typeof(T)) throw InvalidHistory();
            return (T)value;
        }
        private static bool ReferenceIs(Entity row, string name, string table, Guid id)
        {
            EntityReference reference = Read<EntityReference>(row, name);
            return reference.Id == id && reference.LogicalName == table && reference.KeyAttributes.Count == 0;
        }
        private static DateTime Instant(Entity row, string name)
        {
            DateTime value = Read<DateTime>(row, name); RequireUtc(value); return value;
        }
        private static void RequireUtc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc) throw InvalidHistory();
        }
        private static string Text(Entity row, string name, int maximum, bool emptyAllowed)
        {
            string value = Read<string>(row, name);
            if (value.Length > maximum || (!emptyAllowed && string.IsNullOrWhiteSpace(value))) throw InvalidHistory();
            new UTF8Encoding(false, true).GetByteCount(value);
            return value;
        }
        private static InvalidPluginExecutionException InvalidHistory() => new InvalidPluginExecutionException(
            "Recorded scope could not be verified from complete history. No decision or scope has been changed.");
    }
}
