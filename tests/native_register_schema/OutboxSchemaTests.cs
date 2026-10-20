using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class OutboxSchemaTests
    {
        [Fact]
        public void MetadataIsModuleOwnedAndDoesNotProvisionOrSendAnything()
        {
            var plan = DecisionEventSchema.Build(); Assert.Equal(10, plan.Count);
            CreateEntityRequest table = Assert.IsType<CreateEntityRequest>(plan[0]);
            Assert.Equal("dft_credentialingevent", table.Entity.SchemaName);
            Assert.Equal(OwnershipTypes.UserOwned, table.Entity.OwnershipType); Assert.True(table.Entity.IsAuditEnabled.Value);
            Assert.False(table.Entity.IsActivity); Assert.False(table.HasNotes); Assert.False(table.HasActivities); Assert.False(table.HasFeedback);
            Assert.Equal("dft_name", table.PrimaryAttribute.SchemaName); Assert.Equal(36, table.PrimaryAttribute.MaxLength);
            Assert.All(plan, r => Assert.Equal(RegisterSchemaPlan.Solution, r.Parameters["SolutionUniqueName"]));
            Assert.Equal(6, plan.OfType<CreateAttributeRequest>().Count());
            Assert.Equal(2, plan.OfType<CreateOneToManyRequest>().Count()); Assert.Single(plan.OfType<CreateEntityKeyRequest>());
            Assert.Throws<NotSupportedException>(() => ((IList<OrganizationRequest>)plan).Clear());
        }

        [Fact]
        public void PayloadIsBoundedAndContainsNoClinicalNarrativesOrRecipientLists()
        {
            var fields = DecisionEventSchema.Build().OfType<CreateAttributeRequest>().ToDictionary(x => x.Attribute.SchemaName, x => x.Attribute);
            Assert.Equal(new[] { "dft_eventkey", "dft_eventtype", "dft_eventversion", "dft_organizationref", "dft_recordedat", "dft_sequence" }, fields.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(32, ((StringAttributeMetadata)fields["dft_eventkey"]).MaxLength);
            Assert.Equal(32, ((StringAttributeMetadata)fields["dft_organizationref"]).MaxLength);
            Assert.Equal(80, ((StringAttributeMetadata)fields["dft_eventtype"]).MaxLength);
            var version = Assert.IsType<IntegerAttributeMetadata>(fields["dft_eventversion"]);
            Assert.Equal(1, version.MinValue); Assert.Equal(1, version.MaxValue);
            var sequence = Assert.IsType<IntegerAttributeMetadata>(fields["dft_sequence"]);
            Assert.Equal(1, sequence.MinValue); Assert.Equal(int.MaxValue, sequence.MaxValue);
            var stamp = Assert.IsType<DateTimeAttributeMetadata>(fields["dft_recordedat"]);
            Assert.Equal(Microsoft.Xrm.Sdk.Metadata.DateTimeFormat.DateAndTime, stamp.Format);
            Assert.Equal(DateTimeBehavior.UserLocal.Value, stamp.DateTimeBehavior.Value);
            Assert.All(fields.Values, f => { Assert.True(f.IsAuditEnabled.Value); Assert.Equal(AttributeRequiredLevel.ApplicationRequired, f.RequiredLevel.Value); });
        }

        [Fact]
        public void JournalLinksCannotCascadeDeleteOrShareEventHistory()
        {
            var links = DecisionEventSchema.Build().OfType<CreateOneToManyRequest>().ToArray();
            Assert.Equal(new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable }, links.Select(x => x.OneToManyRelationship.ReferencedEntity));
            Assert.Equal(new[] { "dft_record", "dft_decision" }, links.Select(x => x.Lookup.SchemaName));
            Assert.All(links, link =>
            {
                var relation = link.OneToManyRelationship;
                Assert.Equal(DecisionEventSchema.Table, relation.ReferencingEntity);
                Assert.Equal(CascadeType.Restrict, relation.CascadeConfiguration.Delete);
                Assert.Equal(CascadeType.NoCascade, relation.CascadeConfiguration.Assign);
                Assert.Equal(CascadeType.NoCascade, relation.CascadeConfiguration.Merge);
                Assert.Equal(CascadeType.NoCascade, relation.CascadeConfiguration.Reparent);
                Assert.Equal(CascadeType.NoCascade, relation.CascadeConfiguration.Share);
                Assert.Equal(CascadeType.NoCascade, relation.CascadeConfiguration.Unshare);
                Assert.True(link.Lookup.IsAuditEnabled.Value);
                Assert.Equal(AttributeRequiredLevel.ApplicationRequired, link.Lookup.RequiredLevel.Value);
            });
            var key = Assert.Single(DecisionEventSchema.Build().OfType<CreateEntityKeyRequest>());
            Assert.Equal(DecisionEventSchema.Table, key.EntityName); Assert.Equal(new[] { "dft_eventkey" }, key.EntityKey.KeyAttributes);
        }

        [Fact]
        public void FreshMetadataAndPublishedInspectionDoNotAssertAnInstalledSchema()
        {
            var first = DecisionEventSchema.Build(); ((CreateEntityRequest)first[0]).Entity.SchemaName = "changed";
            Assert.Equal(DecisionEventSchema.Table, ((CreateEntityRequest)DecisionEventSchema.Build()[0]).Entity.SchemaName);
            var read = DecisionEventSchema.InspectionRequest(); Assert.Equal(DecisionEventSchema.Table, read.LogicalName);
            Assert.Equal(EntityFilters.All, read.EntityFilters); Assert.False(read.RetrieveAsIfPublished);
        }

        [Fact]
        public void MutatingSdkKeyColumnsCannotAlterSubsequentPlans()
        {
            var first = Assert.Single(DecisionEventSchema.Build().OfType<CreateEntityKeyRequest>());
            first.EntityKey.KeyAttributes[0] = "changed";
            var second = Assert.Single(DecisionEventSchema.Build().OfType<CreateEntityKeyRequest>());
            Assert.Equal(new[] { "dft_eventkey" }, second.EntityKey.KeyAttributes);
            Assert.NotSame(first.EntityKey.KeyAttributes, second.EntityKey.KeyAttributes);
        }

        [Fact]
        public void EnvelopeMetadataRoundTripsThroughTheActualSdkContracts()
        {
            Type[] known = { typeof(EntityMetadata), typeof(StringAttributeMetadata), typeof(IntegerAttributeMetadata),
                typeof(DateTimeAttributeMetadata), typeof(LookupAttributeMetadata), typeof(OneToManyRelationshipMetadata), typeof(EntityKeyMetadata) };
            foreach (OrganizationRequest request in DecisionEventSchema.Build())
            {
                var serializer = new DataContractSerializer(request.GetType(), known);
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, request); stream.Position = 0;
                    var roundTrip = Assert.IsAssignableFrom<OrganizationRequest>(serializer.ReadObject(stream));
                    Assert.Equal(request.RequestName, roundTrip.RequestName); Assert.Equal(request.Parameters.Count, roundTrip.Parameters.Count);
                }
            }
        }
    }
}
