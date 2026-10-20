using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class JournalAppenderTests
    {
        private sealed class ContextProxy : RealProxy
        {
            internal static readonly Guid Org = Guid.Parse("40000000-0000-0000-0000-000000000004");
            internal static readonly Guid User = Guid.Parse("50000000-0000-0000-0000-000000000005");
            internal readonly Dictionary<string, object?> Values = new Dictionary<string, object?>
            {
                ["MessageName"] = RegisterJournalAppender.ApiName, ["Stage"] = 30, ["Mode"] = 0,
                ["IsInTransaction"] = true, ["Depth"] = 1, ["ParentContext"] = null,
                ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false,
                ["OrganizationId"] = Org, ["InitiatingUserId"] = User, ["UserId"] = User
            };
            internal ContextProxy() : base(typeof(IPluginExecutionContext)) { }
            internal IPluginExecutionContext Context => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                if (!call.MethodName.StartsWith("get_", StringComparison.Ordinal) || !Values.TryGetValue(call.MethodName.Substring(4), out object? result))
                    throw new NotSupportedException(call.MethodName);
                return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
            }
        }

        private sealed class Store : IOrganizationService
        {
            internal readonly List<string> Calls = new List<string>();
            internal EntityCollection Rows = new EntityCollection();
            internal Entity Root = new Entity(RegisterSchemaPlan.RecordTable, DecisionAppendTests.Record) { RowVersion = "9" };
            internal Entity Previous = new Entity(RegisterSchemaPlan.DecisionTable, DecisionAppendTests.Previous);
            internal OrganizationResponse Who = new WhoAmIResponse();
            internal OrganizationResponse UpdateResult = new UpdateResponse();
            internal Guid CreatedId = DecisionAppendTests.Decision;
            internal string? FailAt;
            internal readonly Exception Fault = new InvalidOperationException("synthetic SDK failure");
            internal Entity? Created;
            internal UpdateRequest? UpdateCommand;
            internal QueryExpression? Query;
            internal readonly List<ColumnSet> ReadColumns = new List<ColumnSet>();
            internal Store(int sequence = 0)
            {
                Root["dft_latestsequence"] = sequence;
                if (sequence != 0) Root["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, DecisionAppendTests.Previous);
                Root["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 3);
                Root["dft_conditionstext"] = "existing restrictions";
                Root["dft_supervisiontext"] = "existing supervision";
                Root["dft_projectedasof"] = DecisionAppendTests.Instant;
                Previous["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, DecisionAppendTests.Record);
                Previous["dft_sequence"] = sequence;
                Previous["dft_recordedat"] = DecisionAppendTests.Instant;
                Who.Results["OrganizationId"] = ContextProxy.Org; Who.Results["UserId"] = ContextProxy.User;
            }
            private void Call(string name) { Calls.Add(name); if (FailAt == name) throw Fault; }
            public EntityCollection RetrieveMultiple(QueryBase query) { Call("query"); Query = Assert.IsType<QueryExpression>(query); return Rows; }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                Call(table == RegisterSchemaPlan.RecordTable ? "root" : "previous"); ReadColumns.Add(columns);
                if (table == RegisterSchemaPlan.RecordTable) { Assert.Equal(DecisionAppendTests.Record, id); return Root; }
                Assert.Equal(RegisterSchemaPlan.DecisionTable, table); Assert.Equal(DecisionAppendTests.Previous, id); return Previous;
            }
            public Guid Create(Entity entity) { Call("create"); Created = entity; return CreatedId; }
            public OrganizationResponse Execute(OrganizationRequest request)
            {
                if (request is WhoAmIRequest) { Call("who"); return Who; }
                UpdateCommand = Assert.IsType<UpdateRequest>(request); Call("update"); return UpdateResult;
            }
            public void Update(Entity entity) => throw new NotSupportedException("Blind update forbidden");
            public void Delete(string entityName, Guid id) => throw new NotSupportedException("Compensating delete forbidden");
            public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
            public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
        }

        private static JournalAppendDisposition Run(Store store, DecisionAppend? intent = null, ContextProxy? context = null) =>
            RegisterJournalAppender.Stage((context ?? new ContextProxy()).Context, store, intent ?? DecisionAppendTests.Intent());

        [Fact]
        public void FirstAppendUsesConditionalNativeUpdateAndNeverOverwritesScope()
        {
            var store = new Store(); DecisionAppend intent = DecisionAppendTests.Intent();
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store, intent));
            Assert.Equal(new[] { "who", "query", "root", "create", "update" }, store.Calls);
            Assert.NotNull(store.Created); Assert.True(intent.Matches(store.Created));
            UpdateRequest command = Assert.IsType<UpdateRequest>(store.UpdateCommand);
            Assert.Equal(ConcurrencyBehavior.IfRowVersionMatches, command.ConcurrencyBehavior);
            Assert.Equal(RegisterSchemaPlan.RecordTable, command.Target.LogicalName); Assert.Equal(intent.RecordId, command.Target.Id);
            Assert.Equal("9", command.Target.RowVersion); Assert.Equal(1, command.Target["dft_latestsequence"]);
            Assert.Equal(intent.DecisionId, ((EntityReference)command.Target["dft_latestdecision"]).Id);
            Assert.Null(command.Target["dft_projectedasof"]);
            Assert.Equal(new[] { "dft_latestdecision", "dft_latestsequence", "dft_projectedasof" }, command.Target.Attributes.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal("existing restrictions", store.Root["dft_conditionstext"]);
            Assert.Equal("existing supervision", store.Root["dft_supervisiontext"]);
            Assert.Equal(RegisterSchemaPlan.Unverified + 3, ((OptionSetValue)store.Root["dft_recordedstate"]).Value);
            QueryExpression query = Assert.IsType<QueryExpression>(store.Query);
            Assert.Equal(RegisterSchemaPlan.DecisionTable, query.EntityName); Assert.Equal(2, query.TopCount);
            Assert.False(query.ColumnSet.AllColumns); Assert.Equal(intent.Columns, query.ColumnSet.Columns);
            ConditionExpression filter = Assert.Single(query.Criteria.Conditions);
            Assert.Equal(RegisterSchemaPlan.DecisionTable + "id", filter.AttributeName);
            Assert.Equal(ConditionOperator.Equal, filter.Operator); Assert.Equal(intent.DecisionId, Assert.Single(filter.Values));
            Assert.Equal(new[] { "dft_latestsequence", "dft_latestdecision" }, Assert.Single(store.ReadColumns).Columns);
        }

        [Fact]
        public void LaterAppendChecksPredecessorBelongsToTheSameRecordBeforeWriting()
        {
            var store = new Store(4); DecisionAppend intent = DecisionAppendTests.Intent(4, DecisionAppendTests.Previous);
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store, intent));
            Assert.Equal(new[] { "who", "query", "root", "previous", "create", "update" }, store.Calls);
            Assert.Equal(new[] { "dft_record", "dft_sequence", "dft_recordedat" }, store.ReadColumns[1].Columns);
            Assert.Equal(5, store.Created!["dft_sequence"]); Assert.Equal(DecisionAppendTests.Previous, ((EntityReference)store.Created["dft_supersedes"]).Id);
        }

        [Fact]
        public void ExactHistoricalReplayReturnsOnlyTheReceiptWithoutReapplyingAnOldGrant()
        {
            var store = new Store(99) { FailAt = "root" }; DecisionAppend intent = DecisionAppendTests.Intent();
            store.Rows.Entities.Add(intent.ToEntity());
            Assert.Equal(JournalAppendDisposition.AlreadyRecorded, Run(store, intent));
            Assert.Equal(new[] { "who", "query" }, store.Calls); Assert.Null(store.Created); Assert.Null(store.UpdateCommand);
            Assert.Equal(99, store.Root["dft_latestsequence"]);
            Assert.Equal(RegisterSchemaPlan.Unverified + 3, ((OptionSetValue)store.Root["dft_recordedstate"]).Value);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void UnavailableAmbiguousOrAlteredReceiptsCannotBecomeNewWrites(int change)
        {
            var store = new Store();
            if (change == 0) store.Rows = null!;
            if (change == 1) store.Rows.MoreRecords = true;
            if (change == 2) { store.Rows.Entities.Add(DecisionAppendTests.Intent().ToEntity()); store.Rows.Entities.Add(DecisionAppendTests.Intent().ToEntity()); }
            if (change == 3) { Entity altered = DecisionAppendTests.Intent().ToEntity(); altered["dft_scopetext"] = "altered"; store.Rows.Entities.Add(altered); }
            if (change == 4) store.Rows.Entities.Add(null!);
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Equal(new[] { "who", "query" }, store.Calls);
        }

        [Theory]
        [InlineData("MessageName", "Other")] [InlineData("Stage", 20)] [InlineData("Mode", 1)]
        [InlineData("IsInTransaction", false)] [InlineData("Depth", 2)]
        [InlineData("IsExecutingOffline", true)] [InlineData("IsOfflinePlayback", true)]
        public void WrongExecutionContextDoesNotReachTheService(string field, object value)
        {
            var context = new ContextProxy(); context.Values[field] = value; var store = new Store();
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store, context: context)); Assert.Empty(store.Calls);
        }

        [Fact]
        public void NestedOrUnboundPrincipalsAreNotAccepted()
        {
            foreach (string field in new[] { "OrganizationId", "InitiatingUserId", "UserId", "ParentContext" })
            {
                var context = new ContextProxy(); context.Values[field] = field == "ParentContext" ? (object)context.Context : Guid.Empty;
                var store = new Store(); Assert.Throws<InvalidPluginExecutionException>(() => Run(store, context: context)); Assert.Empty(store.Calls);
            }
        }

        [Fact]
        public void MissingArgumentsCannotInvokeStorage()
        {
            var store = new Store(); var context = new ContextProxy(); var intent = DecisionAppendTests.Intent();
            Assert.Throws<ArgumentNullException>(() => RegisterJournalAppender.Stage(null!, store, intent));
            Assert.Throws<ArgumentNullException>(() => RegisterJournalAppender.Stage(context.Context, null!, intent));
            Assert.Throws<ArgumentNullException>(() => RegisterJournalAppender.Stage(context.Context, store, null!));
            Assert.Empty(store.Calls); Assert.Equal(JournalAppendDisposition.Unverified, default(JournalAppendDisposition));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void ServiceAccountOrOrganizationMismatchFailsBeforeDataReads(int change)
        {
            var store = new Store();
            if (change == 0) store.Who = null!;
            if (change == 1) store.Who = new OrganizationResponse();
            if (change == 2) store.Who.Results["UserId"] = Guid.Empty;
            if (change == 3) store.Who.Results["OrganizationId"] = Guid.Empty;
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Equal(new[] { "who" }, store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
        public void ExistingRootRequiresExactVersionIdentityAndExplicitSequence(int change)
        {
            var store = new Store();
            if (change == 0) store.Root = null!;
            if (change == 1) store.Root.LogicalName = "other";
            if (change == 2) store.Root.Id = Guid.Empty;
            if (change == 3) store.Root.RowVersion = "10";
            if (change == 4) store.Root.Attributes.Remove("dft_latestsequence");
            if (change == 5) store.Root["dft_latestsequence"] = "0";
            if (change == 6) store.Root["dft_latestsequence"] = 1;
            if (change == 7) store.Root["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, DecisionAppendTests.Previous);
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Equal(new[] { "who", "query", "root" }, store.Calls);
        }

        [Fact]
        public void AnExplicitNullInitialPointerDoesNotCreateAPredecessor()
        {
            var store = new Store(); store.Root["dft_latestdecision"] = null!;
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store)); Assert.DoesNotContain("previous", store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void LaterRootMustPointToTheExpectedPredecessor(int change)
        {
            var store = new Store(1);
            if (change == 0) store.Root.Attributes.Remove("dft_latestdecision");
            if (change == 1) store.Root["dft_latestdecision"] = "untrusted";
            if (change == 2) ((EntityReference)store.Root["dft_latestdecision"]).LogicalName = "other";
            if (change == 3) ((EntityReference)store.Root["dft_latestdecision"]).Id = Guid.Empty;
            if (change == 4) ((EntityReference)store.Root["dft_latestdecision"]).KeyAttributes.Add("id", "other");
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store, DecisionAppendTests.Intent(1, DecisionAppendTests.Previous)));
            Assert.Equal(new[] { "who", "query", "root" }, store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
        public void CrossRecordOrCorruptPredecessorFailsBeforeTheFirstWrite(int change)
        {
            var store = new Store(1);
            if (change == 0) store.Previous = null!;
            if (change == 1) store.Previous.LogicalName = "other";
            if (change == 2) store.Previous.Id = Guid.Empty;
            if (change == 3) store.Previous.Attributes.Remove("dft_record");
            if (change == 4) ((EntityReference)store.Previous["dft_record"]).Id = DecisionAppendTests.Previous;
            if (change == 5) store.Previous.Attributes.Remove("dft_sequence");
            if (change == 6) store.Previous["dft_sequence"] = "1";
            if (change == 7) store.Previous["dft_sequence"] = 2;
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store, DecisionAppendTests.Intent(1, DecisionAppendTests.Previous)));
            Assert.Equal(new[] { "who", "query", "root", "previous" }, store.Calls);
        }

        [Theory]
        [InlineData("who")] [InlineData("query")] [InlineData("root")] [InlineData("previous")] [InlineData("create")] [InlineData("update")]
        public void EverySdkFailureEscapesWithoutRetryOrCompensatingActions(string phase)
        {
            var store = new Store(1) { FailAt = phase };
            Exception error = Assert.Throws<InvalidOperationException>(() => Run(store, DecisionAppendTests.Intent(1, DecisionAppendTests.Previous)));
            Assert.Same(store.Fault, error);
            string[] order = { "who", "query", "root", "previous", "create", "update" };
            Assert.Equal(order.Take(Array.IndexOf(order, phase) + 1), store.Calls);
        }

        [Fact]
        public void WrongCreatedIdentityAbortsBeforeConditionalUpdate()
        {
            var store = new Store { CreatedId = Guid.Empty };
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Equal(new[] { "who", "query", "root", "create" }, store.Calls);
        }

        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void MissingOrWrongUpdateResponseIsNeverReportedAsStaged(bool missing)
        {
            var store = new Store { UpdateResult = missing ? null! : new OrganizationResponse() };
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Equal(new[] { "who", "query", "root", "create", "update" }, store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void MissingAmbiguousOrReversedRecordingTimeCannotStageWrites(int change)
        {
            var store = new Store(1);
            if (change == 0) store.Previous.Attributes.Remove("dft_recordedat");
            if (change == 1) store.Previous["dft_recordedat"] = null!;
            if (change == 2) store.Previous["dft_recordedat"] = "2026-09-24";
            if (change == 3) store.Previous["dft_recordedat"] = DateTime.SpecifyKind(DecisionAppendTests.Instant, DateTimeKind.Unspecified);
            if (change == 4) store.Previous["dft_recordedat"] = DateTime.SpecifyKind(DecisionAppendTests.Instant, DateTimeKind.Local);
            if (change == 5) store.Previous["dft_recordedat"] = DecisionAppendTests.Instant.AddTicks(1);
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store, DecisionAppendTests.Intent(1, DecisionAppendTests.Previous)));
            Assert.Equal(new[] { "who", "query", "root", "previous" }, store.Calls);
            Assert.Null(store.Created); Assert.Null(store.UpdateCommand);
        }

        [Theory]
        [InlineData(-1)] [InlineData(0)]
        public void RecordingTimeMayAdvanceOrTieWithoutChangingEffectiveDates(long priorOffset)
        {
            var store = new Store(1); store.Previous["dft_recordedat"] = DecisionAppendTests.Instant.AddTicks(priorOffset);
            Entity payload = DecisionAppendTests.Payload(); payload["dft_effectiveat"] = DecisionAppendTests.Instant.AddDays(10);
            var intent = new DecisionAppend(payload, DecisionAppendTests.Record, "9", 1, DecisionAppendTests.Previous);
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store, intent));
            Assert.Equal(2, store.Created!["dft_sequence"]);
            Assert.Equal(DecisionAppendTests.Instant.AddDays(10), store.Created["dft_effectiveat"]);
            Assert.Equal(DecisionAppendTests.Instant, store.Created["dft_recordedat"]);
        }

        [Fact]
        public void RecordingChronologyDoesNotInventAnEffectiveDateRule()
        {
            var store = new Store(1);
            Entity payload = DecisionAppendTests.Payload(); payload["dft_effectiveat"] = DecisionAppendTests.Instant.AddDays(-10);
            var intent = new DecisionAppend(payload, DecisionAppendTests.Record, "9", 1, DecisionAppendTests.Previous);
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store, intent));
            Assert.Equal(DecisionAppendTests.Instant.AddDays(-10), store.Created!["dft_effectiveat"]);
        }
    }
}
