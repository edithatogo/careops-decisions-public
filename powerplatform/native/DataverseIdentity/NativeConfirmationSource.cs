using System;
using System.Globalization;
using System.Linq;
using System.Text;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Immutable original confirmation read from one native row. Not a lock, signature or authority token.</summary>
    public sealed class NativeConfirmation
    {
        internal NativeConfirmation(Guid source, string version, string policy, string fence, DecisionAppend intent, HumanDecisionConfirmation confirmation)
        { SourceId = source; RowVersion = version; PolicyReference = policy; FenceToken = fence; Intent = intent; Confirmation = confirmation; }
        /// <summary>Original native source identity, not a generated replacement decision ID.</summary>
        public Guid SourceId { get; }
        /// <summary>Observed native concurrency version; not a clock or a reusable permission.</summary>
        public string RowVersion { get; }
        /// <summary>Original outcome-rule version, which still needs resolution against an approved rule release.</summary>
        public string PolicyReference { get; }
        /// <summary>Observed technical token; no lock has been acquired by a read.</summary>
        public string FenceToken { get; }
        /// <summary>The actual original owner-bound intent restored against the saved original digest.</summary>
        public DecisionAppend Intent { get; }
        /// <summary>Typed original source snapshot, not caller-supplied human-action claims.</summary>
        public HumanDecisionConfirmation Confirmation { get; }
    }

    /// <summary>Caller-scoped native confirmation source acquisition. Not a registered API, capture route or concurrency fence.</summary>
    public static class NativeConfirmationSource
    {
        private const string Failure = "The original credentialing confirmation source is unavailable. No decision was repeated or changed.";
        /// <summary>Stable evidence reference derived from the source ID. Its presence is not evidence of capture.</summary>
        public static string EvidenceReference(Guid sourceId)
        {
            if (sourceId == Guid.Empty) throw new ArgumentException("A stable original source identifier is required.", nameof(sourceId));
            return "native-confirmation:" + sourceId.ToString("D");
        }

        /// <summary>
        /// Read the original source through the SAME verified caller service. Requires the same original human creator.
        /// Protected capture, direct-Create restrictions and source immutability must be enforced separately before use.
        /// Do not expose this full-content snapshot in the register card or treat read success as permission to stage.
        /// </summary>
        public static NativeConfirmation Read(CallerBinding binding, Guid recordId, Guid sourceId)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (recordId == Guid.Empty || sourceId == Guid.Empty) throw new ArgumentException("Canonical record and source IDs are required.");
            try
            {
                Entity row = binding.Service.Retrieve(ConfirmationSourceSchema.Table, sourceId, new ColumnSet(ConfirmationSourceSchema.ReadColumns));
                if (row == null || row.LogicalName != ConfirmationSourceSchema.Table || row.Id != sourceId || row.KeyAttributes.Count != 0)
                    throw new InvalidPluginExecutionException(Failure);
                string version = row.RowVersion;
                Require(ulong.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out ulong parsedVersion) && parsedVersion > 0 &&
                    version == parsedVersion.ToString(CultureInfo.InvariantCulture));
                Require(row.Attributes.TryGetValue("statecode", out object state) && state is OptionSetValue active && active.Value == 0);
                Require(row.Attributes.TryGetValue("dft_withdrawn", out object rawWithdrawal) && rawWithdrawal is bool withdrawn && !withdrawn);
                Match(row, "dft_protocol", ConfirmationSourceSchema.Protocol); Match(row, "dft_confirmationkey", sourceId.ToString("N"));
                Match(row, "dft_actorref", binding.Caller.Person.ToString("D"));
                Reference(row, "dft_record", RegisterSchemaPlan.RecordTable, recordId);
                Reference(row, "createdby", "systemuser", binding.Caller.PlatformUser);
                Require(!row.Attributes.TryGetValue("createdonbehalfby", out object onBehalf) || onBehalf == null);
                string digest = Identifier(row, "dft_originaldigest", 64);
                string document = Text(row, "dft_originaldocument");
                DecisionAppend intent = DecisionIntentDocument.Restore(binding.Caller.Organization, document, digest);
                Require(intent.RecordId == recordId);
                string evidence = EvidenceReference(sourceId);
                Entity payload = intent.ToEntity(); Match(payload, "dft_actorref", binding.Caller.Person.ToString("D"));
                Match(payload, "dft_decisionevidenceref", evidence);
                string function = Identifier(row, "dft_function", 256); Match(payload, "dft_function", function);
                Require(row.Attributes.TryGetValue("dft_recordedstate", out object rawOutcome) && rawOutcome is OptionSetValue);
                int outcome = ((OptionSetValue)row["dft_recordedstate"]).Value;
                Require(outcome == ((OptionSetValue)payload["dft_recordedstate"]).Value);
                string token = Identifier(row, "dft_fencetoken", 32);
                Require(Guid.TryParseExact(token, "N", out Guid tokenId) && tokenId != Guid.Empty && token == tokenId.ToString("N"));
                var confirmation = new HumanDecisionConfirmation(binding.Caller.Organization, binding.Caller.Person, evidence, digest, outcome,
                    function, Identifier(row, "dft_pathway", 256), Instant(row, "dft_confirmedutc"), Instant(row, "dft_currentuntilutc"), false);
                DateTimeOffset at = DateTimeOffset.UtcNow;
                DateTime recorded = (DateTime)payload["dft_recordedat"];
                Require(confirmation.ConfirmedAt <= at && at < confirmation.CurrentUntil && confirmation.ConfirmedAt.UtcDateTime <= recorded && recorded <= at.UtcDateTime);
                return new NativeConfirmation(sourceId, version, Identifier(row, "dft_policyref", 256), token, intent, confirmation);
            }
            catch (Exception)
            {
                // Native permission faults and invalid/missing source data remain indistinguishable to the caller.
                throw new InvalidPluginExecutionException(Failure);
            }
        }
        private static string Text(Entity row, string name)
        {
            if (!row.Attributes.TryGetValue(name, out object value) || !(value is string text)) throw new InvalidPluginExecutionException(Failure);
            return text;
        }
        private static string Identifier(Entity row, string name, int maximum)
        {
            string value = Text(row, name);
            Require(!string.IsNullOrWhiteSpace(value) && value.Length <= maximum && value == value.Trim() && !value.Any(char.IsControl));
            new UTF8Encoding(false, true).GetByteCount(value); return value;
        }
        private static DateTimeOffset Instant(Entity row, string name)
        {
            string value = Text(row, name);
            Require(DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime instant) &&
                instant.Kind == DateTimeKind.Utc && value == instant.ToString("O", CultureInfo.InvariantCulture));
            return new DateTimeOffset(instant);
        }
        private static void Match(Entity row, string name, string expected) => Require(Text(row, name) == expected);
        private static void Reference(Entity row, string name, string table, Guid id) => Require(row.Attributes.TryGetValue(name, out object raw) &&
            raw is EntityReference value && value.LogicalName == table && value.Id == id && value.KeyAttributes.Count == 0);
        private static void Require(bool condition)
        { if (!condition) throw new InvalidPluginExecutionException(Failure); }
    }
}
