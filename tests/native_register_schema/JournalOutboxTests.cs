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
    public sealed class JournalOutboxTests
    {
        private sealed class Context : RealProxy
        {
            internal static readonly Guid Organization = Guid.Parse("40000000-0000-0000-0000-000000000004");
            internal static readonly Guid User = Guid.Parse("50000000-0000-0000-0000-000000000005");
            internal bool InTransaction = true;
            internal Context() : base(typeof(IPluginExecutionContext)) { }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                var fields = new Dictionary<string, object?>
                {
                    ["MessageName"] = RegisterJournalAppender.ApiName, ["Stage"] = 30, ["Mode"] = 0,
                    ["Depth"] = 1, ["IsInTransaction"] = InTransaction, ["ParentContext"] = null,
                    ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false,
                    ["OrganizationId"] = Organization, ["InitiatingUserId"] = User, ["UserId"] = User
                };
                return new ReturnMessage(fields[call.MethodName.Substring(4)], null, 0, call.LogicalCallContext, call);
            }
        }

        private sealed class Store : IOrganizationService
        {
            internal readonly List<string> Calls = new List<string>();
            internal EntityCollection Journal = new EntityCollection();
            internal EntityCollection Events = new EntityCollection();
            internal readonly List<Entity> Created = new List<Entity>();
            internal readonly List<QueryExpression> Queries = new List<QueryExpression>();
            internal UpdateRequest? Updated;
            internal string? FailAt;
            internal Guid EventCreatedId = DecisionAppendTests.Decision;
            internal readonly Exception Fault = new TimeoutException("synthetic service failure");
            private void Call(string label) { Calls.Add(label); if (FailAt == label) throw Fault; }
            public OrganizationResponse Execute(OrganizationRequest request)
            {
                if (request is WhoAmIRequest)
                {
                    Call("identity"); var response = new WhoAmIResponse();
                    response.Results["UserId"] = Context.User; response.Results["OrganizationId"] = Context.Organization; return response;
                }
                Call("version-update"); Updated = Assert.IsType<UpdateRequest>(request); return new UpdateResponse();
            }
            public EntityCollection RetrieveMultiple(QueryBase request)
            {
                var query = Assert.IsType<QueryExpression>(request); Queries.Add(query);
                if (query.EntityName == RegisterSchemaPlan.DecisionTable) { Call("journal-query"); return Journal; }
                Assert.Equal(DecisionEventSchema.Table, query.EntityName); Call("event-query"); return Events;
            }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                Call("root"); Assert.Equal(RegisterSchemaPlan.RecordTable, table); Assert.Equal(DecisionAppendTests.Record, id);
                var root = new Entity(table, id) { RowVersion = "9" }; root["dft_latestsequence"] = 0; return root;
            }
            public Guid Create(Entity row)
            {
                if (row.LogicalName == RegisterSchemaPlan.DecisionTable) { Call("journal-create"); Created.Add(row); return row.Id; }
                Assert.Equal(DecisionEventSchema.Table, row.LogicalName); Call("event-create"); Created.Add(row); return EventCreatedId;
            }
            public void Update(Entity row) => throw new NotSupportedException("No blind update");
            public void Delete(string table, Guid id) => throw new NotSupportedException("No compensation");
            public void Associate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
        }

        private static Entity ExpectedEvent()
        {
            var row = new Entity("dft_credentialingevent", DecisionAppendTests.Decision);
            row["dft_name"] = DecisionAppendTests.Decision.ToString("D"); row["dft_eventkey"] = DecisionAppendTests.Decision.ToString("N");
            row["dft_eventtype"] = "credentialing.decision.recorded.v1"; row["dft_eventversion"] = 1;
            row["dft_organizationref"] = Context.Organization.ToString("N");
            row["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, DecisionAppendTests.Record);
            row["dft_decision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, DecisionAppendTests.Decision);
            row["dft_sequence"] = 1; row["dft_recordedat"] = DecisionAppendTests.Instant; return row;
        }
        private static Store Replay()
        {
            var store = new Store { FailAt = "root" };
            store.Journal.Entities.Add(DecisionAppendTests.Intent().ToEntity()); store.Events.Entities.Add(ExpectedEvent()); return store;
        }
        private static JournalAppendDisposition Run(Store store) => JournalWithOutbox.Stage(new Context().Value, store, DecisionAppendTests.Intent());

        [Fact]
        public void StagesOneMinimumNecessaryEventAfterTheConditionalJournalUpdate()
        {
            var store = new Store(); Assert.Equal(JournalAppendDisposition.StagedInTransaction, Run(store));
            Assert.Equal(new[] { "identity", "journal-query", "root", "journal-create", "version-update", "event-create" }, store.Calls);
            Assert.Equal(2, store.Created.Count); Entity row = store.Created[1]; Entity expected = ExpectedEvent();
            Assert.Equal(expected.Id, row.Id); Assert.Equal(expected.LogicalName, row.LogicalName);
            Assert.Equal(9, row.Attributes.Count);
            foreach (var pair in expected.Attributes) Assert.Equal(pair.Value, row[pair.Key]);
            Assert.Equal(ConcurrencyBehavior.IfRowVersionMatches, store.Updated!.ConcurrencyBehavior);
            Assert.Null(store.Updated.Target["dft_projectedasof"]);
            Assert.DoesNotContain("dft_scopetext", row.Attributes.Keys); Assert.DoesNotContain("dft_actorref", row.Attributes.Keys);
            Assert.DoesNotContain("dft_casehash", row.Attributes.Keys); Assert.Empty(row.RelatedEntities);
        }

        [Fact]
        public void ExactHistoricalReplayDoesNotRewriteOrResendAnything()
        {
            var store = Replay(); Assert.Equal(JournalAppendDisposition.AlreadyRecorded, Run(store));
            Assert.Equal(new[] { "identity", "journal-query", "event-query" }, store.Calls);
            Assert.Empty(store.Created); Assert.Null(store.Updated);
            QueryExpression query = store.Queries[1]; Assert.Equal(2, query.TopCount); Assert.False(query.ColumnSet.AllColumns);
            Assert.Equal(ExpectedEvent().Attributes.Keys.OrderBy(x => x, StringComparer.Ordinal), query.ColumnSet.Columns);
            ConditionExpression filter = Assert.Single(query.Criteria.Conditions);
            Assert.Equal("dft_credentialingeventid", filter.AttributeName); Assert.Equal(ConditionOperator.Equal, filter.Operator);
            Assert.Equal(DecisionAppendTests.Decision, Assert.Single(filter.Values));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void MissingOrAmbiguousOldEventsAreNotAutomaticallyRepaired(int change)
        {
            var store = Replay();
            if (change == 0) store.Events = null!;
            if (change == 1) store.Events.Entities.Clear();
            if (change == 2) store.Events.MoreRecords = true;
            if (change == 3) store.Events.Entities.Add(ExpectedEvent());
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store));
            Assert.Empty(store.Created); Assert.Null(store.Updated);
            Assert.Equal(new[] { "identity", "journal-query", "event-query" }, store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
        [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)] [InlineData(16)] [InlineData(17)]
        public void AlteredOrIncompleteEventEnvelopesFailWithoutRequeueing(int change)
        {
            var store = Replay(); Entity row = store.Events.Entities[0];
            switch (change)
            {
                case 0: store.Events.Entities[0] = null!; break;
                case 1: row.LogicalName = "other"; break;
                case 2: row.Id = Guid.Empty; break;
                case 3: row["dft_credentialingeventid"] = "not-a-guid"; break;
                case 4: row["dft_credentialingeventid"] = Guid.Empty; break;
                case 5: row["unexpected"] = true; break;
                case 6: row.Attributes.Remove("dft_sequence"); break;
                case 7: row.Attributes.Remove("dft_sequence"); row["unexpected"] = 1; break;
                case 8: row["dft_sequence"] = null!; break;
                case 9: row["dft_sequence"] = "1"; break;
                case 10: row["dft_sequence"] = 2; break;
                case 11: row["dft_recordedat"] = DecisionAppendTests.Instant.AddTicks(1); break;
                case 12: row["dft_recordedat"] = DateTime.SpecifyKind(DecisionAppendTests.Instant, DateTimeKind.Unspecified); break;
                case 13: ((EntityReference)row["dft_record"]).Id = Guid.Empty; break;
                case 14: ((EntityReference)row["dft_record"]).LogicalName = "other"; break;
                case 15: ((EntityReference)row["dft_record"]).KeyAttributes.Add("key", "x"); break;
                case 16: row["dft_organizationref"] = Context.User.ToString("N"); break;
                case 17: row["dft_eventtype"] = "permission.granted"; break;
            }
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store)); Assert.Empty(store.Created); Assert.Null(store.Updated);
        }

        [Fact]
        public void NativePrimaryKeyTransportAndIrrelevantFormattedValuesAreNotRepublished()
        {
            var store = Replay(); Entity row = store.Events.Entities[0];
            row["dft_credentialingeventid"] = row.Id; row.FormattedValues.Add("sensitive", "not copied");
            Assert.Equal(JournalAppendDisposition.AlreadyRecorded, Run(store)); Assert.Empty(store.Created);
        }

        [Theory]
        [InlineData("identity")] [InlineData("journal-query")] [InlineData("root")]
        [InlineData("journal-create")] [InlineData("version-update")] [InlineData("event-create")]
        public void EveryStagingFaultEscapesWithoutRetryCompensationOrExternalAction(string phase)
        {
            var store = new Store { FailAt = phase };
            Assert.Same(store.Fault, Assert.Throws<TimeoutException>(() => Run(store)));
            string[] order = { "identity", "journal-query", "root", "journal-create", "version-update", "event-create" };
            Assert.Equal(order.Take(Array.IndexOf(order, phase) + 1), store.Calls);
        }

        [Fact]
        public void ReplayReadFailureIsNotAReasonToResend()
        {
            var store = Replay(); store.FailAt = "event-query";
            Assert.Same(store.Fault, Assert.Throws<TimeoutException>(() => Run(store))); Assert.Empty(store.Created);
        }

        [Fact]
        public void WrongEventIdentityMustAbortTheEnclosingTransaction()
        {
            var store = new Store { EventCreatedId = Guid.Empty };
            Assert.Throws<InvalidPluginExecutionException>(() => Run(store));
            Assert.Equal("event-create", store.Calls.Last()); Assert.Equal(2, store.Created.Count);
            // The fake records requests, NOT rollback. Native all-or-nothing qualification remains a separate gate.
        }

        [Fact]
        public void CannotUseTheOutboxWrapperToEscapeTransactionRequirements()
        {
            var store = new Store(); var context = new Context { InTransaction = false };
            Assert.Throws<InvalidPluginExecutionException>(() => JournalWithOutbox.Stage(context.Value, store, DecisionAppendTests.Intent()));
            Assert.Empty(store.Calls);
            Assert.Throws<ArgumentNullException>(() => JournalWithOutbox.Stage(null!, store, DecisionAppendTests.Intent()));
            Assert.Throws<ArgumentNullException>(() => JournalWithOutbox.Stage(new Context().Value, null!, DecisionAppendTests.Intent()));
            Assert.Throws<ArgumentNullException>(() => JournalWithOutbox.Stage(new Context().Value, store, null!));
            Assert.Empty(store.Calls);
        }
    }
}
