using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class CurrentScopeApiDefinitionTests
    {
        private static readonly Guid Api = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static Entity Plugin()
        {
            var row = new Entity("plugintype", Guid.Parse("20000000-0000-0000-0000-000000000002"));
            row["typename"] = typeof(ReadCurrentRecordedScope).FullName; return row;
        }
        private static Entity Privilege(string name = "prvRead_SyntheticRecord09")
        {
            var row = new Entity("privilege", Guid.Parse("30000000-0000-0000-0000-000000000003"));
            row["name"] = name; return row;
        }
        private static EntityCollection Responses(Entity api) => api.RelatedEntities[new Relationship(CurrentScopeApiDefinition.ResponseRelationship)];

        [Fact]
        public void DefinitionBindsTheSingleImplementedHandlerAndNeverLeavesPrivilegeBlank()
        {
            Entity plugin = Plugin(), privilege = Privilege();
            var request = CurrentScopeApiDefinition.Build(Api, plugin, privilege); Entity row = request.Target;
            Assert.Equal("Create", request.RequestName); Assert.Equal(2, request.Parameters.Count);
            Assert.Equal(RegisterSchemaPlan.Solution, request["SolutionUniqueName"]);
            Assert.Equal("customapi", row.LogicalName); Assert.Equal(Api, row.Id);
            Assert.Equal(ReadCurrentRecordedScope.ApiName, row["uniquename"]);
            Assert.Equal(ReadCurrentRecordedScope.ApiName, row["name"]);
            Assert.Equal(RegisterSchemaPlan.RecordTable, row["boundentitylogicalname"]);
            Assert.Equal(1, ((OptionSetValue)row["bindingtype"]).Value);
            Assert.Equal(0, ((OptionSetValue)row["allowedcustomprocessingsteptype"]).Value);
            Assert.False((bool)row["isfunction"]); Assert.True((bool)row["isprivate"]);
            Assert.False((bool)row["workflowsdkstepenabled"]); Assert.False(((BooleanManagedProperty)row["iscustomizable"]).Value);
            EntityReference reference = Assert.IsType<EntityReference>(row["plugintypeid"]);
            Assert.Equal("plugintype", reference.LogicalName); Assert.Equal(plugin.Id, reference.Id);
            Assert.Equal(privilege["name"], row["executeprivilegename"]);
            Assert.False(row.Contains("ownerid")); Assert.False(row.Contains("statecode"));
        }

        [Fact]
        public void SingleDeepCreateIncludesEveryTypedResponseAndNoExtraRequestInput()
        {
            Entity api = CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege()).Target;
            Assert.Single(api.RelatedEntities);
            EntityCollection rows = Responses(api); Assert.Equal(10, rows.Entities.Count);
            var actual = rows.Entities.ToDictionary(x => (string)x["uniquename"]);
            var expected = new Dictionary<string, int>
            {
                ["ReadSucceeded"] = 0, ["RootVersion"] = 10, ["AsOfUtc"] = 1, ["CurrentnessVerified"] = 0,
                ["RecordedStatus"] = 10, ["LatestSequence"] = 7, ["SelectedSequence"] = 7,
                ["HasEffectiveDecision"] = 0, ["SelectedDecision"] = 3, ["ReadDisposition"] = 10
            };
            foreach (var pair in expected)
            {
                Entity row = actual[pair.Key];
                Assert.Equal("customapiresponseproperty", row.LogicalName);
                Assert.Equal(pair.Value, ((OptionSetValue)row["type"]).Value);
                Assert.Equal(ReadCurrentRecordedScope.ApiName + "." + pair.Key, row["name"]);
                Assert.False(((BooleanManagedProperty)row["iscustomizable"]).Value);
                Assert.Empty(row.RelatedEntities); Assert.False(row.Contains("isoptional"));
                if (pair.Key == "SelectedDecision") Assert.Equal(RegisterSchemaPlan.DecisionTable, row["logicalentityname"]);
                else Assert.False(row.Contains("logicalentityname"));
            }
        }

        [Fact]
        public void MetadataAndPluginOutputsMatchTheTrackedContract()
        {
            var h = new CurrentScopeHost(); h.Run();
            var rows = Responses(CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege()).Target).Entities;
            string[] names = rows.Select(x => (string)x["uniquename"]).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(h.Identity.Context.OutputParameters.Keys.OrderBy(x => x, StringComparer.Ordinal), names);
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "current-scope-read.contract.json"))))
            {
                JsonElement root = doc.RootElement;
                Assert.False(root.GetProperty("registered").GetBoolean());
                Assert.Equal(ReadCurrentRecordedScope.ApiName, root.GetProperty("apiName").GetString());
                Assert.Equal(typeof(ReadCurrentRecordedScope).FullName, root.GetProperty("pluginType").GetString());
                Assert.Equal(root.GetProperty("responses").EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal), names);
                Assert.Empty(root.GetProperty("requestProperties").EnumerateArray());
            }
        }

        [Fact]
        public void MetadataDoesNotRetainMutableSourceRowsOrShareDefinitionsBetweenCalls()
        {
            Entity plugin = Plugin(), privilege = Privilege();
            Guid id = plugin.Id;
            Entity first = CurrentScopeApiDefinition.Build(Api, plugin, privilege).Target;
            plugin.Id = Guid.Empty; privilege["name"] = "changed";
            Assert.Equal(id, ((EntityReference)first["plugintypeid"]).Id);
            Assert.Equal("prvRead_SyntheticRecord09", first["executeprivilegename"]);
            Responses(first).Entities[0]["type"] = new OptionSetValue(10);
            ((EntityReference)first["plugintypeid"]).Id = Guid.Empty;
            Entity second = CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege()).Target;
            Assert.Equal(0, ((OptionSetValue)Responses(second).Entities[0]["type"]).Value);
            Assert.Equal(id, ((EntityReference)second["plugintypeid"]).Id);
            Assert.NotSame(Responses(first), Responses(second));
        }

        [Theory]
        [InlineData("plugin", 0)] [InlineData("plugin", 1)] [InlineData("plugin", 2)]
        [InlineData("privilege", 0)] [InlineData("privilege", 1)] [InlineData("privilege", 2)]
        public void WrongNativeRowIdentityIsNotARegistrationCandidate(string which, int change)
        {
            Entity plugin = Plugin(), privilege = Privilege(); Entity row = which == "plugin" ? plugin : privilege;
            if (change == 0) row.LogicalName = "other";
            if (change == 1) row.Id = Guid.Empty;
            if (change == 2) row.KeyAttributes.Add("alias", "untrusted");
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, plugin, privilege));
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void MissingOrDifferentHandlerIsNotSilentlyRegistered(int change)
        {
            Entity plugin = Plugin();
            if (change == 0) plugin.Attributes.Remove("typename");
            if (change == 1) plugin["typename"] = null!;
            if (change == 2) plugin["typename"] = 1;
            if (change == 3) plugin["typename"] = "Different.Plugin";
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, plugin, Privilege()));
        }

        [Theory]
        [InlineData("")] [InlineData("prv")] [InlineData("readRecord")] [InlineData(" prvRead")]
        [InlineData("prvRead ")] [InlineData("prvRead\n")] [InlineData("prvRead/All")] [InlineData("prvRead\u00e9")]
        public void EmptyOrMalformedPrivilegeIsNotMadeOptional(string value) =>
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege(value)));

        [Fact]
        public void PrivilegeLengthAndMissingTypeAreBoundedWithoutTrimming()
        {
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege("prv" + new string('A', 98))));
            Assert.Equal(100, ((string)CurrentScopeApiDefinition.Build(Api, Plugin(), Privilege("prv" + new string('Z', 97))).Target["executeprivilegename"]).Length);
            Entity row = Privilege(); row.Attributes.Remove("name");
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), row));
            row["name"] = null!; Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), row));
            row["name"] = 1; Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), row));
        }

        [Fact]
        public void MissingInputsCannotCreateAnUnboundDefinition()
        {
            Assert.Throws<ArgumentException>(() => CurrentScopeApiDefinition.Build(Guid.Empty, Plugin(), Privilege()));
            Assert.Throws<ArgumentNullException>(() => CurrentScopeApiDefinition.Build(Api, null!, Privilege()));
            Assert.Throws<ArgumentNullException>(() => CurrentScopeApiDefinition.Build(Api, Plugin(), null!));
        }
    }
}
