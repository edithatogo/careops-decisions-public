using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class VersionedScopeReaderTests
    {
        private static readonly Guid Org = Guid.Parse("60000000-0000-0000-0000-000000000006");
        private static readonly Guid User = Guid.Parse("70000000-0000-0000-0000-000000000007");
        private static readonly Guid Record = RecordedScopeProjectionTests.Record;
        private static readonly DateTime At = RecordedScopeProjectionTests.At;
        private sealed class Context : RealProxy
        {
            internal readonly Dictionary<string, object?> Fields = new Dictionary<string, object?>
            {
                ["MessageName"] = VersionedScopeReader.ApiName, ["Stage"] = 30, ["Mode"] = 0,
                ["Depth"] = 1, ["ParentContext"] = null, ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false,
                ["OrganizationId"] = Org, ["InitiatingUserId"] = User, ["UserId"] = User
            };
            internal Context() : base(typeof(IPluginExecutionContext)) { }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                return new ReturnMessage(Fields[call.MethodName.Substring(4)], null, 0, call.LogicalCallContext, call);
            }
        }
        private static Entity Root(int count = 1)
        {
            var root = new Entity(RegisterSchemaPlan.RecordTable, Record) { RowVersion = "9" };
            root["dft_latestsequence"] = count;
            if (count > 0) root["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, RecordedScopeProjectionTests.Row(count).Id);
            root["dft_sourcecheckedat"] = At.AddDays(-1); root["dft_sourcecurrentuntil"] = At.AddDays(1);
            return root;
        }
        private sealed class Store : IOrganizationService
        {
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<ColumnSet> ReadColumns = new List<ColumnSet>();
            internal Entity First = Root(), Last = Root();
            internal EntityCollection History = RecordedScopeProjectionTests.History(RecordedScopeProjectionTests.Row());
            internal OrganizationResponse Who = new WhoAmIResponse();
            internal QueryExpression? Query;
            internal Action? DuringHistory;
            internal string? FailAt;
            internal readonly Exception Fault = new TimeoutException("synthetic read unavailable");
            internal Store() { Who.Results["UserId"] = User; Who.Results["OrganizationId"] = Org; }
            private void Call(string label) { Calls.Add(label); if (FailAt == label) throw Fault; }
            public OrganizationResponse Execute(OrganizationRequest request)
            { Assert.IsType<WhoAmIRequest>(request); Call("identity"); return Who; }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                Assert.Equal(RegisterSchemaPlan.RecordTable, table); Assert.Equal(Record, id);
                bool first = ReadColumns.Count == 0; ReadColumns.Add(columns);
                Call(first ? "root-before" : "root-after"); return first ? First : Last;
            }
            public EntityCollection RetrieveMultiple(QueryBase query)
            { Query = Assert.IsType<QueryExpression>(query); Call("history"); DuringHistory?.Invoke(); return History; }
            public Guid Create(Entity row) => throw new NotSupportedException("Read must not create");
            public void Update(Entity row) => throw new NotSupportedException("Read must not update");
            public void Delete(string table, Guid id) => throw new NotSupportedException("Read must not delete");
            public void Associate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
        }
        private static VersionedScopeRead Read(Store store, Context? context = null) =>
            VersionedScopeReader.Read((context ?? new Context()).Value, store, Record, At);

        [Fact]
        public void ReadsBoundedCompleteHistoryBetweenTwoUnchangedRootMarkers()
        {
            var store = new Store(); VersionedScopeRead result = Read(store);
            Assert.Equal(new[] { "identity", "root-before", "history", "root-after" }, store.Calls);
            Assert.Equal("9", result.RootVersion); Assert.Equal(At, result.View.AsOfUtc);
            Assert.Equal(RecordedScopeStatus.ScopeRecorded, result.View.Status); Assert.True(result.View.CurrentnessVerified);
            Assert.Equal("Must retain this condition", result.View.ToEntity()!["dft_conditionstext"]);
            Assert.False(result.View.ToEntity()!.Contains("dft_actorref"));
            QueryExpression query = Assert.IsType<QueryExpression>(store.Query);
            Assert.Equal(RegisterSchemaPlan.DecisionTable, query.EntityName); Assert.Equal(257, query.TopCount);
            Assert.False(query.ColumnSet.AllColumns); Assert.Equal(RecordedScopeProjection.Columns, query.ColumnSet.Columns);
            Assert.Empty(query.LinkEntities); Assert.Empty(query.Criteria.Filters);
            ConditionExpression filter = Assert.Single(query.Criteria.Conditions);
            Assert.Equal("dft_record", filter.AttributeName); Assert.Equal(ConditionOperator.Equal, filter.Operator); Assert.Equal(Record, Assert.Single(filter.Values));
            Assert.Equal(new[] { "dft_sequence", "dft_credentialingdecisionid" }, query.Orders.Select(x => x.AttributeName));
            Assert.All(query.Orders, order => Assert.Equal(OrderType.Ascending, order.OrderType));
            Assert.All(store.ReadColumns, columns =>
            {
                Assert.False(columns.AllColumns);
                Assert.Equal(new[] { "dft_latestsequence", "dft_latestdecision", "dft_sourcecheckedat", "dft_sourcecurrentuntil" }, columns.Columns);
            });
        }

        [Fact]
        public void VerifiedEmptyJournalAndMissingSourceLeaseRemainExplicitlyUnverified()
        {
            var store = new Store { First = Root(0), Last = Root(0), History = RecordedScopeProjectionTests.History() };
            foreach (Entity root in new[] { store.First, store.Last })
            {
                root["dft_latestdecision"] = null!; root.Attributes.Remove("dft_sourcecheckedat"); root["dft_sourcecurrentuntil"] = null!;
            }
            VersionedScopeRead result = Read(store); Assert.Equal(RecordedScopeStatus.NoEffectiveDecision, result.View.Status);
            Assert.False(result.View.CurrentnessVerified); Assert.Null(result.View.ToEntity());
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void AnyRelevantChangeAtSecondReadRejectsTheComputedView(int change)
        {
            var store = new Store();
            if (change == 0) store.Last.RowVersion = "10";
            if (change == 1) store.Last["dft_latestsequence"] = 2;
            if (change == 2) store.Last["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, Guid.NewGuid());
            if (change == 3) store.Last["dft_sourcecheckedat"] = At;
            if (change == 4) store.Last["dft_sourcecurrentuntil"] = At.AddDays(2);
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store));
            Assert.Equal(new[] { "identity", "root-before", "history", "root-after" }, store.Calls);
        }

        [Fact]
        public void CapturesFirstMarkerBeforeAnSdkObjectCanBeReusedAndMutated()
        {
            var store = new Store(); store.Last = store.First;
            store.DuringHistory = () => store.First["dft_sourcecurrentuntil"] = At.AddDays(2);
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
        [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)] [InlineData(16)] [InlineData(17)]
        public void InvalidFirstRootStopsBeforeHistoryRetrieval(int change)
        {
            var store = new Store();
            switch (change)
            {
                case 0: store.First = null!; break; case 1: store.First.LogicalName = "other"; break;
                case 2: store.First.Id = Guid.Empty; break; case 3: store.First.RowVersion = "bad"; break;
                case 4: store.First.RowVersion = "0"; break; case 5: store.First.RowVersion = "01"; break;
                case 6: store.First.Attributes.Remove("dft_latestsequence"); break;
                case 7: store.First["dft_latestsequence"] = "1"; break; case 8: store.First["dft_latestsequence"] = -1; break;
                case 9: store.First["dft_latestsequence"] = 257; break;
                case 10: store.First["dft_latestdecision"] = "untrusted"; break;
                case 11: ((EntityReference)store.First["dft_latestdecision"]).LogicalName = "other"; break;
                case 12: ((EntityReference)store.First["dft_latestdecision"]).Id = Guid.Empty; break;
                case 13: ((EntityReference)store.First["dft_latestdecision"]).KeyAttributes.Add("alias", "x"); break;
                case 14: store.First.Attributes.Remove("dft_latestdecision"); break;
                case 15: store.First["dft_latestsequence"] = 0; break;
                case 16: store.First["dft_sourcecheckedat"] = "unknown"; break;
                case 17: store.First["dft_sourcecurrentuntil"] = DateTime.SpecifyKind(At, DateTimeKind.Unspecified); break;
            }
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store)); Assert.Equal(new[] { "identity", "root-before" }, store.Calls);
        }

        [Fact]
        public void InvalidSecondRootIsNotAcceptedAfterAValidHistoryRead()
        {
            var store = new Store { Last = null! }; Assert.Throws<InvalidPluginExecutionException>(() => Read(store));
            Assert.Equal(new[] { "identity", "root-before", "history", "root-after" }, store.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void CallerOrOrganizationMismatchCannotReachTheRegister(int change)
        {
            var store = new Store();
            if (change == 0) store.Who = null!; if (change == 1) store.Who = new OrganizationResponse();
            if (change == 2) store.Who.Results["OrganizationId"] = Guid.Empty; if (change == 3) store.Who.Results["UserId"] = Guid.Empty;
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store)); Assert.Equal(new[] { "identity" }, store.Calls);
        }

        [Theory]
        [InlineData("MessageName", "other")] [InlineData("Stage", 20)] [InlineData("Mode", 1)] [InlineData("Depth", 2)]
        [InlineData("IsExecutingOffline", true)] [InlineData("IsOfflinePlayback", true)]
        public void WrongExecutionRouteStopsBeforeAnyServiceAccess(string field, object value)
        {
            var context = new Context(); context.Fields[field] = value; var store = new Store();
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store, context)); Assert.Empty(store.Calls);
        }

        [Fact]
        public void NestedOrUnboundCallersAndInvalidArgumentsFailWithoutServiceCalls()
        {
            foreach (string field in new[] { "ParentContext", "OrganizationId", "InitiatingUserId", "UserId" })
            {
                var context = new Context(); context.Fields[field] = field == "ParentContext" ? (object)context.Value : Guid.Empty;
                var store = new Store(); Assert.Throws<InvalidPluginExecutionException>(() => Read(store, context)); Assert.Empty(store.Calls);
            }
            var f = new Store(); var c = new Context();
            Assert.Throws<ArgumentNullException>(() => VersionedScopeReader.Read(null!, f, Record, At));
            Assert.Throws<ArgumentNullException>(() => VersionedScopeReader.Read(c.Value, null!, Record, At));
            Assert.Throws<InvalidPluginExecutionException>(() => VersionedScopeReader.Read(c.Value, f, Guid.Empty, At));
            Assert.Throws<InvalidPluginExecutionException>(() => VersionedScopeReader.Read(c.Value, f, Record, DateTime.SpecifyKind(At, DateTimeKind.Local)));
            Assert.Empty(f.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void PartialHiddenOrMalformedHistoryNeverFallsBackToRootScope(int change)
        {
            var store = new Store(); store.First["dft_scopetext"] = "untrusted cached scope";
            if (change == 0) store.History = null!;
            if (change == 1) store.History.MoreRecords = true;
            if (change == 2) store.History.Entities.Clear();
            if (change == 3) store.History.Entities[0].Attributes.Remove("dft_conditionstext");
            if (change == 4) store.History.TotalRecordCountLimitExceeded = true;
            Assert.Throws<InvalidPluginExecutionException>(() => Read(store)); Assert.Equal(new[] { "identity", "root-before", "history" }, store.Calls);
        }

        [Theory]
        [InlineData("identity")] [InlineData("root-before")] [InlineData("history")] [InlineData("root-after")]
        public void EverySdkFaultEscapesWithoutRetryOrElevation(string stage)
        {
            var store = new Store { FailAt = stage };
            Assert.Same(store.Fault, Assert.Throws<TimeoutException>(() => Read(store)));
            string[] order = { "identity", "root-before", "history", "root-after" };
            Assert.Equal(order.Take(Array.IndexOf(order, stage) + 1), store.Calls);
        }
    }
}
