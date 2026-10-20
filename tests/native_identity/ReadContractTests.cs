using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class ReadContractTests
    {
        [Fact]
        public void ReadFieldsMatchTheActualNativeOperationalSchemaNotTheEvidenceTable()
        {
            var host = new ReadHost(); host.Run(); Assert.NotNull(host.ReadColumns);
            var fields = RegisterSchemaPlan.Build().OfType<CreateAttributeRequest>()
                .Where(r => r.EntityName == RegisterSchemaPlan.RecordTable).ToDictionary(r => r.Attribute.SchemaName);
            Assert.Equal(RegisterSchemaPlan.RecordTable, ReadCredentialingRecord.Table);
            Assert.Equal(fields.Keys.OrderBy(x => x, StringComparer.Ordinal), host.ReadColumns.Columns.OrderBy(x => x, StringComparer.Ordinal));
            var result = Assert.IsType<Entity>(host.Identity.Context.OutputParameters["Record"]);
            foreach (string name in host.ReadColumns.Columns)
            {
                AttributeMetadata field = fields[name].Attribute;
                Assert.False(field.IsSecured);
                Type type = field switch
                {
                    StringAttributeMetadata _ => typeof(string), MemoAttributeMetadata _ => typeof(string),
                    DateTimeAttributeMetadata _ => typeof(DateTime), IntegerAttributeMetadata _ => typeof(int),
                    PicklistAttributeMetadata _ => typeof(OptionSetValue),
                    _ => throw new InvalidOperationException("Review newly introduced schema field type.")
                };
                Assert.Equal(type, result[name].GetType());
            }
        }

        [Fact]
        public void NativeApiContractMatchesPluginAndResponseTypesWithoutClaimingDeployment()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "register-read.contract.json")));
            JsonElement root = document.RootElement;
            Assert.False(root.GetProperty("nativeRegistrationVerified").GetBoolean());
            Assert.False(root.GetProperty("productionEnabled").GetBoolean());
            Assert.Equal(typeof(ReadCredentialingRecord).FullName, root.GetProperty("pluginType").GetString());
            JsonElement api = root.GetProperty("customApi");
            Assert.Equal(ReadCredentialingRecord.ApiName, api.GetProperty("uniqueName").GetString());
            Assert.Equal(ReadCredentialingRecord.Table, api.GetProperty("boundEntityLogicalName").GetString());
            Assert.Equal(1, api.GetProperty("bindingType").GetInt32());
            Assert.Equal(0, api.GetProperty("allowedCustomProcessingStepType").GetInt32());
            Assert.False(api.GetProperty("isFunction").GetBoolean());
            Assert.False(api.GetProperty("workflowSdkStepEnabled").GetBoolean());
            Assert.False(api.GetProperty("isCustomizableInManagedSolution").GetBoolean());
            Assert.True(root.GetProperty("requireResolvedExecutePrivilege").GetBoolean());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("resolvedExecutePrivilegeName").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("pluginTypeId").ValueKind);
            Assert.Empty(root.GetProperty("additionalRequestParameters").EnumerateArray());
            var types = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["ReadSucceeded"] = 0, ["Record"] = 3, ["UnavailableFields"] = 11,
                ["ReadDisposition"] = 10, ["RetrievedAtUtc"] = 1
            };
            var host = new ReadHost(); host.Run();
            JsonElement[] responses = root.GetProperty("responseProperties").EnumerateArray().ToArray();
            Assert.Equal(types.Count, responses.Length);
            Assert.Equal(types.Keys.OrderBy(x => x, StringComparer.Ordinal),
                responses.Select(x => x.GetProperty("uniqueName").GetString() ?? throw new InvalidOperationException("Response name required."))
                    .OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(types.Keys.OrderBy(x => x, StringComparer.Ordinal),
                host.Identity.Context.OutputParameters.Keys.OrderBy(x => x, StringComparer.Ordinal));
            foreach (JsonElement response in responses)
            {
                string name = response.GetProperty("uniqueName").GetString() ?? throw new InvalidOperationException("Response name required.");
                Assert.Equal(types[name], response.GetProperty("type").GetInt32());
                Assert.False(response.GetProperty("isCustomizableInManagedSolution").GetBoolean());
            }
        }

        [Fact]
        public void EntirelyUnavailableProjectionIsNotPopulatedWithDefaultApprovalOrNoConditions()
        {
            var host = new ReadHost(); host.Record.Attributes.Clear(); host.Run();
            var output = host.Identity.Context.OutputParameters;
            var row = Assert.IsType<Entity>(output["Record"]);
            Assert.Empty(row.Attributes);
            Assert.Equal(16, Assert.IsType<string[]>(output["UnavailableFields"]).Length);
            Assert.Equal(ReadCredentialingRecord.Disposition, output["ReadDisposition"]);
            Assert.Equal(1, host.RecordReads);
            Assert.Equal(2, host.FactoryCalls);
        }
    }
}
