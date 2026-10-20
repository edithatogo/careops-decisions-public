using System;
using System.IO;
using System.Runtime.Serialization;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class HistoryMutationTests
    {
        private static readonly Guid Step = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid Handler = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid Message = Guid.Parse("30000000-0000-0000-0000-000000000003");
        private static readonly Guid Filter = Guid.Parse("40000000-0000-0000-0000-000000000004");
        private sealed class ForbiddenServices : IServiceProvider
        {
            public object GetService(Type serviceType) => throw new InvalidOperationException("Private data must not be read.");
        }
        private sealed class Fixture
        {
            internal readonly Entity Type = new Entity("plugintype", Handler)
                { ["typename"] = "CareOps.Decisions.RegisterSchema.RejectHistoryMutation" };
            internal readonly Entity Verb = new Entity("sdkmessage", Message) { ["name"] = "Update" };
            internal readonly Entity Table = new Entity("sdkmessagefilter", Filter)
            {
                ["primaryobjecttypecode"] = "dft_credentialingdecision", ["sdkmessageid"] = new EntityReference("sdkmessage", Message),
                ["iscustomprocessingstepallowed"] = true, ["availability"] = 0
            };
            internal CreateRequest Build() => HistoryMutationStepDefinition.Build(Step, 50, Type, Verb, Table);
        }

        [Fact]
        public void TerminalGuardNeverConsultsCallerExemptionsOrReadsPrivateHistory()
        {
            var guard = new RejectHistoryMutation();
            foreach (IServiceProvider? service in new IServiceProvider?[] { null, new ForbiddenServices() })
            {
                var error = Assert.Throws<InvalidPluginExecutionException>(() => guard.Execute(service!));
                Assert.Equal("Recorded credentialing history cannot be edited or deleted through this operation. Use the controlled correction or records-management process.", error.Message);
                Assert.Null(error.InnerException);
            }
        }

        [Theory]
        [InlineData("dft_credentialingdecision", "Update")]
        [InlineData("dft_credentialingdecision", "Delete")]
        [InlineData("dft_credentialingevent", "Update")]
        [InlineData("dft_credentialingevent", "Delete")]
        public void OnlyTheFourFinalHistoryBindingsHaveEarlyDisabledSynchronousDefinitions(string table, string operation)
        {
            var f = new Fixture(); f.Table["primaryobjecttypecode"] = table; f.Verb["name"] = operation;
            CreateRequest request = f.Build(); Entity step = request.Target;
            Assert.Equal("Create", request.RequestName); Assert.Equal("decisionsworkspace", request["SolutionUniqueName"]);
            Assert.Equal("sdkmessageprocessingstep", step.LogicalName); Assert.Equal(Step, step.Id);
            Assert.Equal("CareOps preserve " + table + " on " + operation, step["name"]);
            Assert.NotEmpty((string)step["description"]);
            Assert.Equal(new EntityReference("plugintype", Handler), step["eventhandler"]);
            Assert.Equal(new EntityReference("sdkmessage", Message), step["sdkmessageid"]);
            Assert.Equal(new EntityReference("sdkmessagefilter", Filter), step["sdkmessagefilterid"]);
            Assert.Equal(50, step["rank"]); Assert.Equal(10, ((OptionSetValue)step["stage"]).Value);
            Assert.Equal(0, ((OptionSetValue)step["mode"]).Value); Assert.Equal(0, ((OptionSetValue)step["supporteddeployment"]).Value);
            Assert.Equal(1, ((OptionSetValue)step["statecode"]).Value); Assert.Equal(2, ((OptionSetValue)step["statuscode"]).Value);
            Assert.Equal(false, step["canbebypassed"]); Assert.Equal(false, step["asyncautodelete"]);
            Assert.False(((BooleanManagedProperty)step["iscustomizable"]).Value);
            Assert.Equal(14, step.Attributes.Count); Assert.Empty(step.RelatedEntities);
            foreach (string name in new[] { "filteringattributes", "impersonatinguserid", "configuration", "sdkmessageprocessingstepsecureconfigid" })
                Assert.DoesNotContain(name, step.Attributes.Keys);
        }

        [Fact]
        public void ConstructionDoesNotAuthorizeCreateOrChangeTheOperationalRegister()
        {
            foreach (object? value in new object?[] { null, 1, "", "Create", "Upsert", "UpdateMultiple", "update", " Delete" })
            {
                var f = new Fixture(); f.Verb["name"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.Verb.Attributes.Remove("name"); Assert.Throws<ArgumentException>(() => missing.Build());
            foreach (object? value in new object?[] { null, 1, "", "dft_credentialingrecord", "account", "DFT_CREDENTIALINGEVENT" })
            {
                var f = new Fixture(); f.Table["primaryobjecttypecode"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            missing.Table.Attributes.Remove("primaryobjecttypecode"); missing.Verb["name"] = "Update";
            Assert.Throws<ArgumentException>(() => missing.Build());
        }

        [Theory]
        [InlineData(0)] [InlineData(-1)]
        public void OrderMustBeExplicitAndPositive(int rank)
        {
            var f = new Fixture(); Assert.Throws<ArgumentException>(() => HistoryMutationStepDefinition.Build(Step, rank, f.Type, f.Verb, f.Table));
        }
        [Fact]
        public void StepIdCannotBeInventedByTheFactory()
        {
            var f = new Fixture(); Assert.Throws<ArgumentException>(() => HistoryMutationStepDefinition.Build(Guid.Empty, 50, f.Type, f.Verb, f.Table));
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void EveryMetadataRowRequiresCanonicalIdentityAndExactType(int which)
        {
            for (int change = 0; change < 4; change++)
            {
                var f = new Fixture(); Entity[] rows = { f.Type, f.Verb, f.Table };
                if (change == 0) rows[which] = null!;
                if (change == 1) rows[which].LogicalName = "other";
                if (change == 2) rows[which].Id = Guid.Empty;
                if (change == 3) rows[which].KeyAttributes.Add("name", "unresolved");
                Assert.ThrowsAny<ArgumentException>(() => HistoryMutationStepDefinition.Build(Step, 50, rows[0], rows[1], rows[2]));
            }
        }
        [Fact]
        public void AnotherPluginCannotBeSubstitutedForTheTerminalGuard()
        {
            foreach (object? value in new object?[] { null, 1, "", "CareOps.Decisions.RegisterSchema.ProtectRegisterIdentity" })
            {
                var f = new Fixture(); f.Type["typename"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.Type.Attributes.Remove("typename"); Assert.Throws<ArgumentException>(() => missing.Build());
        }
        [Fact]
        public void TheFilterMustBelongToTheExactMessage()
        {
            object?[] invalid = { null, "x", new EntityReference("other", Message), new EntityReference("sdkmessage", Guid.Empty),
                new EntityReference("sdkmessage", Step), new EntityReference("sdkmessage", Message) { KeyAttributes = { ["name"] = "Update" } } };
            foreach (object? value in invalid)
            {
                var f = new Fixture(); f.Table["sdkmessageid"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.Table.Attributes.Remove("sdkmessageid"); Assert.Throws<ArgumentException>(() => missing.Build());
        }
        [Fact]
        public void MissingFalseOrCoercedProcessingPermissionIsNotAccepted()
        {
            foreach (object? value in new object?[] { null, false, 1, "true" })
            {
                var f = new Fixture(); f.Table["iscustomprocessingstepallowed"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.Table.Attributes.Remove("iscustomprocessingstepallowed"); Assert.Throws<ArgumentException>(() => missing.Build());
        }
        [Fact]
        public void ServerAvailabilityIsRequiredButBothModeStillBuildsServerOnly()
        {
            foreach (object? value in new object?[] { null, "0", 1, -1, 3 })
            {
                var f = new Fixture(); f.Table["availability"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.Table.Attributes.Remove("availability"); Assert.Throws<ArgumentException>(() => missing.Build());
            var both = new Fixture(); both.Table["availability"] = 2; Assert.Equal(0, ((OptionSetValue)both.Build().Target["supporteddeployment"]).Value);
        }
        [Fact]
        public void SecondaryTableBindingIsNeverAnImplicitWildcard()
        {
            foreach (object value in new object[] { "account", 0, false })
            {
                var f = new Fixture(); f.Table["secondaryobjecttypecode"] = value; Assert.Throws<ArgumentException>(() => f.Build());
            }
            foreach (object? value in new object?[] { null, "" })
            {
                var f = new Fixture(); f.Table["secondaryobjecttypecode"] = value!; Assert.Equal(Step, f.Build().Target.Id);
            }
        }
        [Fact]
        public void InputAndReturnedMetadataAreNotSharedBetweenPlans()
        {
            var f = new Fixture(); Entity first = f.Build().Target;
            ((EntityReference)first["eventhandler"]).Id = Step; ((OptionSetValue)first["statecode"]).Value = 0;
            Entity next = f.Build().Target; Assert.Equal(Handler, ((EntityReference)next["eventhandler"]).Id);
            Assert.Equal(1, ((OptionSetValue)next["statecode"]).Value); Assert.Equal(Handler, f.Type.Id);
            f.Type.Id = Message; Assert.Equal(Handler, ((EntityReference)next["eventhandler"]).Id);
        }
        [Fact]
        public void NativeSdkRoundtripPreservesDisabledStateAndNoImages()
        {
            CreateRequest request = new Fixture().Build(); var serializer = new DataContractSerializer(typeof(CreateRequest));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, request); stream.Position = 0;
                var restored = Assert.IsType<CreateRequest>(serializer.ReadObject(stream));
                Assert.Equal(Step, restored.Target.Id); Assert.Equal(1, ((OptionSetValue)restored.Target["statecode"]).Value);
                Assert.Equal(10, ((OptionSetValue)restored.Target["stage"]).Value); Assert.Empty(restored.Target.RelatedEntities);
            }
        }
    }
}
