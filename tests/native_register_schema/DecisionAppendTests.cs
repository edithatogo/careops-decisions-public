using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class DecisionAppendTests
    {
        internal static readonly Guid Record = Guid.Parse("10000000-0000-0000-0000-000000000001");
        internal static readonly Guid Decision = Guid.Parse("20000000-0000-0000-0000-000000000002");
        internal static readonly Guid Previous = Guid.Parse("30000000-0000-0000-0000-000000000003");
        internal static readonly DateTime Instant = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
        internal static Entity Payload()
        {
            var value = new Entity(RegisterSchemaPlan.DecisionTable, Decision);
            foreach (string name in new[] { "dft_actorref", "dft_function", "dft_profileversionref", "dft_decisionevidenceref", "dft_authorityevidenceref", "dft_scopeversionref" })
                value[name] = "synthetic-reference";
            value["dft_profilehash"] = new string('a', 64); value["dft_casehash"] = new string('b', 64);
            value["dft_scopetext"] = "Synthetic scope"; value["dft_conditionstext"] = ""; value["dft_supervisiontext"] = "";
            value["dft_validfrom"] = Instant; value["dft_validuntil"] = Instant.AddDays(30);
            value["dft_effectiveat"] = Instant; value["dft_recordedat"] = Instant;
            value["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 1);
            return value;
        }
        internal static DecisionAppend Intent(int sequence = 0, Guid? previous = null) => new DecisionAppend(Payload(), Record, "9", sequence, previous);

        [Fact]
        public void FreezesCompleteFirstEntryAndDerivesCanonicalStorageFields()
        {
            DecisionAppend intent = Intent(); Entity row = intent.ToEntity();
            Assert.Equal(Record, intent.RecordId); Assert.Equal(Decision, intent.DecisionId); Assert.Equal("9", intent.ExpectedVersion);
            Assert.Equal(0, intent.ExpectedSequence); Assert.Equal(1, intent.Sequence); Assert.Null(intent.PreviousDecision);
            Assert.Equal(Decision.ToString("N"), row["dft_decisionkey"]); Assert.Equal(Decision.ToString("D"), row["dft_name"]);
            Assert.Equal(Record, ((EntityReference)row["dft_record"]).Id); Assert.False(row.Contains("dft_supersedes"));
            Assert.True(intent.Matches(row)); Assert.Equal(20, intent.Columns.Length);
            string[] fields = intent.Columns; fields[0] = "tampered"; Assert.DoesNotContain("tampered", intent.Columns);
        }

        [Fact]
        public void LaterEntryBindsItsExactPredecessorAndSequence()
        {
            DecisionAppend intent = Intent(4, Previous); Entity row = intent.ToEntity();
            Assert.Equal(4, intent.ExpectedSequence); Assert.Equal(5, intent.Sequence); Assert.Equal(Previous, intent.PreviousDecision);
            Assert.Equal(5, row["dft_sequence"]); Assert.Equal(Previous, ((EntityReference)row["dft_supersedes"]).Id);
            Assert.True(intent.Matches(row)); Assert.Equal(21, intent.Columns.Length);
        }

        [Theory]
        [InlineData(null)] [InlineData("")] [InlineData("0")] [InlineData("01")] [InlineData("+1")]
        [InlineData(" 1")] [InlineData("-1")] [InlineData("1.0")] [InlineData("18446744073709551616")]
        public void InvalidVersionsCannotBecomeBlindUpdates(string? version) =>
            Assert.Throws<ArgumentException>(() => new DecisionAppend(Payload(), Record, version!, 0, null));

        [Fact]
        public void NativeMaximumVersionIsPreservedExactly() =>
            Assert.Equal("18446744073709551615", new DecisionAppend(Payload(), Record, "18446744073709551615", 0, null).ExpectedVersion);

        [Fact]
        public void RejectsMissingOrWrongIdentitiesAndImpossibleSequenceChains()
        {
            Assert.Throws<ArgumentNullException>(() => new DecisionAppend(null!, Record, "9", 0, null));
            Entity wrong = Payload(); wrong.LogicalName = "other";
            Assert.Throws<ArgumentException>(() => new DecisionAppend(wrong, Record, "9", 0, null));
            wrong = Payload(); wrong.Id = Guid.Empty;
            Assert.Throws<ArgumentException>(() => new DecisionAppend(wrong, Record, "9", 0, null));
            Assert.Throws<ArgumentException>(() => new DecisionAppend(Payload(), Guid.Empty, "9", 0, null));
            Assert.Throws<ArgumentException>(() => Intent(-1)); Assert.Throws<ArgumentException>(() => Intent(int.MaxValue, Previous));
            Assert.Throws<ArgumentException>(() => Intent(0, Previous)); Assert.Throws<ArgumentException>(() => Intent(1));
            Assert.Throws<ArgumentException>(() => Intent(1, Guid.Empty)); Assert.Throws<ArgumentException>(() => Intent(1, Decision));
            Assert.Equal(int.MaxValue, Intent(int.MaxValue - 1, Previous).Sequence);
        }

        [Fact]
        public void NoMissingOrExtraFieldIsSilentlyDefaulted()
        {
            foreach (string name in Payload().Attributes.Keys)
            {
                Entity missing = Payload(); missing.Attributes.Remove(name);
                Assert.Throws<ArgumentException>(() => new DecisionAppend(missing, Record, "9", 0, null));
                missing["dft_unknown"] = "x";
                Assert.Throws<ArgumentException>(() => new DecisionAppend(missing, Record, "9", 0, null));
            }
            Entity extra = Payload(); extra["dft_sequence"] = 99;
            Assert.Throws<ArgumentException>(() => new DecisionAppend(extra, Record, "9", 0, null));
        }

        [Fact]
        public void TextIsBoundedTypedAndExplicitRatherThanMissingRestrictions()
        {
            foreach (object bad in new object[] { 1, null!, "", "  ", new string('x', 257) })
            {
                Entity value = Payload(); value["dft_actorref"] = bad;
                Assert.Throws<ArgumentException>(() => new DecisionAppend(value, Record, "9", 0, null));
            }
            foreach (object bad in new object[] { 1, null!, new string('x', 32769) })
            {
                Entity value = Payload(); value["dft_conditionstext"] = bad;
                Assert.Throws<ArgumentException>(() => new DecisionAppend(value, Record, "9", 0, null));
            }
            Entity emptyScope = Payload(); emptyScope["dft_scopetext"] = "";
            Assert.Throws<ArgumentException>(() => new DecisionAppend(emptyScope, Record, "9", 0, null));
            Entity invalid = Payload(); invalid["dft_scopetext"] = "\ud800";
            Assert.Throws<EncoderFallbackException>(() => new DecisionAppend(invalid, Record, "9", 0, null));
            Entity maximum = Payload(); maximum["dft_conditionstext"] = new string('x', 32768);
            Assert.Equal(maximum["dft_conditionstext"], new DecisionAppend(maximum, Record, "9", 0, null).ToEntity()["dft_conditionstext"]);
        }

        [Theory]
        [InlineData("short")] [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        public void HashesAreExactLowercaseSha256(string hash)
        {
            Entity value = Payload(); value["dft_profilehash"] = hash;
            Assert.Throws<ArgumentException>(() => new DecisionAppend(value, Record, "9", 0, null));
        }

        [Fact]
        public void NoImplicitTimezoneConversionOrEmptyValidity()
        {
            foreach (object bad in new object[] { "2026-09-24", DateTime.SpecifyKind(Instant, DateTimeKind.Unspecified), DateTime.SpecifyKind(Instant, DateTimeKind.Local) })
            {
                Entity value = Payload(); value["dft_effectiveat"] = bad;
                Assert.Throws<ArgumentException>(() => new DecisionAppend(value, Record, "9", 0, null));
            }
            Entity empty = Payload(); empty["dft_validuntil"] = Instant;
            Assert.Throws<ArgumentException>(() => new DecisionAppend(empty, Record, "9", 0, null));
        }

        [Theory]
        [InlineData(602169999)] [InlineData(602170000)] [InlineData(602170005)]
        public void DefaultAndUnsupportedOutcomesCannotEnterTheJournal(int state)
        {
            Entity value = Payload(); value["dft_recordedstate"] = new OptionSetValue(state);
            Assert.Throws<ArgumentException>(() => new DecisionAppend(value, Record, "9", 0, null));
        }

        [Fact]
        public void TypedOutcomesAndDefensiveCopiesPreserveTheConfirmedSnapshot()
        {
            Entity bad = Payload(); bad["dft_recordedstate"] = 602170001;
            Assert.Throws<ArgumentException>(() => new DecisionAppend(bad, Record, "9", 0, null));
            for (int i = 1; i <= 4; i++)
            {
                Entity value = Payload(); var state = new OptionSetValue(RegisterSchemaPlan.Unverified + i); value["dft_recordedstate"] = state;
                var intent = new DecisionAppend(value, Record, "9", 1, Previous);
                state.Value = 0; value["dft_scopetext"] = "changed";
                Entity copy = intent.ToEntity(); ((OptionSetValue)copy["dft_recordedstate"]).Value = 0;
                ((EntityReference)copy["dft_record"]).Id = Guid.Empty; copy.Attributes.Clear();
                Assert.Equal(RegisterSchemaPlan.Unverified + i, ((OptionSetValue)intent.ToEntity()["dft_recordedstate"]).Value);
                Assert.Equal("Synthetic scope", intent.ToEntity()["dft_scopetext"]); Assert.True(intent.Matches(intent.ToEntity()));
            }
        }

        [Fact]
        public void ReplayedPayloadMustMatchEveryBusinessField()
        {
            DecisionAppend intent = Intent(1, Previous);
            foreach (string name in intent.Columns)
            {
                Entity row = intent.ToEntity(); row.Attributes.Remove(name); Assert.False(intent.Matches(row));
                row["dft_unknown"] = "x"; Assert.False(intent.Matches(row));
                row = intent.ToEntity(); row[name] = null!; Assert.False(intent.Matches(row));
                row = intent.ToEntity(); row[name] = new object(); Assert.False(intent.Matches(row));
            }
            Entity changed = intent.ToEntity(); changed["dft_conditionstext"] = "new restriction"; Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); changed["dft_sequence"] = 99; Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); changed["dft_recordedstate"] = new OptionSetValue(602170004); Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); changed["dft_effectiveat"] = Instant.AddSeconds(1); Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); changed["dft_effectiveat"] = DateTime.SpecifyKind(Instant, DateTimeKind.Unspecified); Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); ((EntityReference)changed["dft_record"]).Id = Previous; Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); ((EntityReference)changed["dft_record"]).LogicalName = "other"; Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); ((EntityReference)changed["dft_record"]).KeyAttributes.Add("other", "x"); Assert.False(intent.Matches(changed));
            Assert.False(intent.Matches(null!)); changed = intent.ToEntity(); changed.Id = Guid.NewGuid(); Assert.False(intent.Matches(changed));
            changed = intent.ToEntity(); changed.LogicalName = "other"; Assert.False(intent.Matches(changed));
        }

        [Fact]
        public void PayloadFieldsMatchTheActualDecisionSchemaWithoutCopyingSdkExtras()
        {
            DecisionAppend intent = Intent(1, Previous);
            var plan = RegisterSchemaPlan.Build();
            string[] fields = plan.OfType<CreateAttributeRequest>().Where(x => x.EntityName == RegisterSchemaPlan.DecisionTable).Select(x => x.Attribute.SchemaName)
                .Concat(plan.OfType<CreateOneToManyRequest>().Where(x => x.OneToManyRelationship.ReferencingEntity == RegisterSchemaPlan.DecisionTable).Select(x => x.Lookup.SchemaName))
                .Concat(new[] { "dft_name" }).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(fields, intent.Columns);
            Entity input = Payload(); input.FormattedValues["dft_actorref"] = "must-not-leak"; input.KeyAttributes["x"] = "no"; input.RowVersion = "111";
            Entity output = new DecisionAppend(input, Record, "9", 0, null).ToEntity();
            Assert.Empty(output.FormattedValues); Assert.Empty(output.KeyAttributes); Assert.Empty(output.RelatedEntities); Assert.Null(output.RowVersion);
        }
    }
}
