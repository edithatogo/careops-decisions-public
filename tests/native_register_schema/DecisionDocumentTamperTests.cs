using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class DecisionDocumentTamperTests
    {
        private static readonly Guid Org = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        private static readonly Guid Record = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        private static readonly Guid Owner = Guid.Parse("dddddddd-0000-0000-0000-000000000004");
        private static readonly Guid Other = Guid.Parse("eeeeeeee-0000-0000-0000-000000000005");
        private static DecisionAppend Intent(Entity? payload = null) => new DecisionAppend(
            payload ?? DecisionIntentDocumentTests.Payload(), Record, "42", 0, null, Owner, Other);

        [Fact]
        public void EveryValidBusinessContentEditStillBreaksTheOriginalConfirmationBinding()
        {
            DecisionAppend original = Intent(); string digest = DecisionConfirmationHash.Compute(Org, original);
            string encoded = DecisionIntentDocument.Encode(Org, original);
            string[] fields = JsonNode.Parse(encoded)!["payload"]!.AsObject().Select(x => x.Key).ToArray();
            foreach (string field in fields)
            {
                JsonNode changed = JsonNode.Parse(encoded)!;
                if (field == "dft_recordedstate") changed["payload"]![field] = 602170002;
                else if (field.EndsWith("hash", StringComparison.Ordinal)) changed["payload"]![field] = new string('c', 64);
                else if (new[] { "dft_validfrom", "dft_validuntil", "dft_effectiveat", "dft_recordedat" }.Contains(field))
                    changed["payload"]![field] = ((DateTime)original.ToEntity()[field]).AddTicks(1).ToString("O", CultureInfo.InvariantCulture);
                else changed["payload"]![field] = "Different but otherwise valid synthetic content";
                Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, changed.ToJsonString(), digest));
            }
            foreach (string header in new[] { "record", "decision", "decisionOwner", "eventOwner" })
            {
                JsonNode changed = JsonNode.Parse(encoded)!; changed[header] = Org.ToString("D");
                Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, changed.ToJsonString(), digest));
            }
            JsonNode differentVersion = JsonNode.Parse(encoded)!; differentVersion["expectedVersion"] = "43";
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, differentVersion.ToJsonString(), digest));
        }

        [Fact]
        public void MalformedEscapedUnicodeCannotMasqueradeAsAnApprovedReplacementCharacter()
        {
            Entity payload = DecisionIntentDocumentTests.Payload(); payload["dft_conditionstext"] = "\uFFFD";
            DecisionAppend intent = Intent(payload); string encoded = DecisionIntentDocument.Encode(Org, intent);
            Assert.Contains("\\uFFFD", encoded);
            string malformed = encoded.Replace("\\uFFFD", "\\uD800");
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, malformed, DecisionConfirmationHash.Compute(Org, intent)));
        }

        [Fact]
        public void ExactByteBudgetAllowsValidPaddedDocumentButNeverOneByteMore()
        {
            DecisionAppend original = Intent(); string encoded = DecisionIntentDocument.Encode(Org, original);
            // Default JSON writer escapes non-ASCII, so the emitted document has one byte per character.
            string padded = encoded + new string(' ', DecisionIntentDocument.MaximumBytes - encoded.Length);
            string digest = DecisionConfirmationHash.Compute(Org, original);
            Assert.True(original.Matches(DecisionIntentDocument.Restore(Org, padded, digest).ToEntity()));
            Assert.Throws<ArgumentException>(() => DecisionIntentDocument.Restore(Org, padded + " ", digest));
        }
    }
}
