using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class RegisterIdentityStepTests
    {
        private static readonly Guid Step = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid Before = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid After = Guid.Parse("30000000-0000-0000-0000-000000000003");
        private static readonly Guid Handler = Guid.Parse("40000000-0000-0000-0000-000000000004");
        private static readonly Guid Message = Guid.Parse("50000000-0000-0000-0000-000000000005");
        private static readonly Guid Filter = Guid.Parse("60000000-0000-0000-0000-000000000006");
        private sealed class Fixture
        {
            internal readonly Entity Plugin = new Entity("plugintype", Handler)
                { ["typename"] = "CareOps.Decisions.RegisterSchema.ProtectRegisterIdentity" };
            internal readonly Entity MessageRow = new Entity("sdkmessage", Message) { ["name"] = "Update" };
            internal readonly Entity FilterRow = new Entity("sdkmessagefilter", Filter)
            {
                ["primaryobjecttypecode"] = "dft_credentialingrecord", ["sdkmessageid"] = new EntityReference("sdkmessage", Message),
                ["iscustomprocessingstepallowed"] = true, ["availability"] = 0
            };
            internal CreateRequest Build() => RegisterIdentityStepDefinition.Build(Step, Before, After, 50, Plugin, MessageRow, FilterRow);
        }
        private static EntityCollection Images(CreateRequest request) => request.Target.RelatedEntities[new Relationship(RegisterIdentityStepDefinition.ImageRelationship)];

        [Fact]
        public void ExplicitDisabledSynchronousServerStepHasNoPrivilegeOrFilteringFallback()
        {
            CreateRequest request = new Fixture().Build(); Entity step = request.Target;
            Assert.Equal("Create", request.RequestName); Assert.Equal("decisionsworkspace", request["SolutionUniqueName"]);
            Assert.Equal("sdkmessageprocessingstep", step.LogicalName); Assert.Equal(Step, step.Id);
            Assert.Equal(new EntityReference("plugintype", Handler), step["eventhandler"]);
            Assert.Equal(new EntityReference("sdkmessage", Message), step["sdkmessageid"]);
            Assert.Equal(new EntityReference("sdkmessagefilter", Filter), step["sdkmessagefilterid"]);
            Assert.Equal(50, step["rank"]); Assert.Equal(40, ((OptionSetValue)step["stage"]).Value);
            Assert.Equal(0, ((OptionSetValue)step["mode"]).Value); Assert.Equal(0, ((OptionSetValue)step["supporteddeployment"]).Value);
            Assert.Equal(1, ((OptionSetValue)step["statecode"]).Value); Assert.Equal(2, ((OptionSetValue)step["statuscode"]).Value);
            Assert.Equal(false, step["canbebypassed"]); Assert.Equal(false, step["asyncautodelete"]);
            Assert.False(((BooleanManagedProperty)step["iscustomizable"]).Value);
            Assert.DoesNotContain("impersonatinguserid", step.Attributes.Keys);
            Assert.DoesNotContain("filteringattributes", step.Attributes.Keys);
            Assert.DoesNotContain("sdkmessageprocessingstepsecureconfigid", step.Attributes.Keys);
            Assert.DoesNotContain("configuration", step.Attributes.Keys);
            Assert.Equal(14, step.Attributes.Count); Assert.Single(step.RelatedEntities);
        }
        [Fact]
        public void BothMinimalImageDefinitionsAreBoundToTheActualHandlerAliases()
        {
            var images = Images(new Fixture().Build()).Entities; Assert.Equal(2, images.Count);
            Assert.Equal(new[] { Before, After }, images.Select(x => x.Id));
            Assert.Equal(new[] { ProtectRegisterIdentity.BeforeAlias, ProtectRegisterIdentity.AfterAlias }, images.Select(x => x["entityalias"]));
            Assert.Equal(new[] { 0, 1 }, images.Select(x => ((OptionSetValue)x["imagetype"]).Value));
            Assert.All(images, image =>
            {
                Assert.Equal("sdkmessageprocessingstepimage", image.LogicalName);
                Assert.Equal(image["name"], image["entityalias"]); Assert.Equal("Target", image["messagepropertyname"]);
                Assert.Equal(ProtectRegisterIdentity.ImageColumns, ((string)image["attributes"]).Split(','));
                Assert.False(((BooleanManagedProperty)image["iscustomizable"]).Value);
                Assert.Equal(6, image.Attributes.Count); Assert.Empty(image.RelatedEntities);
            });
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void MissingStableIdsDuplicateImagesOrUnspecifiedOrderCannotProduceMetadata(int invalid)
        {
            var f = new Fixture();
            Assert.Throws<ArgumentException>(() => RegisterIdentityStepDefinition.Build(
                invalid == 0 ? Guid.Empty : Step, invalid == 1 ? Guid.Empty : Before,
                invalid == 2 ? Guid.Empty : invalid == 3 ? Before : After,
                invalid == 4 ? 0 : invalid == 5 ? -1 : 50, f.Plugin, f.MessageRow, f.FilterRow));
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void EveryMetadataRowRequiresCanonicalIdentityAndCorrectTable(int which)
        {
            for (int invalid = 0; invalid < 4; invalid++)
            {
                var f = new Fixture(); Entity[] rows = { f.Plugin, f.MessageRow, f.FilterRow };
                if (invalid == 0) rows[which] = null!;
                if (invalid == 1) rows[which].LogicalName = "wrong";
                if (invalid == 2) rows[which].Id = Guid.Empty;
                if (invalid == 3) rows[which].KeyAttributes.Add("name", "unresolved-alias");
                Assert.ThrowsAny<ArgumentException>(() => RegisterIdentityStepDefinition.Build(Step, Before, After, 50, rows[0], rows[1], rows[2]));
            }
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void ExactHandlerMessageAndTableNamesAreRequired(int which)
        {
            object?[] invalid = { null, 1, "", "wrong", "UPDATE" };
            foreach (object? value in invalid)
            {
                var f = new Fixture(); Entity[] rows = { f.Plugin, f.MessageRow, f.FilterRow }; string[] fields = { "typename", "name", "primaryobjecttypecode" };
                rows[which][fields[which]] = value!; Assert.Throws<ArgumentException>(() => f.Build());
                rows[which].Attributes.Remove(fields[which]); Assert.Throws<ArgumentException>(() => f.Build());
            }
        }
        [Fact]
        public void FilterMustReferenceTheSameCanonicalUpdateMessage()
        {
            object?[] invalid = { null, "wrong", new EntityReference("other", Message), new EntityReference("sdkmessage", Guid.Empty),
                new EntityReference("sdkmessage", After), new EntityReference("sdkmessage", Message) { KeyAttributes = { ["name"] = "Update" } } };
            foreach (object? value in invalid)
            {
                var f = new Fixture(); f.FilterRow["sdkmessageid"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.FilterRow.Attributes.Remove("sdkmessageid"); Assert.Throws<ArgumentException>(() => missing.Build());
        }
        [Fact]
        public void NativeFilterPermissionCannotBeAssumedOrCoerced()
        {
            foreach (object? value in new object?[] { null, false, 1, "true" })
            {
                var f = new Fixture(); f.FilterRow["iscustomprocessingstepallowed"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.FilterRow.Attributes.Remove("iscustomprocessingstepallowed"); Assert.Throws<ArgumentException>(() => missing.Build());
        }
        [Fact]
        public void OnlyServerCapableFiltersAreAccepted()
        {
            foreach (object? value in new object?[] { null, "0", 1, -1, 3 })
            {
                var f = new Fixture(); f.FilterRow["availability"] = value!; Assert.Throws<ArgumentException>(() => f.Build());
            }
            var missing = new Fixture(); missing.FilterRow.Attributes.Remove("availability"); Assert.Throws<ArgumentException>(() => missing.Build());
            var both = new Fixture(); both.FilterRow["availability"] = 2;
            Assert.Equal(0, ((OptionSetValue)both.Build().Target["supporteddeployment"]).Value);
        }
        [Fact]
        public void SecondaryEntityFiltersCannotBroadenTheGuard()
        {
            foreach (object value in new object[] { "account", 0, false })
            {
                var f = new Fixture(); f.FilterRow["secondaryobjecttypecode"] = value; Assert.Throws<ArgumentException>(() => f.Build());
            }
            foreach (object? value in new object?[] { null, "" })
            {
                var f = new Fixture(); f.FilterRow["secondaryobjecttypecode"] = value!; Assert.Equal(Step, f.Build().Target.Id);
            }
        }
        [Fact]
        public void MetadataIsFreshAndCannotMutateInputsOrLaterPlans()
        {
            var f = new Fixture(); CreateRequest first = f.Build();
            ((EntityReference)first.Target["eventhandler"]).Id = Step;
            Images(first).Entities[0]["attributes"] = "*";
            Assert.Equal(Handler, f.Plugin.Id);
            CreateRequest next = f.Build(); Assert.Equal(Handler, ((EntityReference)next.Target["eventhandler"]).Id);
            Assert.Equal(string.Join(",", ProtectRegisterIdentity.ImageColumns), Images(next).Entities[0]["attributes"]);
            f.Plugin.Id = After; Assert.Equal(Handler, ((EntityReference)next.Target["eventhandler"]).Id);
        }
        [Fact]
        public void DeepCreateRoundTripsThroughActualSdkContractsWithoutExecutingIt()
        {
            var request = new Fixture().Build(); var serializer = new DataContractSerializer(typeof(CreateRequest));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, request); stream.Position = 0;
                var restored = Assert.IsType<CreateRequest>(serializer.ReadObject(stream));
                Assert.Equal(Step, restored.Target.Id); Assert.Equal(2, Images(restored).Entities.Count);
                Assert.Equal(1, ((OptionSetValue)restored.Target["statecode"]).Value);
                Assert.Equal(ProtectRegisterIdentity.AfterAlias, Images(restored).Entities[1]["entityalias"]);
            }
        }
    }
}
