using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class ReceiptApiDefinitionTests
    {
        private static readonly Guid Api = Guid.Parse("70000000-0000-0000-0000-000000000007");
        private static readonly Guid Handler = Guid.Parse("80000000-0000-0000-0000-000000000008");
        private static readonly Guid Privilege = Guid.Parse("90000000-0000-0000-0000-000000000009");
        private static Entity Type() => new Entity("plugintype", Handler) { ["typename"] = "CareOps.Decisions.DataverseIdentity.ReadOwnDecisionReceipt" };
        private static Entity Right() => new Entity("privilege", Privilege) { ["name"] = "prvReaddft_credentialingdecision" };
        private static CreateRequest Build() => OwnDecisionReceiptApiDefinition.Build(Api, Type(), Right());
        private static EntityCollection Inputs(Entity api) => api.RelatedEntities[new Relationship(OwnDecisionReceiptApiDefinition.RequestRelationship)];
        private static EntityCollection Outputs(Entity api) => api.RelatedEntities[new Relationship(OwnDecisionReceiptApiDefinition.ResponseRelationship)];

        [Fact]
        public void ExactHandlerAndExistingPrivilegeAreRequiredOnTheBoundReadApi()
        {
            CreateRequest request = Build(); Entity api = request.Target;
            Assert.Equal("Create", request.RequestName); Assert.Equal("decisionsworkspace", request["SolutionUniqueName"]);
            Assert.Equal("customapi", api.LogicalName); Assert.Equal(Api, api.Id); Assert.Equal(13, api.Attributes.Count);
            Assert.Equal(ReadOwnDecisionReceipt.ApiName, api["uniquename"]); Assert.Equal(api["uniquename"], api["name"]);
            Assert.NotEmpty((string)api["displayname"]); Assert.NotEmpty((string)api["description"]);
            Assert.Equal(1, ((OptionSetValue)api["bindingtype"]).Value); Assert.Equal(RegisterSchemaPlan.RecordTable, api["boundentitylogicalname"]);
            Assert.Equal(0, ((OptionSetValue)api["allowedcustomprocessingsteptype"]).Value);
            Assert.Equal(false, api["isfunction"]); Assert.Equal(true, api["isprivate"]);
            Assert.Equal(false, api["workflowsdkstepenabled"]); Assert.False(((BooleanManagedProperty)api["iscustomizable"]).Value);
            Assert.Equal(new EntityReference("plugintype", Handler), api["plugintypeid"]);
            Assert.Equal("prvReaddft_credentialingdecision", api["executeprivilegename"]);
            Assert.Equal(2, api.RelatedEntities.Count);
            Assert.DoesNotContain("statecode", api.Attributes.Keys); // Custom API state is not an activation safeguard.
        }
        [Fact]
        public void OneRequiredGuidInputDoesNotDuplicateThePlatformTargetOrAcceptAuthorityClaims()
        {
            Entity input = Assert.Single(Inputs(Build().Target).Entities);
            Assert.Equal("customapirequestparameter", input.LogicalName); Assert.Equal("DecisionId", input["uniquename"]);
            Assert.Equal(12, ((OptionSetValue)input["type"]).Value); Assert.Equal(false, input["isoptional"]);
            Assert.Equal(7, input.Attributes.Count); Assert.False(((BooleanManagedProperty)input["iscustomizable"]).Value);
            Assert.DoesNotContain("logicalentityname", input.Attributes.Keys);
            Assert.Equal(ReadOwnDecisionReceipt.ApiName + ".DecisionId", input["name"]);
        }
        [Fact]
        public void EveryResponseIsPrimitiveAndKeepsHistoricalInterpretation()
        {
            var rows = Outputs(Build().Target).Entities;
            Assert.Equal(new[] { "RecordId", "DecisionId", "RecordedAtUtc", "Sequence", "RecordedOutcome", "CaseHash", "RetrievedAtUtc", "ReadDisposition", "ReadSucceeded" }, rows.Select(x => (string)x["uniquename"]));
            Assert.Equal(new[] { 12, 12, 1, 7, 7, 10, 1, 10, 0 }, rows.Select(x => ((OptionSetValue)x["type"]).Value));
            Assert.All(rows, row =>
            {
                Assert.Equal("customapiresponseproperty", row.LogicalName); Assert.Equal(6, row.Attributes.Count);
                Assert.Equal(ReadOwnDecisionReceipt.ApiName + "." + row["uniquename"], row["name"]);
                Assert.Equal(row["uniquename"], row["displayname"]); Assert.NotEmpty((string)row["description"]);
                Assert.False(((BooleanManagedProperty)row["iscustomizable"]).Value);
                Assert.DoesNotContain("logicalentityname", row.Attributes.Keys);
            });
        }
        [Fact]
        public void DefinitionMatchesTheActualPluginOutputsRatherThanASeparateSchemaExample()
        {
            var host = new DecisionReceiptTests.Host(); host.Run();
            ParameterCollection actual = host.Identity.Context.OutputParameters;
            var declared = Outputs(Build().Target).Entities.ToDictionary(row => (string)row["uniquename"], row => ((OptionSetValue)row["type"]).Value);
            Assert.Equal(actual.Keys.OrderBy(x => x, StringComparer.Ordinal), declared.Keys.OrderBy(x => x, StringComparer.Ordinal));
            var types = new Dictionary<Type, int> { [typeof(bool)] = 0, [typeof(DateTime)] = 1, [typeof(int)] = 7, [typeof(string)] = 10, [typeof(Guid)] = 12 };
            foreach (var pair in actual)
            {
                Assert.True(types.TryGetValue(pair.Value.GetType(), out int code));
                Assert.Equal(code, declared[pair.Key]);
            }
            Assert.Equal(ReadOwnDecisionReceipt.ApiName, host.Identity.Context.MessageName);
            Assert.Equal(new[] { "DecisionId", "Target" }, host.Identity.Context.InputParameters.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(ReadOwnDecisionReceipt.Disposition, actual["ReadDisposition"]);
        }
        [Fact]
        public void EmptyApiIdCannotCreateAnUntrackedDefinition()
        { Assert.Throws<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Guid.Empty, Type(), Right())); }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void BothInspectedRowsNeedExactTablesCanonicalIdsAndNoUnresolvedKeys(bool privilege)
        {
            for (int change = 0; change < 4; change++)
            {
                Entity type = Type(), right = Right(); Entity target = privilege ? right : type;
                if (change == 0) { if (privilege) right = null!; else type = null!; }
                if (change == 1) target.LogicalName = "other";
                if (change == 2) target.Id = Guid.Empty;
                if (change == 3) target.KeyAttributes.Add("name", "alias");
                Assert.ThrowsAny<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Api, type, right));
            }
        }
        [Fact]
        public void NoMissingOrSubstitutedPluginCanYieldDefaultSuccessValues()
        {
            foreach (object? value in new object?[] { null, 1, "", "CareOps.Decisions.DataverseIdentity.HumanConfirmedJournal", "CareOps.Decisions.DataverseIdentity.ReadCurrentRecordedScope" })
            {
                Entity type = Type(); type["typename"] = value!;
                Assert.Throws<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Api, type, Right()));
            }
            Entity missing = Type(); missing.Attributes.Remove("typename");
            Assert.Throws<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Api, missing, Right()));
        }
        [Fact]
        public void PrivilegeIsExplicitBoundedAndNotAnUnrestrictedFallback()
        {
            object?[] invalid = { null, 1, "", "prv", "read", "PRVRead", "prv Read", "prv\nRead", "prv/read", "prv" + (char)0, "prv" + new string('x', 98) };
            foreach (object? value in invalid)
            {
                Entity right = Right(); right["name"] = value!;
                Assert.Throws<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Api, Type(), right));
            }
            Entity missing = Right(); missing.Attributes.Remove("name");
            Assert.Throws<ArgumentException>(() => OwnDecisionReceiptApiDefinition.Build(Api, Type(), missing));
            Entity limit = Right(); limit["name"] = "prv" + new string('x', 97);
            Assert.Equal(limit["name"], OwnDecisionReceiptApiDefinition.Build(Api, Type(), limit).Target["executeprivilegename"]);
        }
        [Fact]
        public void NativeTransportObjectsCannotModifyInputsOrLaterDefinitions()
        {
            Entity type = Type(), right = Right();
            var first = OwnDecisionReceiptApiDefinition.Build(Api, type, right);
            ((EntityReference)first.Target["plugintypeid"]).Id = Api;
            Inputs(first.Target).Entities[0]["isoptional"] = true; Outputs(first.Target).Entities.Clear();
            var second = OwnDecisionReceiptApiDefinition.Build(Api, type, right);
            Assert.Equal(Handler, type.Id); Assert.Equal(Handler, ((EntityReference)second.Target["plugintypeid"]).Id);
            Assert.Equal(false, Inputs(second.Target).Entities[0]["isoptional"]); Assert.Equal(9, Outputs(second.Target).Entities.Count);
            type.Id = Api; right["name"] = "prvChanged";
            Assert.Equal(Handler, ((EntityReference)second.Target["plugintypeid"]).Id);
            Assert.Equal("prvReaddft_credentialingdecision", second.Target["executeprivilegename"]);
        }
        [Fact]
        public void OneDeepCreateRoundTripsThroughTheSdkWithoutExecutingARegistration()
        {
            CreateRequest request = Build(); var serializer = new DataContractSerializer(typeof(CreateRequest));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, request); stream.Position = 0;
                var restored = Assert.IsType<CreateRequest>(serializer.ReadObject(stream));
                Assert.Equal(Api, restored.Target.Id); Assert.Single(Inputs(restored.Target).Entities);
                Assert.Equal(9, Outputs(restored.Target).Entities.Count);
                Assert.Equal("DecisionId", Inputs(restored.Target).Entities[0]["uniquename"]);
                Assert.Equal(12, ((OptionSetValue)Inputs(restored.Target).Entities[0]["type"]).Value);
                Assert.Equal("prvReaddft_credentialingdecision", restored.Target["executeprivilegename"]);
            }
        }
    }
}
