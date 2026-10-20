using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class DecisionIntentDocumentTests
    {
        private static readonly Guid Org = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        private static readonly Guid Record = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        private static readonly Guid Decision = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
        private static readonly Guid Owner = Guid.Parse("dddddddd-0000-0000-0000-000000000004");
        private static readonly Guid EventOwner = Guid.Parse("eeeeeeee-0000-0000-0000-000000000005");
        private static readonly Guid Previous = Guid.Parse("ffffffff-0000-0000-0000-000000000006");
        private const string Failure = "The original decision source document could not be restored exactly.";
        internal static Entity Payload()
        {
            var row = new Entity(RegisterSchemaPlan.DecisionTable, Decision);
            foreach (string name in new[] { "actorref", "function", "profileversionref", "decisionevidenceref", "authorityevidenceref", "scopeversionref" })
                row["dft_" + name] = "synthetic-" + name;
            row["dft_profilehash"] = new string('a', 64); row["dft_casehash"] = new string('b', 64);
            row["dft_scopetext"] = "Synthetic scope \u00e9 \U0001F33F\n<not markup>";
            row["dft_conditionstext"] = ""; row["dft_supervisiontext"] = "Explicit synthetic supervision";
            DateTime at = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc).AddTicks(1234567);
            row["dft_validfrom"] = at; row["dft_validuntil"] = at.AddDays(10);
            row["dft_effectiveat"] = at; row["dft_recordedat"] = at;
            row["dft_recordedstate"] = new OptionSetValue(602170001); return row;
        }
        private static DecisionAppend Intent(bool previous = false) => new DecisionAppend(Payload(), Record, "42", previous ? 2 : 0, previous ? Previous : (Guid?)null, Owner, EventOwner);
        private static string Document() => DecisionIntentDocument.Encode(Org, Intent());
        private static JsonObject Object() => JsonNode.Parse(Document())!.AsObject();
        private static void Denied(string document, string? digest = null)
        {
            var error = Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, document,
                digest ?? DecisionConfirmationHash.Compute(Org, Intent())));
            Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
        }

        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void OriginalContentControlsAndDigestSurviveExactRoundtrip(bool previous)
        {
            DecisionAppend original = Intent(previous); string document = DecisionIntentDocument.Encode(Org, original);
            DecisionAppend restored = DecisionIntentDocument.Restore(Org, document, DecisionConfirmationHash.Compute(Org, original));
            Assert.True(original.Matches(restored.ToEntity())); Assert.True(restored.Matches(original.ToEntity()));
            Assert.Equal(original.RecordId, restored.RecordId); Assert.Equal(original.DecisionId, restored.DecisionId);
            Assert.Equal(original.ExpectedVersion, restored.ExpectedVersion); Assert.Equal(original.ExpectedSequence, restored.ExpectedSequence);
            Assert.Equal(original.PreviousDecision, restored.PreviousDecision); Assert.Equal(original.DecisionOwnerId, restored.DecisionOwnerId);
            Assert.Equal(original.EventOwnerId, restored.EventOwnerId); Assert.Equal(document, DecisionIntentDocument.Encode(Org, restored));
            Assert.Equal(DateTimeKind.Utc, ((DateTime)restored.ToEntity()["dft_recordedat"]).Kind);
        }
        [Fact]
        public void EncodingCannotPromoteAnUnownedSyntheticFixture()
        {
            Assert.Throws<ArgumentNullException>(() => DecisionIntentDocument.Encode(Org, null!));
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Encode(Guid.Empty, Intent()));
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Encode(Org, new DecisionAppend(Payload(), Record, "42", 0, null)));
        }
        [Fact]
        public void OriginalDigestMustComeFromOutsideTheDocument()
        {
            var node = Object(); node["payload"]!["dft_conditionstext"] = "Changed since the human decision";
            Denied(node.ToJsonString());
            var injected = Object(); injected["expectedDigest"] = new string('a', 64); Denied(injected.ToJsonString());
            foreach (string? digest in new string?[] { null, "", new string('A', 64), new string('g', 64), new string('a', 63), new string('c', 64) })
            {
                var error = Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, Document(), digest!));
                Assert.Equal(Failure, error.Message);
            }
            Assert.Throws<ArgumentNullException>(() => DecisionIntentDocument.Restore(Org, null!, new string('a', 64)));
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Guid.Empty, Document(), new string('a', 64)));
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Record, Document(), new string('a', 64)));
        }
        [Fact]
        public void EveryHeaderAndPayloadFieldIsRequiredAndNoExtraPropertyIsAccepted()
        {
            foreach (string name in Object().Select(p => p.Key).ToArray()) { var node = Object(); node.Remove(name); Denied(node.ToJsonString()); }
            foreach (string name in Object()["payload"]!.AsObject().Select(p => p.Key).ToArray())
            { var node = Object(); node["payload"]!.AsObject().Remove(name); Denied(node.ToJsonString()); }
            var extra = Object(); extra["$type"] = "unsafe"; Denied(extra.ToJsonString());
            extra = Object(); extra["payload"]!["ownerid"] = Owner.ToString("D"); Denied(extra.ToJsonString());
            extra = Object(); extra["payload"] = new JsonArray(); Denied(extra.ToJsonString());
            Denied("[]"); Denied("null"); Denied("1");
        }
        [Fact]
        public void DuplicateEscapedOrCaseVariantNamesCannotOverrideAnEarlierValue()
        {
            string document = Document();
            Denied(document.Insert(1, "\"version\":\"careops-decision-source-v1\","));
            Denied(document.Insert(1, "\"ver\\u0073ion\":\"careops-decision-source-v1\","));
            Denied(document.Replace("\"payload\":{", "\"payload\":{\"dft_scopetext\":\"override\","));
            Denied(document.Replace("\"version\":", "\"Version\":"));
        }
        [Fact]
        public void InvalidVersionSyntaxDepthAndOversizedBytesFailBeforeReconstruction()
        {
            var node = Object(); node["version"] = "careops-decision-source-v2"; Denied(node.ToJsonString());
            foreach (string invalid in new[] { "", "{", Document() + "{}", Document().Insert(1, "/* comment */"), Document().Insert(Document().Length - 1, ","),
                "{\"payload\":{\"x\":[[[[1]]]]}}", "\uD800", new string(' ', DecisionIntentDocument.MaximumBytes + 1),
                new string('\u00e9', DecisionIntentDocument.MaximumBytes / 2 + 1) }) Denied(invalid);
        }
        [Theory]
        [InlineData("organization")] [InlineData("record")] [InlineData("decision")] [InlineData("decisionOwner")] [InlineData("eventOwner")]
        public void CanonicalNonemptyGuidsAreNeverInferredFromAliasesOrOtherJsonTypes(string field)
        {
            foreach (string value in new[] { "", "name", Guid.Empty.ToString("D"), Org.ToString("N"), "AAAAAAAA-0000-0000-0000-000000000001" })
            { var node = Object(); node[field] = value; Denied(node.ToJsonString()); }
            var typed = Object(); typed[field] = 1; Denied(typed.ToJsonString());
        }
        [Fact]
        public void PreviousDecisionAndSequenceMustRemainConsistent()
        {
            var node = Object(); node["previousDecision"] = Previous.ToString("D"); Denied(node.ToJsonString());
            node = Object(); node["expectedSequence"] = 1; Denied(node.ToJsonString());
            node = Object(); node["previousDecision"] = ""; Denied(node.ToJsonString());
            node = Object(); node["expectedVersion"] = "042"; Denied(node.ToJsonString());
            node = Object(); node["expectedVersion"] = 42; Denied(node.ToJsonString());
            node = Object(); node["expectedSequence"] = int.MaxValue; Denied(node.ToJsonString());
        }
        [Fact]
        public void NumericCoercionExponentAndFractionsAreRejected()
        {
            foreach (string raw in new[] { "null", "\"0\"", "false", "-1", "0.0", "0e0", "2147483648" })
                Denied(Document().Replace("\"expectedSequence\":0", "\"expectedSequence\":" + raw));
            foreach (string raw in new[] { "null", "\"602170001\"", "602170001.0", "602170000", "602170005" })
                Denied(Document().Replace("\"dft_recordedstate\":602170001", "\"dft_recordedstate\":" + raw));
        }
        [Theory]
        [InlineData("dft_validfrom")] [InlineData("dft_validuntil")] [InlineData("dft_effectiveat")] [InlineData("dft_recordedat")]
        public void ExactUtcAndFractionalTimeAreRequiredNotSilentlyTruncated(string field)
        {
            foreach (string value in new[] { "invalid", "2026-09-25T00:00:00.1234567", "2026-09-25T00:00:00.1234567+00:00", "2026-09-25T00:00:00Z", "2026-09-25T00:00:00.1230000Z" })
            { var node = Object(); node["payload"]![field] = value; Denied(node.ToJsonString()); }
        }
        [Fact]
        public void ExplicitEmptyRestrictionsArePreservedButNullAndMissingAreNotNoConditions()
        {
            var restored = DecisionIntentDocument.Restore(Org, Document(), DecisionConfirmationHash.Compute(Org, Intent()));
            Assert.Equal("", restored.ToEntity()["dft_conditionstext"]);
            var node = Object(); node["payload"]!["dft_conditionstext"] = null; Denied(node.ToJsonString());
            node = Object(); node["payload"]!["dft_scopetext"] = ""; Denied(node.ToJsonString());
            node = Object(); node["payload"]!["dft_scopetext"] = new string('x', 32769); Denied(node.ToJsonString());
            node = Object(); node["payload"]!["dft_casehash"] = new string('a', 63); Denied(node.ToJsonString());
        }
        [Fact]
        public void InvalidEscapedSurrogatesCannotBeReplacedDuringRestore()
        {
            Denied(Document().Replace("\"dft_conditionstext\":\"\"", "\"dft_conditionstext\":\"\\uD800\""));
        }
        [Fact]
        public void FullBoundedUnicodePayloadFitsAndRoundtripsWithoutLoss()
        {
            Entity payload = Payload();
            foreach (string field in new[] { "dft_scopetext", "dft_conditionstext", "dft_supervisiontext" }) payload[field] = new string('\u00e9', 32768);
            foreach (string field in new[] { "dft_actorref", "dft_function", "dft_profileversionref", "dft_decisionevidenceref", "dft_authorityevidenceref", "dft_scopeversionref" })
                payload[field] = new string('\u00e9', 256);
            var original = new DecisionAppend(payload, Record, "18446744073709551615", 0, null, Owner, EventOwner);
            string document = DecisionIntentDocument.Encode(Org, original);
            Assert.True(Encoding.UTF8.GetByteCount(document) < DecisionIntentDocument.MaximumBytes);
            Assert.True(original.Matches(DecisionIntentDocument.Restore(Org, document, DecisionConfirmationHash.Compute(Org, original)).ToEntity()));
        }
        [Fact]
        public void EveryStoredBusinessFieldParticipatesInTheDecodedIntent()
        {
            JsonObject payload = Object()["payload"]!.AsObject();
            var excluded = new HashSet<string>(StringComparer.Ordinal) { "dft_name", "dft_decisionkey", "dft_record", "dft_sequence", "ownerid" };
            Assert.Equal(Intent().Columns.Where(x => !excluded.Contains(x)).OrderBy(x => x, StringComparer.Ordinal), payload.Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal));
        }
        [Fact]
        public void StableValuesDoNotDependOnPropertyOrderWhitespaceOrAmbientCulture()
        {
            string original = Document(); var root = Object();
            var reversed = new JsonObject(); foreach (var item in root.Reverse()) reversed[item.Key] = item.Value?.DeepClone();
            string alternate = " \n" + reversed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.Equal(original, DecisionIntentDocument.Encode(Org, DecisionIntentDocument.Restore(Org, alternate, DecisionConfirmationHash.Compute(Org, Intent()))));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }
}
