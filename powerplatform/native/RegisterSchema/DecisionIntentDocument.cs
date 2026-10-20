using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Bounded, versioned persistence of the original confirmed intent. Not an approval or public API.</summary>
    public static class DecisionIntentDocument
    {
        /// <summary>Explicit format version; unknown versions are never interpreted as this one.</summary>
        public const string Version = "careops-decision-source-v1";
        /// <summary>Maximum UTF-8 document size, including all JSON escaping and metadata.</summary>
        public const int MaximumBytes = 1048576;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly string[] Headers = { "version", "organization", "record", "decision", "expectedVersion", "expectedSequence",
            "previousDecision", "decisionOwner", "eventOwner", "payload" };
        private static readonly string[] TextFields = { "dft_actorref", "dft_function", "dft_profileversionref", "dft_decisionevidenceref",
            "dft_authorityevidenceref", "dft_scopeversionref", "dft_profilehash", "dft_casehash", "dft_scopetext", "dft_conditionstext", "dft_supervisiontext" };
        private static readonly string[] Times = { "dft_validfrom", "dft_validuntil", "dft_effectiveat", "dft_recordedat" };
        private static readonly string[] PayloadFields = TextFields.Concat(Times).Concat(new[] { "dft_recordedstate" }).ToArray();
        private const string Failure = "The original decision source document could not be restored exactly.";

        /// <summary>Serialize only the sealed, fully owner-bound intent; no SDK type metadata or arbitrary fields.</summary>
        public static string Encode(Guid organization, DecisionAppend intent)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            if (organization == Guid.Empty || !intent.EventOwnerId.HasValue)
                throw new ArgumentException("An organization and fully owner-bound intent are required.");
            Entity row = intent.ToEntity();
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject(); writer.WriteString("version", Version);
                    writer.WriteString("organization", organization.ToString("D"));
                    writer.WriteString("record", intent.RecordId.ToString("D"));
                    writer.WriteString("decision", intent.DecisionId.ToString("D"));
                    writer.WriteString("expectedVersion", intent.ExpectedVersion);
                    writer.WriteNumber("expectedSequence", intent.ExpectedSequence);
                    if (intent.PreviousDecision.HasValue) writer.WriteString("previousDecision", intent.PreviousDecision.Value.ToString("D"));
                    else writer.WriteNull("previousDecision");
                    writer.WriteString("decisionOwner", intent.DecisionOwnerId!.Value.ToString("D"));
                    writer.WriteString("eventOwner", intent.EventOwnerId.Value.ToString("D"));
                    writer.WriteStartObject("payload");
                    foreach (string name in TextFields) writer.WriteString(name, (string)row[name]);
                    foreach (string name in Times) writer.WriteString(name, ((DateTime)row[name]).ToString("O", CultureInfo.InvariantCulture));
                    writer.WriteNumber("dft_recordedstate", ((OptionSetValue)row["dft_recordedstate"]).Value);
                    writer.WriteEndObject(); writer.WriteEndObject(); writer.Flush();
                }
                // Sealed field bounds limit the encoded maximum below MaximumBytes, even with six-byte escaping.
                return Utf8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// Restore the original intent only when its complete digest matches the independently stored original.
        /// The host must obtain expectedDigest from a protected original confirmation, not the incoming document.
        /// Successful parsing/digest equality does not authenticate that source or authorise journal staging.
        /// </summary>
        public static DecisionAppend Restore(Guid expectedOrganization, string document, string expectedDigest)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            try
            {
                if (expectedOrganization == Guid.Empty || expectedDigest == null || expectedDigest.Length != 64 ||
                    expectedDigest.Any(c => !"0123456789abcdef".Contains(c)) || document.Length > MaximumBytes || Utf8.GetByteCount(document) > MaximumBytes)
                    throw Invalid();
                var options = new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 3 };
                using (JsonDocument parsed = JsonDocument.Parse(document, options))
                {
                    Dictionary<string, JsonElement> header = Object(parsed.RootElement, Headers);
                    if (Text(header["version"]) != Version || Identity(header["organization"]) != expectedOrganization) throw Invalid();
                    var row = new Entity(RegisterSchemaPlan.DecisionTable, Identity(header["decision"]));
                    Dictionary<string, JsonElement> payload = Object(header["payload"], PayloadFields);
                    foreach (string name in TextFields) row[name] = Text(payload[name]);
                    foreach (string name in Times)
                    {
                        string value = Text(payload[name]);
                        if (!DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime instant) ||
                            instant.Kind != DateTimeKind.Utc || value != instant.ToString("O", CultureInfo.InvariantCulture)) throw Invalid();
                        row[name] = instant;
                    }
                    row["dft_recordedstate"] = new OptionSetValue(Number(payload["dft_recordedstate"]));
                    Guid? previous = header["previousDecision"].ValueKind == JsonValueKind.Null ? (Guid?)null : Identity(header["previousDecision"]);
                    var intent = new DecisionAppend(row, Identity(header["record"]), Text(header["expectedVersion"]),
                        Number(header["expectedSequence"]), previous, Identity(header["decisionOwner"]), Identity(header["eventOwner"]));
                    if (DecisionConfirmationHash.Compute(expectedOrganization, intent) != expectedDigest) throw Invalid();
                    return intent;
                }
            }
            catch (Exception)
            {
                // Do not expose snippets, field values, parser offsets or inner exceptions containing source data.
                throw Invalid();
            }
        }

        private static Dictionary<string, JsonElement> Object(JsonElement value, string[] names)
        {
            if (value.ValueKind != JsonValueKind.Object) throw Invalid();
            var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Contains(property.Name, StringComparer.Ordinal) || fields.ContainsKey(property.Name)) throw Invalid();
                fields.Add(property.Name, property.Value);
            }
            if (fields.Count != names.Length) throw Invalid();
            return fields;
        }
        private static string Text(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String) throw Invalid();
            return value.GetString()!;
        }
        private static Guid Identity(JsonElement value)
        {
            string text = Text(value);
            if (!Guid.TryParseExact(text, "D", out Guid id) || id == Guid.Empty || text != id.ToString("D")) throw Invalid();
            return id;
        }
        private static int Number(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Number || !int.TryParse(value.GetRawText(), NumberStyles.None,
                CultureInfo.InvariantCulture, out int number)) throw Invalid();
            return number;
        }
        private static ArgumentException Invalid() => new ArgumentException(Failure);
    }
}
