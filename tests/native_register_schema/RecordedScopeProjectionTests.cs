using System;
using System.Linq;
using System.Text;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class RecordedScopeProjectionTests
    {
        internal static readonly DateTime At = DecisionAppendTests.Instant;
        internal static readonly Guid Record = DecisionAppendTests.Record;
        internal static Entity Row(int sequence = 1, int state = 1, int effectDays = -1, int recordedDays = -2)
        {
            Entity row = DecisionAppendTests.Payload(); row.Id = new Guid(sequence, 0, 0, new byte[8]);
            row["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, Record); row["dft_sequence"] = sequence;
            if (sequence > 1) row["dft_supersedes"] = new EntityReference(RegisterSchemaPlan.DecisionTable, new Guid(sequence - 1, 0, 0, new byte[8]));
            row["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + state);
            row["dft_recordedat"] = At.AddDays(recordedDays); row["dft_effectiveat"] = At.AddDays(effectDays);
            row["dft_validfrom"] = At.AddDays(-10); row["dft_validuntil"] = At.AddDays(20);
            row["dft_conditionstext"] = "Must retain this condition"; row["dft_supervisiontext"] = "Named supervision";
            return row;
        }
        internal static EntityCollection History(params Entity[] rows) => new EntityCollection(rows.ToList());
        private static RecordedScopeView Project(params Entity[] rows) => RecordedScopeProjection.Project(
            Record, rows.Length, rows.Length == 0 ? (Guid?)null : rows.Last().Id, History(rows), At, At.AddDays(-1), At.AddDays(1));

        [Fact]
        public void EmptyHistoryDoesNotInventPermission()
        {
            RecordedScopeView result = Project(); Assert.Equal(RecordedScopeStatus.NoEffectiveDecision, result.Status);
            Assert.Equal(RecordedScopeStatus.Unverified, default(RecordedScopeStatus)); Assert.Null(result.ToEntity());
            Assert.Equal(0, result.SelectedSequence); Assert.Equal(0, result.LatestSequence); Assert.Equal(Record, result.RecordId);
            Assert.Equal(At, result.AsOfUtc); Assert.Equal("recorded-data-only-not-practice-clearance", result.Disposition);
        }

        [Theory]
        [InlineData(1, RecordedScopeStatus.ScopeRecorded)] [InlineData(2, RecordedScopeStatus.Suspended)]
        [InlineData(3, RecordedScopeStatus.Revoked)] [InlineData(4, RecordedScopeStatus.Refused)]
        public void RecordedStateAndRestrictionsRemainVisible(int state, RecordedScopeStatus expected)
        {
            RecordedScopeView result = Project(Row(state: state)); Assert.Equal(expected, result.Status); Assert.True(result.CurrentnessVerified);
            Assert.Equal(1, result.SelectedSequence); Assert.Equal(1, result.LatestSequence);
            Assert.Equal("Must retain this condition", result.ToEntity()!["dft_conditionstext"]);
            Assert.Equal("Named supervision", result.ToEntity()!["dft_supervisiontext"]);
        }

        [Fact]
        public void OlderFutureGrantNeverRevivesAfterALaterRevocation()
        {
            Entity grant = Row(effectDays: 5); Entity revoked = Row(2, 3, 0, -1);
            var history = History(grant, revoked);
            foreach (int day in new[] { 0, 5, 25 })
            {
                var result = RecordedScopeProjection.Project(Record, 2, revoked.Id, history, At.AddDays(day), null, null);
                Assert.Equal(RecordedScopeStatus.Revoked, result.Status); Assert.Equal(2, result.SelectedSequence);
            }
        }

        [Fact]
        public void FutureRevocationDoesNotDisplaceCurrentGrantBeforeItsEffectiveInstant()
        {
            Entity grant = Row(); Entity revoked = Row(2, 3, 1, -1);
            Assert.Equal(1, Project(grant, revoked).SelectedSequence); Assert.Equal(2, Project(grant, revoked).LatestSequence);
            var active = RecordedScopeProjection.Project(Record, 2, revoked.Id, History(grant, revoked), At.AddDays(1), null, null);
            Assert.Equal(RecordedScopeStatus.Revoked, active.Status); Assert.Equal(2, active.SelectedSequence);
        }

        [Fact]
        public void LaterExplicitGrantCanSupersedeRevocationWithoutAutomaticallyReusingAnOlderGrant()
        {
            Assert.Equal(RecordedScopeStatus.ScopeRecorded, Project(Row(state: 3), Row(2, 1, -1, -1)).Status);
        }

        [Theory]
        [InlineData(1, -1)] [InlineData(-1, 1)]
        public void NeitherUnrecordedNorNotYetEffectiveDecisionIsSelected(int effect, int recording)
        {
            Assert.Equal(RecordedScopeStatus.NoEffectiveDecision, Project(Row(effectDays: effect, recordedDays: recording)).Status);
        }

        [Theory]
        [InlineData(-1, RecordedScopeStatus.GrantNotYetValid)] [InlineData(0, RecordedScopeStatus.ScopeRecorded)]
        [InlineData(1, RecordedScopeStatus.GrantExpired)]
        public void GrantValidityUsesHalfOpenIntervalsWithoutOlderFallback(int offset, RecordedScopeStatus expected)
        {
            Entity old = Row(); Entity newer = Row(2, recordedDays: -1);
            newer["dft_validfrom"] = At; newer["dft_validuntil"] = At.AddTicks(1);
            var result = RecordedScopeProjection.Project(Record, 2, newer.Id, History(old, newer), At.AddTicks(offset), null, null);
            Assert.Equal(expected, result.Status); Assert.Equal(2, result.SelectedSequence);
        }

        [Fact]
        public void EqualRecordingInstantsUseSequenceNotEffectiveTimeOrdering()
        {
            Entity older = Row(effectDays: 0, recordedDays: 0); Entity later = Row(2, 3, -5, 0);
            Assert.Equal(RecordedScopeStatus.Revoked, Project(older, later).Status);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void UnknownStaleOrInvalidCurrentnessDoesNotHideRecordedRevocation(int scenario)
        {
            DateTime? check = At.AddDays(-1), end = At.AddDays(1);
            if (scenario == 0) check = null; if (scenario == 1) end = null;
            if (scenario == 2) end = At; if (scenario == 3) end = At.AddDays(-2);
            if (scenario == 4) check = At.AddDays(2); if (scenario == 5) { check = null; end = null; }
            Entity row = Row(state: 3);
            var result = RecordedScopeProjection.Project(Record, 1, row.Id, History(row), At, check, end);
            Assert.False(result.CurrentnessVerified); Assert.Equal(RecordedScopeStatus.Revoked, result.Status);
        }

        [Fact]
        public void CurrentnessCheckStartsInclusivelyAndNeverGrantsScope()
        {
            Entity row = Row();
            Assert.True(RecordedScopeProjection.Project(Record, 1, row.Id, History(row), At, At, At.AddTicks(1)).CurrentnessVerified);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void AmbiguousRequestedAndVerificationInstantsAreRejected(int field)
        {
            DateTime? check = At, until = At.AddDays(1); DateTime at = At;
            DateTime ambiguous = DateTime.SpecifyKind(At, DateTimeKind.Unspecified);
            if (field == 0) at = ambiguous; if (field == 1) check = ambiguous; if (field == 2) until = ambiguous;
            Entity row = Row();
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(Record, 1, row.Id, History(row), at, check, until));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
        public void MissingTruncatedOrInconsistentHistoryCannotLookLikeACompleteView(int change)
        {
            Entity row = Row(); EntityCollection history = History(row); Guid record = Record; Guid? latest = row.Id; int count = 1;
            if (change == 0) record = Guid.Empty; if (change == 1) count = -1;
            if (change == 2) count = RecordedScopeProjection.MaximumHistory + 1;
            if (change == 3) latest = null; if (change == 4) latest = Guid.Empty;
            if (change == 5) history.MoreRecords = true; if (change == 6) history.TotalRecordCountLimitExceeded = true;
            if (change == 7) history.Entities.Clear(); if (change == 8) latest = Record;
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(record, count, latest, history, At, null, null));
        }

        [Fact]
        public void MissingHistoryOrPointerWithNoEntriesIsNotAnEmptyVerifiedRegister()
        {
            Assert.Throws<ArgumentNullException>(() => RecordedScopeProjection.Project(Record, 0, null, null!, At, null, null));
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(Record, 0, Record, History(), At, null, null));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
        public void ChainIdentitySequenceAndSupersessionMustMatchExactly(int change)
        {
            Entity a = Row(); Entity b = Row(2); EntityCollection history = History(a, b);
            if (change == 0) history.Entities[0] = null!; if (change == 1) a.LogicalName = "other";
            if (change == 2) a.Id = Guid.Empty; if (change == 3) b.Id = a.Id;
            if (change == 4) b["dft_sequence"] = 3; if (change == 5) ((EntityReference)a["dft_record"]).Id = Guid.Empty;
            if (change == 6) ((EntityReference)b["dft_supersedes"]).Id = Record;
            if (change == 7) a["dft_supersedes"] = new EntityReference(RegisterSchemaPlan.DecisionTable, Record);
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(Record, 2, b.Id, history, At, null, null));
        }

        [Fact]
        public void ReferencesCannotUseAnotherTableOrAlternateKey()
        {
            foreach (int kind in new[] { 0, 1 })
            {
                Entity row = Row(); var reference = (EntityReference)row["dft_record"];
                if (kind == 0) reference.LogicalName = "other"; else reference.KeyAttributes.Add("id", "x");
                Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
            }
        }

        [Fact]
        public void ExplicitNullFirstSupersessionIsAcceptedButMissingLaterSupersessionIsNot()
        {
            Entity first = Row(); first["dft_supersedes"] = null!; Assert.Equal(1, Project(first).SelectedSequence);
            Entity second = Row(2); second.Attributes.Remove("dft_supersedes");
            Assert.Throws<InvalidPluginExecutionException>(() => Project(first, second));
        }

        [Theory]
        [InlineData("dft_sequence")] [InlineData("dft_record")] [InlineData("dft_recordedstate")]
        [InlineData("dft_scopeversionref")] [InlineData("dft_scopetext")]
        [InlineData("dft_conditionstext")] [InlineData("dft_supervisiontext")]
        [InlineData("dft_validfrom")] [InlineData("dft_validuntil")] [InlineData("dft_effectiveat")] [InlineData("dft_recordedat")]
        public void EachMissingOrAccessFilteredFieldFailsRatherThanGuessing(string field)
        {
            Entity row = Row(); row.Attributes.Remove(field); Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
            row = Row(); row[field] = null!; Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
            row = Row(); row[field] = new object(); Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
        }

        [Fact]
        public void RejectsReversedChronologyInvalidIntervalsUnsupportedStatesAndAmbiguousRowTimes()
        {
            Assert.Throws<InvalidPluginExecutionException>(() => Project(Row(recordedDays: 0), Row(2, recordedDays: -1)));
            Entity row = Row(); row["dft_validuntil"] = row["dft_validfrom"]; Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
            Assert.Throws<InvalidPluginExecutionException>(() => Project(Row(state: 0)));
            Assert.Throws<InvalidPluginExecutionException>(() => Project(Row(state: 5)));
            row = Row(); row["dft_recordedat"] = DateTime.SpecifyKind(At, DateTimeKind.Local);
            Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
        }

        [Fact]
        public void ValidatesBoundedTextWithoutSilentlyRemovingRestrictions()
        {
            foreach (string name in new[] { "dft_scopeversionref", "dft_scopetext", "dft_conditionstext", "dft_supervisiontext" })
            {
                Entity row = Row(); row[name] = new string('x', 32769); Assert.Throws<InvalidPluginExecutionException>(() => Project(row));
                row = Row(); row[name] = "\ud800"; Assert.Throws<EncoderFallbackException>(() => Project(row));
            }
            Entity empty = Row(); empty["dft_scopetext"] = " "; Assert.Throws<InvalidPluginExecutionException>(() => Project(empty));
            empty = Row(); empty["dft_conditionstext"] = ""; empty["dft_supervisiontext"] = "";
            Assert.Equal("", Project(empty).ToEntity()!["dft_conditionstext"]);
        }

        [Fact]
        public void CopiesMutableValuesAndNeverPassesThroughEvidenceOrFormattedMetadata()
        {
            Entity source = Row(); source.FormattedValues.Add("private", "not for output"); source["extra"] = "not for output";
            RecordedScopeView view = Project(source); Entity output = view.ToEntity()!;
            source["dft_scopetext"] = "altered"; ((OptionSetValue)source["dft_recordedstate"]).Value = -1;
            ((OptionSetValue)output["dft_recordedstate"]).Value = -1; ((EntityReference)output["dft_record"]).Id = Guid.Empty;
            Assert.Equal("Synthetic scope", view.ToEntity()!["dft_scopetext"]);
            Assert.Equal(RegisterSchemaPlan.Unverified + 1, ((OptionSetValue)view.ToEntity()!["dft_recordedstate"]).Value);
            Assert.Equal(Record, ((EntityReference)view.ToEntity()!["dft_record"]).Id);
            Assert.False(output.Contains("dft_actorref")); Assert.False(output.Contains("dft_casehash")); Assert.False(output.Contains("extra"));
            Assert.Empty(output.FormattedValues); Assert.Empty(output.RelatedEntities);
            string[] columns = RecordedScopeProjection.Columns; columns[0] = "altered";
            Assert.DoesNotContain("altered", RecordedScopeProjection.Columns);
        }

        [Fact]
        public void AcceptsTheCompleteEngineeringBoundWithoutTruncation()
        {
            Entity[] rows = Enumerable.Range(1, RecordedScopeProjection.MaximumHistory).Select(i => Row(i)).ToArray();
            Assert.Equal(RecordedScopeProjection.MaximumHistory, Project(rows).SelectedSequence);
        }
    }
}
