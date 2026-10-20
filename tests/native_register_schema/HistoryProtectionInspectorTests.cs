using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class HistoryProtectionInspectorTests
    {
        private static Guid Id(int n) => Guid.Parse("10000000-0000-0000-0000-" + n.ToString("D12", CultureInfo.InvariantCulture));
        private const string Failure = "History protection configuration could not be verified. No configuration or credentialing record was changed.";
        private sealed class Store : IOrganizationService
        {
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<QueryExpression> Queries = new List<QueryExpression>();
            internal readonly Dictionary<Guid, Entity> Messages = new Dictionary<Guid, Entity>();
            internal readonly Dictionary<Guid, Entity> Filters = new Dictionary<Guid, Entity>();
            internal Entity Handler = new Entity("plugintype", Id(3)) { ["typename"] = "CareOps.Decisions.RegisterSchema.RejectHistoryMutation" };
            internal EntityCollection Steps = new EntityCollection();
            internal EntityCollection Settings = new EntityCollection();
            internal EntityCollection Images = new EntityCollection();
            internal OrganizationResponse Who = new WhoAmIResponse();
            internal string? FailAt;
            internal Store()
            {
                Who.Results["OrganizationId"] = Id(1); Who.Results["UserId"] = Id(2);
                int n = 0;
                foreach (string table in new[] { "dft_credentialingdecision", "dft_credentialingevent" })
                foreach (string operation in new[] { "Update", "Delete" })
                {
                    n++;
                    var message = new Entity("sdkmessage", Id(10 + n)) { ["name"] = operation };
                    var filter = new Entity("sdkmessagefilter", Id(20 + n))
                    {
                        ["sdkmessageid"] = new EntityReference("sdkmessage", message.Id), ["primaryobjecttypecode"] = table,
                        ["iscustomprocessingstepallowed"] = true, ["availability"] = 0
                    };
                    Messages.Add(message.Id, message); Filters.Add(filter.Id, filter);
                    Entity step = HistoryMutationStepDefinition.Build(Id(30 + n), 50, Handler, message, filter).Target;
                    step["statecode"] = new OptionSetValue(0); step["statuscode"] = new OptionSetValue(1); Steps.Entities.Add(step);
                }
            }
            private void Mark(string phase) { Calls.Add(phase); if (FailAt == phase) throw new TimeoutException("synthetic confidential native fault"); }
            public OrganizationResponse Execute(OrganizationRequest request)
            {
                Assert.IsType<WhoAmIRequest>(request); Mark("identity"); return Who;
            }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                Assert.False(columns.AllColumns);
                if (table == "plugintype") { Mark("handler"); Assert.Equal(Id(3), id); Assert.Equal(new[] { "typename" }, columns.Columns); return Handler; }
                if (table == "sdkmessage") { Mark("message"); Assert.Equal(new[] { "name" }, columns.Columns); return Messages[id]; }
                Assert.Equal("sdkmessagefilter", table); Mark("filter");
                Assert.Equal(new[] { "primaryobjecttypecode", "secondaryobjecttypecode", "sdkmessageid", "iscustomprocessingstepallowed", "availability" }, columns.Columns);
                return Filters[id];
            }
            public EntityCollection RetrieveMultiple(QueryBase request)
            {
                var query = Assert.IsType<QueryExpression>(request); Queries.Add(query);
                if (query.EntityName == "sdkmessageprocessingstepimage") { Mark("images"); return Images; }
                Assert.Equal("sdkmessageprocessingstep", query.EntityName);
                if (query.Criteria.Filters.Count == 0) { Mark("steps"); return Steps; }
                Mark("settings"); return Settings;
            }
            public Guid Create(Entity row) => throw new NotSupportedException("Read only");
            public void Update(Entity row) => throw new NotSupportedException("Read only");
            public void Delete(string table, Guid id) => throw new NotSupportedException("Read only");
            public void Associate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException("Read only");
            public void Disassociate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException("Read only");
        }
        private static System.Collections.ObjectModel.ReadOnlyCollection<Guid> Run(Store s) => HistoryProtectionInspector.Inspect(s, Id(1), Id(2), Id(3), 50);
        private static void Denied(Store s)
        {
            var error = Assert.Throws<InvalidPluginExecutionException>(() => Run(s));
            Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
        }

        [Fact]
        public void ActiveCompleteMatrixReturnsOnlyImmutableObservedStepIds()
        {
            var s = new Store(); var result = Run(s);
            Assert.Equal(new[] { Id(31), Id(32), Id(33), Id(34) }, result);
            Assert.Throws<NotSupportedException>(() => ((IList<Guid>)result)[0] = Id(99));
            s.Steps.Entities[0].Id = Id(99); Assert.Equal(Id(31), result[0]);
            Assert.Equal(new[] { "identity", "handler", "steps", "message", "filter", "message", "filter", "message", "filter", "message", "filter", "settings", "images" }, s.Calls);
        }
        [Fact]
        public void QueryDoesNotHideDisabledExtraOrBroadRegistrations()
        {
            var s = new Store(); Run(s); QueryExpression q = s.Queries[0];
            Assert.Equal(5, q.TopCount); Assert.False(q.ColumnSet.AllColumns); Assert.Empty(q.LinkEntities);
            ConditionExpression c = Assert.Single(q.Criteria.Conditions);
            Assert.Equal("eventhandler", c.AttributeName); Assert.Equal(ConditionOperator.Equal, c.Operator); Assert.Equal(Id(3), Assert.Single(c.Values));
            Assert.Empty(q.Criteria.Filters);
            Assert.Equal(new[] { "eventhandler", "sdkmessageid", "sdkmessagefilterid", "rank", "stage", "mode", "supporteddeployment", "statecode", "statuscode", "canbebypassed", "asyncautodelete", "iscustomizable" }, q.ColumnSet.Columns);
        }
        [Fact]
        public void ForbiddenSettingsAndImagesAreCheckedWithoutRetrievingTheirContents()
        {
            var s = new Store(); Run(s);
            foreach (QueryExpression q in s.Queries.Skip(1))
            {
                Assert.Equal(1, q.TopCount); Assert.False(q.ColumnSet.AllColumns);
                ConditionExpression c = Assert.Single(q.Criteria.Conditions);
                Assert.Equal("sdkmessageprocessingstepid", c.AttributeName); Assert.Equal(ConditionOperator.In, c.Operator);
                Assert.Equal(new[] { Id(31), Id(32), Id(33), Id(34) }, c.Values.Cast<Guid>().OrderBy(x => x));
            }
            Assert.Equal(new[] { "sdkmessageprocessingstepid" }, s.Queries[1].ColumnSet.Columns);
            FilterExpression any = Assert.Single(s.Queries[1].Criteria.Filters); Assert.Equal(LogicalOperator.Or, any.FilterOperator);
            Assert.Equal(new[] { "filteringattributes", "configuration", "impersonatinguserid", "sdkmessageprocessingstepsecureconfigid" }, any.Conditions.Select(x => x.AttributeName));
            Assert.All(any.Conditions, c => { Assert.Equal(ConditionOperator.NotNull, c.Operator); Assert.Empty(c.Values); });
            Assert.Equal(new[] { "sdkmessageprocessingstepimageid" }, s.Queries[2].ColumnSet.Columns);
            Assert.Empty(s.Queries[2].Criteria.Filters);
        }
        [Fact]
        public void InvalidDeploymentInputsFailBeforeReadingAnything()
        {
            var s = new Store();
            Assert.Throws<ArgumentNullException>(() => HistoryProtectionInspector.Inspect(null!, Id(1), Id(2), Id(3), 50));
            Assert.Throws<ArgumentException>(() => HistoryProtectionInspector.Inspect(s, Guid.Empty, Id(2), Id(3), 50));
            Assert.Throws<ArgumentException>(() => HistoryProtectionInspector.Inspect(s, Id(1), Guid.Empty, Id(3), 50));
            Assert.Throws<ArgumentException>(() => HistoryProtectionInspector.Inspect(s, Id(1), Id(2), Guid.Empty, 50));
            Assert.Throws<ArgumentException>(() => HistoryProtectionInspector.Inspect(s, Id(1), Id(2), Id(3), 0));
            Assert.Empty(s.Calls);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void ConnectionIdentityCannotBeAssumedFromTheSuppliedService(int change)
        {
            var s = new Store();
            if (change == 0) s.Who = null!;
            if (change == 1) s.Who = new OrganizationResponse();
            if (change == 2) s.Who.Results["OrganizationId"] = Id(99);
            if (change == 3) s.Who.Results["UserId"] = Id(99);
            Denied(s); Assert.Equal(new[] { "identity" }, s.Calls);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void MissingIncompleteDuplicateOrExcessStepCollectionsAreNotProtection(int change)
        {
            var s = new Store();
            if (change == 0) s.Steps = null!;
            if (change == 1) s.Steps.MoreRecords = true;
            if (change == 2) s.Steps.Entities.RemoveAt(0);
            if (change == 3) s.Steps.Entities.Add(s.Steps.Entities[0]);
            if (change == 4) s.Steps.Entities[1].Id = s.Steps.Entities[0].Id;
            Denied(s); Assert.DoesNotContain("settings", s.Calls);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void CanonicalHandlerStepMessageAndFilterRowsAreMandatory(int which)
        {
            for (int change = 0; change < 4; change++)
            {
                var s = new Store(); Entity row = which == 0 ? s.Handler : which == 1 ? s.Steps.Entities[0] : which == 2 ? s.Messages[Id(11)] : s.Filters[Id(21)];
                if (change == 0)
                {
                    if (which == 0) s.Handler = null!;
                    if (which == 1) s.Steps.Entities[0] = null!;
                    if (which == 2) s.Messages[Id(11)] = null!;
                    if (which == 3) s.Filters[Id(21)] = null!;
                }
                if (change == 1) row.LogicalName = "other";
                if (change == 2) row.Id = Guid.Empty;
                if (change == 3) row.KeyAttributes.Add("name", "unresolved");
                Denied(s);
            }
            var wrongId = new Store(); wrongId.Handler.Id = Id(99); Denied(wrongId);
        }
        [Fact]
        public void CanonicalReferencesMustBeCompleteAndCannotUseUnresolvedKeys()
        {
            foreach (string field in new[] { "sdkmessageid", "sdkmessagefilterid" })
            {
                var original = (EntityReference)new Store().Steps.Entities[0][field];
                object?[] invalid = { null, "x", new EntityReference("other", original.Id), new EntityReference(original.LogicalName, Guid.Empty),
                    new EntityReference(original.LogicalName, original.Id) { KeyAttributes = { ["name"] = "alias" } } };
                foreach (object? value in invalid) { var s = new Store(); s.Steps.Entities[0][field] = value!; Denied(s); }
                var missing = new Store(); missing.Steps.Entities[0].Attributes.Remove(field); Denied(missing);
            }
        }
        [Fact]
        public void SameNumberOfRowsWithAMissingBindingDoesNotPass()
        {
            var s = new Store(); s.Filters[Id(24)]["primaryobjecttypecode"] = "dft_credentialingdecision"; Denied(s);
            var broad = new Store(); broad.Filters[Id(21)]["primaryobjecttypecode"] = "dft_credentialingrecord"; Denied(broad);
            var wrongMessage = new Store(); wrongMessage.Messages[Id(11)]["name"] = "Create"; Denied(wrongMessage);
            var wrongType = new Store(); wrongType.Handler["typename"] = "different"; Denied(wrongType);
        }
        [Theory]
        [InlineData("rank", 49)] [InlineData("stage", 40)] [InlineData("mode", 1)]
        [InlineData("supporteddeployment", 2)] [InlineData("statecode", 1)] [InlineData("statuscode", 2)]
        public void WrongRankStageDeploymentOrDisabledStateFails(string field, int value)
        {
            var s = new Store(); s.Steps.Entities[0][field] = field == "rank" ? (object)value : new OptionSetValue(value); Denied(s);
        }
        [Theory]
        [InlineData("canbebypassed")] [InlineData("asyncautodelete")] [InlineData("iscustomizable")]
        public void ChangedBooleanControlsFail(string field)
        {
            var s = new Store(); s.Steps.Entities[0][field] = field == "iscustomizable" ? (object)new BooleanManagedProperty(true) : true; Denied(s);
        }
        [Fact]
        public void EveryRequiredRuntimeFieldRejectsMissingNullOrCoercedValues()
        {
            string[] columns = { "eventhandler", "sdkmessageid", "sdkmessagefilterid", "rank", "stage", "mode", "supporteddeployment",
                "statecode", "statuscode", "canbebypassed", "asyncautodelete", "iscustomizable" };
            foreach (string column in columns)
            {
                foreach (object? value in new object?[] { null, "untrusted" }) { var s = new Store(); s.Steps.Entities[0][column] = value!; Denied(s); }
                var missing = new Store(); missing.Steps.Entities[0].Attributes.Remove(column); Denied(missing);
            }
        }
        [Fact]
        public void HandlerReferenceCannotChangeIdentityTypeOrUseAlternateKeys()
        {
            foreach (EntityReference reference in new[] { new EntityReference("plugintype", Id(99)), new EntityReference("serviceendpoint", Id(3)),
                new EntityReference("plugintype", Id(3)) { KeyAttributes = { ["name"] = "alias" } } })
            { var s = new Store(); s.Steps.Entities[0]["eventhandler"] = reference; Denied(s); }
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void ForbiddenSettingsAndUnexpectedImagesCannotBeSilentlyIgnored(bool images)
        {
            for (int change = 0; change < 3; change++)
            {
                var s = new Store(); var result = change == 0 ? null : new EntityCollection();
                if (change == 1) result!.MoreRecords = true;
                if (change == 2) result!.Entities.Add(new Entity("synthetic", Id(99)));
                if (images) s.Images = result!; else s.Settings = result!;
                Denied(s);
            }
        }
        [Theory]
        [InlineData("identity")] [InlineData("handler")] [InlineData("steps")]
        [InlineData("message")] [InlineData("filter")] [InlineData("settings")] [InlineData("images")]
        public void AnyNativeFailureReturnsAGenericTechnicalFailureWithoutRepair(string phase)
        {
            var s = new Store { FailAt = phase }; Denied(s); Assert.Equal(phase, s.Calls.Last());
        }
        [Fact]
        public void LabelsAndTransportDecorationsAreNotLeakedIntoTheReceipt()
        {
            var s = new Store(); s.Steps.Entities[0]["name"] = "private administrative label";
            s.Steps.Entities[0].FormattedValues.Add("statuscode", "translated");
            Assert.Equal(new[] { Id(31), Id(32), Id(33), Id(34) }, Run(s));
        }
    }
}
