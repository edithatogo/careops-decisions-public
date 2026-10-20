using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class DecisionReceiptTests
    {
        private static readonly Guid RecordId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        private static readonly Guid DecisionId = Guid.Parse("60000000-0000-0000-0000-000000000006");
        private static readonly Guid Other = Guid.Parse("70000000-0000-0000-0000-000000000007");
        private const string Failure = "Recorded decision receipt is unavailable. No decision was repeated or changed. Do not infer that the original save failed.";
        // RemoteExecutionContext collections are read-only properties; malformed contexts use the interface instead.
        private sealed class NullPropertyContext : RealProxy
        {
            private readonly IPluginExecutionContext source;
            private readonly string property;
            internal NullPropertyContext(IPluginExecutionContext source, string property) : base(typeof(IPluginExecutionContext))
            { this.source = source; this.property = property; }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                object? value = call.MethodName == "get_" + property ? null : call.MethodBase.Invoke(source, call.Args);
                return new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
            }
        }
        internal sealed class Host : IServiceProvider, IOrganizationServiceFactory, IOrganizationService
        {
            internal readonly FakeDataverse Identity = new FakeDataverse();
            internal Entity Root = new Entity(RegisterSchemaPlan.RecordTable, RecordId);
            internal EntityCollection Rows = new EntityCollection();
            internal QueryExpression? ReceiptQuery;
            internal ColumnSet? RootColumns;
            internal int FactoryCalls, RootReads, ReceiptReads;
            internal bool MissingContext, MissingFactory, ThrowContext, ThrowFactory;
            internal bool FailRoot, FailReceipt, FailFinalRoot;
            internal string? NullContextProperty;
            internal Action? OnRootRead, OnFinalRootRead;
            internal Host()
            {
                var c = Identity.Context;
                c.MessageName = ReadOwnDecisionReceipt.ApiName; c.Stage = 30; c.Mode = 0; c.Depth = 1;
                c.InputParameters["Target"] = new EntityReference(RegisterSchemaPlan.RecordTable, RecordId);
                c.InputParameters["DecisionId"] = DecisionId;
                c.OutputParameters["old-private-output"] = "must be cleared";
                Rows.Entities.Add(new Entity(RegisterSchemaPlan.DecisionTable, DecisionId)
                {
                    ["dft_actorref"] = FakeDataverse.PersonId.ToString("D"), ["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, RecordId),
                    ["dft_decisionkey"] = DecisionId.ToString("N"), ["dft_recordedat"] = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ["dft_sequence"] = 1, ["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 1), ["dft_casehash"] = new string('a', 64)
                });
            }
            internal Entity Decision => Rows.Entities[0];
            public object GetService(Type type)
            {
                if (type == typeof(IPluginExecutionContext))
                {
                    if (ThrowContext) throw new InvalidOperationException("private native error");
                    if (MissingContext) return null!;
                    return NullContextProperty == null ? Identity.Context : new NullPropertyContext(Identity.Context, NullContextProperty).Value;
                }
                Assert.Equal(typeof(IOrganizationServiceFactory), type);
                if (ThrowFactory) throw new InvalidOperationException("private native error");
                return MissingFactory ? null! : this;
            }
            public IOrganizationService CreateOrganizationService(Guid? user)
            {
                FactoryCalls++; Assert.Equal(FakeDataverse.UserId, user);
                return Identity.CreateOrganizationService(user) == null ? null! : this;
            }
            public OrganizationResponse Execute(OrganizationRequest request) => Identity.Execute(request);
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                if (table == "systemuser") return Identity.Retrieve(table, id, columns);
                Assert.Equal(RegisterSchemaPlan.RecordTable, table); Assert.Equal(RecordId, id);
                RootReads++; RootColumns = columns;
                if (FailRoot || (FailFinalRoot && RootReads == 2)) throw new InvalidOperationException("private record access denial");
                OnRootRead?.Invoke();
                if (RootReads == 2) OnFinalRootRead?.Invoke();
                return Root;
            }
            public EntityCollection RetrieveMultiple(QueryBase request)
            {
                var q = Assert.IsType<QueryExpression>(request);
                if (q.EntityName == CallerResolver.PersonTable) return Identity.RetrieveMultiple(request);
                Assert.Equal(RegisterSchemaPlan.DecisionTable, q.EntityName); ReceiptReads++; ReceiptQuery = q;
                if (FailReceipt) throw new InvalidOperationException("private receipt access denial");
                return Rows;
            }
            public Guid Create(Entity row) => throw new NotSupportedException("No repeat save");
            public void Update(Entity row) => throw new NotSupportedException("No repair");
            public void Delete(string table, Guid id) => throw new NotSupportedException("No compensation");
            public void Associate(string table, Guid id, Relationship r, EntityReferenceCollection refs) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship r, EntityReferenceCollection refs) => throw new NotSupportedException();
            internal void Run() => new ReadOwnDecisionReceipt().Execute(this);
            internal void Denied()
            {
                var error = Assert.Throws<InvalidPluginExecutionException>(Run);
                Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
                var output = Identity.Context.OutputParameters;
                Assert.Equal(false, Assert.Single(output).Value); Assert.True(output.Contains("ReadSucceeded"));
            }
        }

        [Fact]
        public void RecoverOwnHistoricalReceiptWithoutCurrentDelegationNewApprovalOrAnotherConnection()
        {
            var h = new Host(); h.Run(); var o = h.Identity.Context.OutputParameters;
            Assert.True((bool)o["ReadSucceeded"]); Assert.Equal(ReadOwnDecisionReceipt.Disposition, o["ReadDisposition"]);
            Assert.Equal(RecordId, o["RecordId"]); Assert.Equal(DecisionId, o["DecisionId"]);
            Assert.Equal(1, o["Sequence"]); Assert.Equal(602170001, o["RecordedOutcome"]);
            Assert.Equal(new string('a', 64), o["CaseHash"]); Assert.Equal(h.Decision["dft_recordedat"], o["RecordedAtUtc"]);
            Assert.Equal(DateTimeKind.Utc, Assert.IsType<DateTime>(o["RetrievedAtUtc"]).Kind);
            Assert.Equal(9, o.Count); Assert.Equal(1, h.FactoryCalls); Assert.Equal(2, h.RootReads); Assert.Equal(1, h.ReceiptReads);
            Assert.Equal(new[] { "service", "WhoAmI", "retrieve-user", "RetrieveEntityKey", "query-person" }, h.Identity.Calls);
        }
        [Fact]
        public void NativeReadsAreBoundedMinimalAndRestrictedToTheOriginalActorAndRecord()
        {
            var h = new Host(); h.Run(); Assert.NotNull(h.RootColumns); Assert.False(h.RootColumns.AllColumns);
            Assert.Equal(new[] { "dft_credentialingrecordid" }, h.RootColumns.Columns);
            QueryExpression q = h.ReceiptQuery!;
            Assert.False(q.ColumnSet.AllColumns); Assert.Equal(2, q.TopCount); Assert.Empty(q.LinkEntities); Assert.Empty(q.Criteria.Filters);
            Assert.Equal(new[] { "dft_actorref", "dft_record", "dft_decisionkey", "dft_recordedat", "dft_sequence", "dft_recordedstate", "dft_casehash" }, q.ColumnSet.Columns);
            Assert.Equal(new[] { "dft_credentialingdecisionid", "dft_record", "dft_actorref" }, q.Criteria.Conditions.Select(c => c.AttributeName));
            Assert.All(q.Criteria.Conditions, c => Assert.Equal(ConditionOperator.Equal, c.Operator));
            Assert.Equal(DecisionId, Assert.Single(q.Criteria.Conditions[0].Values));
            Assert.Equal(RecordId, Assert.Single(q.Criteria.Conditions[1].Values));
            Assert.Equal(FakeDataverse.PersonId.ToString("D"), Assert.Single(q.Criteria.Conditions[2].Values));
        }
        [Theory]
        [InlineData(602170001)] [InlineData(602170002)] [InlineData(602170003)] [InlineData(602170004)]
        public void HistoricalOutcomesAreReturnedWithoutReactivatingOrReinterpretingThem(int outcome)
        {
            var h = new Host(); h.Decision["dft_recordedstate"] = new OptionSetValue(outcome);
            h.Root["dft_latestsequence"] = 50; h.Root["dft_recordedstate"] = new OptionSetValue(602170003);
            h.Run(); Assert.Equal(outcome, h.Identity.Context.OutputParameters["RecordedOutcome"]);
            Assert.Equal(ReadOwnDecisionReceipt.Disposition, h.Identity.Context.OutputParameters["ReadDisposition"]);
            Assert.Equal(50, h.Root["dft_latestsequence"]); Assert.Equal(602170003, ((OptionSetValue)h.Root["dft_recordedstate"]).Value);
        }
        [Fact]
        public void ExtraPrivateFieldsAndSubsequentEntityMutationNeverEscapeTheResponse()
        {
            var h = new Host(); h.Decision["dft_scopetext"] = "private scope";
            h.Decision["dft_decisionevidenceref"] = "private evidence";
            h.Decision.FormattedValues.Add("dft_recordedstate", "untrusted translated text");
            h.Decision.RelatedEntities.Add(new Relationship("private"), new EntityCollection());
            h.Run(); var o = h.Identity.Context.OutputParameters;
            h.Decision["dft_casehash"] = new string('b', 64); ((OptionSetValue)h.Decision["dft_recordedstate"]).Value = 602170003;
            Assert.Equal(new string('a', 64), o["CaseHash"]); Assert.Equal(602170001, o["RecordedOutcome"]);
            Assert.DoesNotContain(o.Values, value => value is Entity || value is EntityReference || value is OptionSetValue);
            Assert.Equal(9, o.Count);
        }
        [Fact]
        public void LostReadResponseCanBeRecoveredRepeatedlyWithoutWrites()
        {
            var h = new Host(); h.Run(); var first = h.Identity.Context.OutputParameters["RecordedAtUtc"];
            h.Run(); Assert.Equal(first, h.Identity.Context.OutputParameters["RecordedAtUtc"]);
            Assert.Equal(2, h.FactoryCalls); Assert.Equal(2, h.ReceiptReads); Assert.Single(h.Rows.Entities);
        }
        [Fact]
        public void MissingContextAndServiceFailuresDoNotExposeNativeFaults()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOwnDecisionReceipt().Execute(null!));
            foreach (bool throws in new[] { false, true })
            {
                var h = new Host { MissingContext = !throws, ThrowContext = throws };
                var error = Assert.Throws<InvalidPluginExecutionException>(h.Run); Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
                Assert.Equal(0, h.FactoryCalls);
            }
            var noOutput = new Host { NullContextProperty = "OutputParameters" };
            Assert.Equal(Failure, Assert.Throws<InvalidPluginExecutionException>(noOutput.Run).Message);
            new Host { MissingFactory = true }.Denied(); new Host { ThrowFactory = true }.Denied();
        }
        [Theory]
        [InlineData("MessageName", "Other")] [InlineData("Stage", 20)] [InlineData("Mode", 1)] [InlineData("Depth", 2)]
        [InlineData("IsExecutingOffline", true)] [InlineData("IsOfflinePlayback", true)]
        public void WrongPipelineHasNoDataAccess(string property, object value)
        {
            var h = new Host(); typeof(RemoteExecutionContext).GetProperty(property)!.SetValue(h.Identity.Context, value);
            h.Denied(); Assert.Equal(0, h.FactoryCalls);
        }
        [Fact]
        public void ParentContextAndCallerImpersonationCannotUseThisDirectHumanRead()
        {
            var h = new Host(); h.Identity.Context.ParentContext = new RemoteExecutionContext(); h.Denied(); Assert.Equal(0, h.FactoryCalls);
            h = new Host(); h.Identity.Context.UserId = Other; h.Denied(); Assert.Equal(0, h.RootReads);
        }
        [Fact]
        public void ExactTwoInputContractRejectsNamesFlagsExtraInputsAndUnresolvedTargets()
        {
            object?[] targets = { null, "name", new Entity(RegisterSchemaPlan.RecordTable, RecordId),
                new EntityReference("other", RecordId), new EntityReference(RegisterSchemaPlan.RecordTable, Guid.Empty),
                new EntityReference(RegisterSchemaPlan.RecordTable, RecordId) { KeyAttributes = { ["name"] = "alias" } } };
            foreach (object? target in targets)
            { var h = new Host(); h.Identity.Context.InputParameters["Target"] = target!; h.Denied(); Assert.Equal(0, h.FactoryCalls); }
            foreach (object? decision in new object?[] { null, "id", DecisionId.ToString("D"), Guid.Empty, new EntityReference(RegisterSchemaPlan.DecisionTable, DecisionId) })
            { var h = new Host(); h.Identity.Context.InputParameters["DecisionId"] = decision!; h.Denied(); Assert.Equal(0, h.FactoryCalls); }
            var extra = new Host(); extra.Identity.Context.InputParameters["Role"] = "administrator"; extra.Denied();
            foreach (string missing in new[] { "Target", "DecisionId" })
            {
                var h = new Host(); h.Identity.Context.InputParameters.Remove(missing); h.Identity.Context.InputParameters["Replacement"] = Other; h.Denied();
            }
            var none = new Host { NullContextProperty = "InputParameters" }; none.Denied();
        }
        [Fact]
        public void CurrentAccountAndRecordAccessStillApplyEvenToOwnHistoricalDecisions()
        {
            var h = new Host(); h.Identity.NoService = true; h.Denied(); Assert.Equal(0, h.RootReads);
            h = new Host(); h.Identity.User["applicationid"] = Other; h.Denied(); Assert.Equal(0, h.RootReads);
            h = new Host(); h.Identity.User["isdisabled"] = true; h.Denied(); Assert.Equal(0, h.RootReads);
            h = new Host { FailRoot = true }; h.Denied(); Assert.Equal(0, h.ReceiptReads);
            h = new Host { FailReceipt = true }; h.Denied(); Assert.Equal(1, h.ReceiptReads);
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void CanonicalNativeEntitiesAreRequired(bool decision)
        {
            for (int change = 0; change < 4; change++)
            {
                var h = new Host(); Entity row = decision ? h.Decision : h.Root;
                if (change == 0) { if (decision) h.Rows.Entities[0] = null!; else h.Root = null!; }
                if (change == 1) row.Id = Other;
                if (change == 2) row.LogicalName = "other";
                if (change == 3) row.KeyAttributes.Add("alias", "unresolved");
                h.Denied();
            }
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void NoVisibleAmbiguousOrIncompleteReceiptNeverMeansTheSaveFailed(int change)
        {
            var h = new Host();
            if (change == 0) h.Rows = null!;
            if (change == 1) h.Rows.MoreRecords = true;
            if (change == 2) h.Rows.Entities.Clear();
            if (change == 3) h.Rows.Entities.Add(h.Decision);
            h.Denied();
        }
        [Fact]
        public void AnotherActorsDecisionOrAnotherRecordsDecisionCannotBeReturned()
        {
            foreach (object? actor in new object?[] { null, 1, Other.ToString("D"), FakeDataverse.UserId.ToString("D") })
            { var h = new Host(); h.Decision["dft_actorref"] = actor!; h.Denied(); }
            var missing = new Host(); missing.Decision.Attributes.Remove("dft_actorref"); missing.Denied();
            foreach (object? reference in new object?[] { null, "id", new EntityReference("other", RecordId),
                new EntityReference(RegisterSchemaPlan.RecordTable, Other), new EntityReference(RegisterSchemaPlan.RecordTable, RecordId) { KeyAttributes = { ["alias"] = "x" } } })
            { var h = new Host(); h.Decision["dft_record"] = reference!; h.Denied(); }
            missing = new Host(); missing.Decision.Attributes.Remove("dft_record"); missing.Denied();
        }
        [Fact]
        public void StableDecisionKeyMustAgreeWithTheRequestedId()
        {
            foreach (object? key in new object?[] { null, 1, "", Other.ToString("N"), DecisionId.ToString("D") })
            { var h = new Host(); h.Decision["dft_decisionkey"] = key!; h.Denied(); }
            var missing = new Host(); missing.Decision.Attributes.Remove("dft_decisionkey"); missing.Denied();
        }
        [Fact]
        public void MissingOrCoercedSequenceStateTimestampAndHashCannotBecomeSuccess()
        {
            var invalid = new Dictionary<string, object?[]>
            {
                ["dft_sequence"] = new object?[] { null, "1", 0, -1, 1L },
                ["dft_recordedstate"] = new object?[] { null, 602170001, new OptionSetValue(602170000), new OptionSetValue(602170005) },
                ["dft_recordedat"] = new object?[] { null, "2020-01-01", DateTime.SpecifyKind(new DateTime(2020, 1, 1), DateTimeKind.Local),
                    new DateTime(2020, 1, 1), DateTime.UtcNow.AddDays(1) },
                ["dft_casehash"] = new object?[] { null, 1, "", new string('a', 63), new string('A', 64), new string('g', 64) }
            };
            foreach (var pair in invalid)
            {
                foreach (object? value in pair.Value) { var h = new Host(); h.Decision[pair.Key] = value!; h.Denied(); }
                var missing = new Host(); missing.Decision.Attributes.Remove(pair.Key); missing.Denied();
            }
        }
        [Fact]
        public void QueryAndResponseUseIdentifiersFrozenBeforeMutableInputChanges()
        {
            var h = new Host(); h.OnRootRead = () =>
            {
                ((EntityReference)h.Identity.Context.InputParameters["Target"]).Id = Other;
                h.Identity.Context.InputParameters["DecisionId"] = Other;
            };
            h.Run(); Assert.Equal(RecordId, h.Identity.Context.OutputParameters["RecordId"]);
            Assert.Equal(DecisionId, h.Identity.Context.OutputParameters["DecisionId"]);
            Assert.Equal(DecisionId, Assert.Single(h.ReceiptQuery!.Criteria.Conditions[0].Values));
        }
        [Fact]
        public void ColumnMetadataCannotBeChangedThroughAnEarlierRequest()
        {
            var h = new Host(); h.Run(); h.ReceiptQuery!.ColumnSet.Columns.Clear();
            h.Run(); Assert.Equal(7, h.ReceiptQuery!.ColumnSet.Columns.Count);
        }
        [Fact]
        public void AReadFailureAfterAnEarlierSuccessClearsThePreviousReceipt()
        {
            var h = new Host(); h.Run(); h.FailReceipt = true; h.Denied();
            Assert.Single(h.Identity.Context.OutputParameters);
        }
        [Fact]
        public void RootAccessRevokedDuringReceiptReadPreventsAnySuccessfulOutput()
        {
            var h = new Host { FailFinalRoot = true }; h.Denied();
            Assert.Equal(2, h.RootReads); Assert.Equal(1, h.ReceiptReads); Assert.Single(h.Rows.Entities);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void FinalRootReadMustStillBeTheSameCanonicalRecord(int change)
        {
            var h = new Host(); h.OnFinalRootRead = () =>
            {
                if (change == 0) h.Root = null!;
                if (change == 1) h.Root.Id = Other;
                if (change == 2) h.Root.LogicalName = "other";
                if (change == 3) h.Root.KeyAttributes.Add("alias", "unresolved");
            };
            h.Denied(); Assert.Equal(2, h.RootReads); Assert.Equal(1, h.ReceiptReads);
        }
        [Fact]
        public void FinalNativeCallCannotMutateTheValidatedReceiptPrimitives()
        {
            var h = new Host(); DateTime original = (DateTime)h.Decision["dft_recordedat"];
            h.OnFinalRootRead = () =>
            {
                ((OptionSetValue)h.Decision["dft_recordedstate"]).Value = 602170003;
                h.Decision["dft_sequence"] = 999; h.Decision["dft_casehash"] = new string('b', 64);
                h.Decision["dft_recordedat"] = DateTime.UtcNow.AddDays(1);
            };
            h.Run(); var output = h.Identity.Context.OutputParameters;
            Assert.Equal(602170001, output["RecordedOutcome"]); Assert.Equal(1, output["Sequence"]);
            Assert.Equal(new string('a', 64), output["CaseHash"]); Assert.Equal(original, output["RecordedAtUtc"]);
        }
    }
}
