using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.Json;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    internal sealed class CurrentScopeHost : IServiceProvider, IOrganizationServiceFactory, IOrganizationService
    {
        internal readonly FakeDataverse Identity = new FakeDataverse();
        internal static readonly Guid RecordId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        internal static readonly Guid DecisionId = Guid.Parse("60000000-0000-0000-0000-000000000006");
        internal readonly Entity Root = new Entity(RegisterSchemaPlan.RecordTable, RecordId) { RowVersion = "42" };
        internal readonly EntityCollection History = new EntityCollection();
        internal readonly DateTime Now = DateTime.UtcNow;
        internal readonly List<string> Calls = new List<string>();
        internal int FactoryCalls, RootReads;
        internal bool MissingContext, MissingFactory, MissingSecondService, ChangeRoot;
        internal Exception? HistoryFailure;
        internal CurrentScopeHost()
        {
            var c = Identity.Context;
            c.MessageName = ReadCurrentRecordedScope.ApiName; c.Stage = 30; c.Mode = 0; c.Depth = 1;
            c.InputParameters["Target"] = new EntityReference(RegisterSchemaPlan.RecordTable, RecordId);
            Root["dft_latestsequence"] = 1; Root["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, DecisionId);
            Root["dft_sourcecheckedat"] = Now.AddDays(-1); Root["dft_sourcecurrentuntil"] = Now.AddDays(1);
            var row = new Entity(RegisterSchemaPlan.DecisionTable, DecisionId);
            row["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, RecordId); row["dft_sequence"] = 1;
            row["dft_recordedat"] = Now.AddDays(-2); row["dft_effectiveat"] = Now.AddDays(-1);
            row["dft_validfrom"] = Now.AddDays(-3); row["dft_validuntil"] = Now.AddDays(2);
            row["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 1);
            row["dft_scopeversionref"] = "synthetic-version"; row["dft_scopetext"] = "Synthetic scope";
            row["dft_conditionstext"] = "Synthetic restriction"; row["dft_supervisiontext"] = "Synthetic supervision";
            History.Entities.Add(row);
        }
        public object GetService(Type type)
        {
            if (type == typeof(IPluginExecutionContext)) return MissingContext ? null! : Identity.Context;
            Assert.Equal(typeof(IOrganizationServiceFactory), type); return MissingFactory ? null! : this;
        }
        public IOrganizationService CreateOrganizationService(Guid? user)
        {
            FactoryCalls++; Assert.Equal(FakeDataverse.UserId, user);
            IOrganizationService result = Identity.CreateOrganizationService(user);
            return result == null || (MissingSecondService && FactoryCalls == 2) ? null! : this;
        }
        public OrganizationResponse Execute(OrganizationRequest request) => Identity.Execute(request);
        public Entity Retrieve(string table, Guid id, ColumnSet columns)
        {
            if (table == "systemuser") return Identity.Retrieve(table, id, columns);
            Assert.Equal(RegisterSchemaPlan.RecordTable, table); Assert.Equal(RecordId, id);
            RootReads++; Calls.Add("root"); if (ChangeRoot && RootReads == 2) Root.RowVersion = "43";
            return Root;
        }
        public EntityCollection RetrieveMultiple(QueryBase request)
        {
            var query = Assert.IsType<QueryExpression>(request);
            if (query.EntityName == CallerResolver.PersonTable) return Identity.RetrieveMultiple(request);
            Assert.Equal(RegisterSchemaPlan.DecisionTable, query.EntityName); Calls.Add("history");
            if (HistoryFailure != null) throw HistoryFailure;
            return History;
        }
        public Guid Create(Entity row) => throw new NotSupportedException("No writes from read endpoint");
        public void Update(Entity row) => throw new NotSupportedException("No writes from read endpoint");
        public void Delete(string table, Guid id) => throw new NotSupportedException("No writes from read endpoint");
        public void Associate(string table, Guid id, Relationship r, EntityReferenceCollection rows) => throw new NotSupportedException();
        public void Disassociate(string table, Guid id, Relationship r, EntityReferenceCollection rows) => throw new NotSupportedException();
        internal void Run() => new ReadCurrentRecordedScope().Execute(this);
        internal void Denied()
        {
            var error = Assert.Throws<InvalidPluginExecutionException>(Run);
            Assert.Equal("Recorded scope read unavailable. No decision or scope has been changed.", error.Message);
            Assert.Null(error.InnerException);
            Assert.Single(Identity.Context.OutputParameters); Assert.False((bool)Identity.Context.OutputParameters["ReadSucceeded"]);
        }
    }

    public sealed class CurrentScopePluginTests
    {
        [Fact]
        public void ComposesActualCallerMappingWithSingleReaderAndReturnsOnlyRecordedData()
        {
            var h = new CurrentScopeHost(); DateTime start = DateTime.UtcNow;
            h.History.Entities[0]["dft_actorref"] = "private"; h.History.Entities[0].FormattedValues["private"] = "secret";
            h.Run(); var output = h.Identity.Context.OutputParameters;
            Assert.True((bool)output["ReadSucceeded"]); Assert.Equal(10, output.Count);
            Assert.Equal(2, h.FactoryCalls); Assert.Equal(FakeDataverse.UserId, h.Identity.FactoryUser);
            Assert.Equal(new[] { "root", "history", "root" }, h.Calls);
            Assert.Equal("ScopeRecorded", output["RecordedStatus"]); Assert.Equal("42", output["RootVersion"]);
            Assert.Equal(1, output["LatestSequence"]); Assert.Equal(1, output["SelectedSequence"]);
            Assert.True((bool)output["HasEffectiveDecision"]); Assert.True((bool)output["CurrentnessVerified"]);
            Assert.InRange(Assert.IsType<DateTime>(output["AsOfUtc"]), start, DateTime.UtcNow);
            Assert.Equal("recorded-data-only-not-practice-clearance", output["ReadDisposition"]);
            var row = Assert.IsType<Entity>(output["SelectedDecision"]);
            Assert.Equal("Synthetic restriction", row["dft_conditionstext"]); Assert.Equal("Synthetic supervision", row["dft_supervisiontext"]);
            Assert.False(row.Contains("dft_actorref")); Assert.Empty(row.FormattedValues); Assert.Empty(row.RelatedEntities);
            ((OptionSetValue)row["dft_recordedstate"]).Value = -1;
            Assert.Equal(RegisterSchemaPlan.Unverified + 1, ((OptionSetValue)h.History.Entities[0]["dft_recordedstate"]).Value);
        }

        [Fact]
        public void EmptyOrFutureHistoryIsSuccessfulRetrievalNotCredentialing()
        {
            foreach (bool empty in new[] { false, true })
            {
                var h = new CurrentScopeHost();
                if (empty) { h.Root["dft_latestsequence"] = 0; h.Root.Attributes.Remove("dft_latestdecision"); h.History.Entities.Clear(); }
                else h.History.Entities[0]["dft_effectiveat"] = h.Now.AddDays(1);
                h.Run(); var output = h.Identity.Context.OutputParameters;
                Assert.True((bool)output["ReadSucceeded"]); Assert.False((bool)output["HasEffectiveDecision"]);
                Assert.Null(output["SelectedDecision"]); Assert.Equal("NoEffectiveDecision", output["RecordedStatus"]);
            }
        }

        [Fact]
        public void ExpiredVerificationNeverErasesARecordedRevocation()
        {
            var h = new CurrentScopeHost(); h.Root["dft_sourcecurrentuntil"] = h.Now.AddHours(-1);
            h.History.Entities[0]["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 3);
            h.Run(); Assert.Equal("Revoked", h.Identity.Context.OutputParameters["RecordedStatus"]);
            Assert.False((bool)h.Identity.Context.OutputParameters["CurrentnessVerified"]);
        }

        [Theory]
        [InlineData("MessageName", "other")] [InlineData("Stage", 20)] [InlineData("Mode", 1)]
        [InlineData("Depth", 2)] [InlineData("IsExecutingOffline", true)] [InlineData("IsOfflinePlayback", true)]
        public void RejectsWrongExecutionRouteBeforeIdentityReads(string field, object value)
        {
            var h = new CurrentScopeHost(); typeof(RemoteExecutionContext).GetProperty(field)!.SetValue(h.Identity.Context, value);
            h.Denied(); Assert.Equal(0, h.FactoryCalls); Assert.Empty(h.Calls);
        }

        [Fact]
        public void NestedOperationCannotMasqueradeAsDirectHumanRead()
        {
            var h = new CurrentScopeHost(); h.Identity.Context.ParentContext = new RemoteExecutionContext();
            h.Denied(); Assert.Equal(0, h.FactoryCalls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
        public void OnlyTheExactRecordTargetIsAccepted(int change)
        {
            var h = new CurrentScopeHost(); var inputs = h.Identity.Context.InputParameters;
            if (change == 0) inputs.Clear();
            if (change == 1) { inputs.Clear(); inputs["Other"] = CurrentScopeHost.RecordId; }
            if (change == 2) inputs["Target"] = "untrusted";
            if (change == 3) ((EntityReference)inputs["Target"]).LogicalName = "other";
            if (change == 4) ((EntityReference)inputs["Target"]).Id = Guid.Empty;
            if (change == 5) ((EntityReference)inputs["Target"]).KeyAttributes.Add("id", "x");
            if (change == 6) inputs["AsOfUtc"] = DateTime.UtcNow.AddYears(-1);
            h.Denied(); Assert.Equal(0, h.FactoryCalls); Assert.Empty(h.Calls);
        }

        [Fact]
        public void MissingHostServicesDoNotProduceDefaultSuccess()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadCurrentRecordedScope().Execute(null!));
            var context = new CurrentScopeHost { MissingContext = true };
            Assert.Throws<InvalidPluginExecutionException>(context.Run); Assert.Equal(0, context.FactoryCalls);
            var factory = new CurrentScopeHost { MissingFactory = true }; factory.Denied(); Assert.Equal(0, factory.FactoryCalls);
            var service = new CurrentScopeHost { MissingSecondService = true }; service.Denied(); Assert.Empty(service.Calls);
        }

        [Fact]
        public void UnverifiedIdentityIsNotReplacedByAServiceAccount()
        {
            var h = new CurrentScopeHost(); h.Identity.Rows.Entities.Clear(); h.Denied(); Assert.Empty(h.Calls);
            h = new CurrentScopeHost(); h.Identity.User["applicationid"] = Guid.NewGuid(); h.Denied(); Assert.Empty(h.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void EveryReadFailureIsGenericAndRemovesAnyStaleSuccess(int failure)
        {
            var h = new CurrentScopeHost();
            Exception[] errors = { new TimeoutException("private-timeout"), new FaultException<OrganizationServiceFault>(
                new OrganizationServiceFault { Message = "private-table" }), new InvalidPluginExecutionException("private-record"), new InvalidOperationException("private-internal") };
            h.HistoryFailure = errors[failure];
            h.Identity.Context.OutputParameters["ReadSucceeded"] = true;
            h.Identity.Context.OutputParameters["SelectedDecision"] = new Entity("private");
            h.Denied(); Assert.Equal(new[] { "root", "history" }, h.Calls);
        }

        [Fact]
        public void ChangedRootAndMissingRestrictionsDoNotReturnAPartialView()
        {
            var changed = new CurrentScopeHost { ChangeRoot = true }; changed.Denied(); Assert.Equal(2, changed.RootReads);
            var missing = new CurrentScopeHost(); missing.History.Entities[0].Attributes.Remove("dft_conditionstext"); missing.Denied();
            var invalid = new CurrentScopeHost(); invalid.History.Entities[0]["dft_scopetext"] = "\ud800"; invalid.Denied();
        }

        [Fact]
        public void ContractMatchesTheComposedEndpointWithoutClaimingRegistration()
        {
            using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "current-scope-read.contract.json"))))
            {
                var root = json.RootElement;
                Assert.Equal(ReadCurrentRecordedScope.ApiName, root.GetProperty("apiName").GetString());
                Assert.Equal(RegisterSchemaPlan.RecordTable, root.GetProperty("boundEntity").GetString());
                Assert.False(root.GetProperty("registered").GetBoolean());
                Assert.Equal("server-utc-now", root.GetProperty("evaluationTime").GetString());
                var h = new CurrentScopeHost(); h.Run();
                Assert.Equal(root.GetProperty("responses").EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal),
                    h.Identity.Context.OutputParameters.Keys.OrderBy(x => x, StringComparer.Ordinal));
            }
        }
    }
}
