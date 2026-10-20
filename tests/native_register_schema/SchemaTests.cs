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
    public sealed class SchemaTests
    {
        private static CreateAttributeRequest[] Columns() => RegisterSchemaPlan.Build().OfType<CreateAttributeRequest>().ToArray();

        [Fact]
        public void OrderedPlanContainsOnlyNativeMetadataRequests()
        {
            var plan = RegisterSchemaPlan.Build();
            Assert.Equal(42, plan.Count);
            Assert.Equal(2, plan.OfType<CreateEntityRequest>().Count());
            Assert.Equal(34, plan.OfType<CreateAttributeRequest>().Count());
            Assert.Equal(3, plan.OfType<CreateOneToManyRequest>().Count());
            Assert.Equal(3, plan.OfType<CreateEntityKeyRequest>().Count());
            Assert.All(plan.Take(2), request => Assert.IsType<CreateEntityRequest>(request));
            Assert.All(plan.Skip(2).Take(34), request => Assert.IsType<CreateAttributeRequest>(request));
            Assert.All(plan.Skip(36).Take(3), request => Assert.IsType<CreateOneToManyRequest>(request));
            Assert.All(plan.Skip(39), request => Assert.IsType<CreateEntityKeyRequest>(request));
            Assert.All(plan, request => Assert.Equal("decisionsworkspace", request.Parameters["SolutionUniqueName"]));
            Assert.Throws<NotSupportedException>(() => ((IList<OrganizationRequest>)plan).Clear());
        }

        [Fact]
        public void CoreTablesAreModuleOwnedAuditableAndDoNotCollectAttachments()
        {
            CreateEntityRequest[] tables = RegisterSchemaPlan.Build().OfType<CreateEntityRequest>().ToArray();
            Assert.Equal(new[] { "dft_credentialingrecord", "dft_credentialingdecision" }, tables.Select(x => x.Entity.SchemaName));
            Assert.All(tables, table =>
            {
                Assert.Equal(OwnershipTypes.UserOwned, table.Entity.OwnershipType);
                Assert.True(table.Entity.IsAuditEnabled.Value);
                Assert.False(table.Entity.IsActivity);
                Assert.False(table.HasNotes); Assert.False(table.HasActivities); Assert.False(table.HasFeedback);
                Assert.Equal("dft_name", table.PrimaryAttribute.SchemaName);
                Assert.Equal(100, table.PrimaryAttribute.MaxLength);
                Assert.Null(table.Entity.DataProviderId);
            });
        }

        [Fact]
        public void EveryColumnHasAuditingAndThereAreNoDuplicateFields()
        {
            var columns = Columns();
            Assert.Equal(columns.Length, columns.Select(x => x.EntityName + "/" + x.Attribute.SchemaName).Distinct(StringComparer.Ordinal).Count());
            Assert.All(columns, field =>
            {
                Assert.StartsWith("dft_", field.Attribute.SchemaName);
                Assert.True(field.Attribute.IsAuditEnabled.Value);
                Assert.NotEmpty(field.Attribute.DisplayName.LocalizedLabels);
                Assert.Contains(field.EntityName, new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable });
            });
        }

        [Fact]
        public void OperationalProjectionAndDecisionEvidenceAreSeparated()
        {
            var columns = Columns();
            string[] protectedNames = { "dft_actorref", "dft_profileversionref", "dft_profilehash", "dft_casehash", "dft_decisionevidenceref", "dft_authorityevidenceref" };
            Assert.Equal(protectedNames.OrderBy(x => x, StringComparer.Ordinal),
                columns.Where(x => x.Attribute.IsSecured == true).Select(x => x.Attribute.SchemaName).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(columns.Where(x => x.Attribute.IsSecured == true), column => Assert.Equal(RegisterSchemaPlan.DecisionTable, column.EntityName));
            foreach (string table in new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable })
                Assert.All(new[] { "dft_scopetext", "dft_conditionstext", "dft_supervisiontext", "dft_scopeversionref" },
                    name => Assert.Contains(columns, x => x.EntityName == table && x.Attribute.SchemaName == name));
            Assert.DoesNotContain(columns, x => x.Attribute.SchemaName.Contains("rag") || x.Attribute.SchemaName.Contains("cv"));
        }

        [Fact]
        public void DefaultStateIsNotApprovalAndOptionsAreStable()
        {
            var states = Columns().Select(x => x.Attribute).OfType<PicklistAttributeMetadata>().ToArray();
            Assert.Equal(2, states.Length);
            Assert.All(states, state =>
            {
                Assert.Equal(RegisterSchemaPlan.Unverified, state.DefaultFormValue);
                Assert.False(state.OptionSet.IsGlobal);
                Assert.Equal(new int?[] { 602170000, 602170001, 602170002, 602170003, 602170004 }, state.OptionSet.Options.Select(x => x.Value));
                Assert.Equal(new[] { "Unverified - not authority", "Granted", "Suspended", "Revoked", "Refused" },
                    state.OptionSet.Options.Select(x => x.Label.LocalizedLabels[0].Label));
            });
        }

        [Fact]
        public void CurrentnessIsNullableAndDistinctFromRecordedDecisionAndProjectionTime()
        {
            var columns = Columns();
            foreach (string name in new[] { "dft_sourcecheckedat", "dft_sourcecurrentuntil", "dft_projectedasof" })
            {
                var column = Assert.Single(columns, x => x.Attribute.SchemaName == name);
                Assert.Equal(RegisterSchemaPlan.RecordTable, column.EntityName);
                Assert.Equal(AttributeRequiredLevel.None, column.Attribute.RequiredLevel.Value);
            }
            Assert.Contains(columns, x => x.Attribute.SchemaName == "dft_effectiveat" && x.EntityName == RegisterSchemaPlan.DecisionTable);
            Assert.Contains(columns, x => x.Attribute.SchemaName == "dft_recordedat" && x.EntityName == RegisterSchemaPlan.DecisionTable);
            Assert.All(columns.Select(x => x.Attribute).OfType<DateTimeAttributeMetadata>(), instant =>
            {
                Assert.Equal(Microsoft.Xrm.Sdk.Metadata.DateTimeFormat.DateAndTime, instant.Format);
                Assert.Equal(DateTimeBehavior.UserLocal.Value, instant.DateTimeBehavior.Value);
            });
        }

        [Fact]
        public void KeysEnforceCanonicalIdentifiersAndPerRecordSequenceWithoutExternalLookupDependencies()
        {
            var keys = RegisterSchemaPlan.Build().OfType<CreateEntityKeyRequest>().ToArray();
            Assert.Equal(new[] { "dft_recordkey" }, keys[0].EntityKey.KeyAttributes);
            Assert.Equal(new[] { "dft_decisionkey" }, keys[1].EntityKey.KeyAttributes);
            Assert.Equal(new[] { "dft_record", "dft_sequence" }, keys[2].EntityKey.KeyAttributes);
            Assert.All(keys.Skip(1), key => Assert.Equal(RegisterSchemaPlan.DecisionTable, key.EntityName));
            Assert.Equal(RegisterSchemaPlan.RecordTable, keys[0].EntityName);
            Assert.All(Columns().Where(x => x.Attribute.SchemaName == "dft_recordkey" || x.Attribute.SchemaName == "dft_decisionkey"), column =>
            {
                Assert.Equal(64, Assert.IsType<StringAttributeMetadata>(column.Attribute).MaxLength);
                Assert.Equal(AttributeRequiredLevel.ApplicationRequired, column.Attribute.RequiredLevel.Value);
            });
        }

        [Fact]
        public void RelationshipsCannotSilentlyDeleteOrShareHistory()
        {
            var relationships = RegisterSchemaPlan.Build().OfType<CreateOneToManyRequest>().ToArray();
            Assert.Equal(new[] { "dft_record", "dft_supersedes", "dft_latestdecision" }, relationships.Select(x => x.Lookup.SchemaName));
            Assert.Equal(AttributeRequiredLevel.ApplicationRequired, relationships[0].Lookup.RequiredLevel.Value);
            Assert.All(relationships.Skip(1), r => Assert.Equal(AttributeRequiredLevel.None, r.Lookup.RequiredLevel.Value));
            Assert.All(relationships, relationship =>
            {
                var r = relationship.OneToManyRelationship;
                Assert.Contains(r.ReferencedEntity, new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable });
                Assert.Contains(r.ReferencingEntity, new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable });
                Assert.Equal(CascadeType.Restrict, r.CascadeConfiguration.Delete);
                Assert.Equal(CascadeType.NoCascade, r.CascadeConfiguration.Assign);
                Assert.Equal(CascadeType.NoCascade, r.CascadeConfiguration.Merge);
                Assert.Equal(CascadeType.NoCascade, r.CascadeConfiguration.Reparent);
                Assert.Equal(CascadeType.NoCascade, r.CascadeConfiguration.Share);
                Assert.Equal(CascadeType.NoCascade, r.CascadeConfiguration.Unshare);
            });
        }

        [Fact]
        public void NumericBoundsPreserveUnknownProjectionAndPositiveDecisionSequence()
        {
            var fields = Columns().Where(x => x.Attribute is IntegerAttributeMetadata).ToDictionary(x => x.Attribute.SchemaName);
            Assert.Equal(0, ((IntegerAttributeMetadata)fields["dft_latestsequence"].Attribute).MinValue);
            Assert.Equal(0, ((IntegerAttributeMetadata)fields["dft_projectionsequence"].Attribute).MinValue);
            Assert.Equal(1, ((IntegerAttributeMetadata)fields["dft_sequence"].Attribute).MinValue);
            Assert.All(fields.Values, x => Assert.Equal(int.MaxValue, ((IntegerAttributeMetadata)x.Attribute).MaxValue));
        }

        [Fact]
        public void BuildDoesNotShareMutableSdkObjectsBetweenCallers()
        {
            var first = RegisterSchemaPlan.Build();
            ((CreateEntityRequest)first[0]).Entity.SchemaName = "changed";
            ((CreateAttributeRequest)first[2]).Attribute.SchemaName = "changed";
            var second = RegisterSchemaPlan.Build();
            Assert.Equal(RegisterSchemaPlan.RecordTable, ((CreateEntityRequest)second[0]).Entity.SchemaName);
            Assert.Equal("dft_recordkey", ((CreateAttributeRequest)second[2]).Attribute.SchemaName);
        }

        [Fact]
        public void InspectionReadsPublishedNativeMetadataRatherThanAssumingConcurrencyOrKeyActivation()
        {
            var reads = RegisterSchemaPlan.InspectionRequests();
            Assert.Equal(new[] { RegisterSchemaPlan.RecordTable, RegisterSchemaPlan.DecisionTable }, reads.Select(x => x.LogicalName));
            Assert.All(reads, request => { Assert.Equal(EntityFilters.All, request.EntityFilters); Assert.False(request.RetrieveAsIfPublished); });
        }

        [Fact]
        public void AllMetadataRequestsRoundTripThroughActualSdkDataContracts()
        {
            Type[] known = { typeof(EntityMetadata), typeof(StringAttributeMetadata), typeof(MemoAttributeMetadata),
                typeof(DateTimeAttributeMetadata), typeof(IntegerAttributeMetadata), typeof(PicklistAttributeMetadata),
                typeof(LookupAttributeMetadata), typeof(OneToManyRelationshipMetadata), typeof(EntityKeyMetadata), typeof(EntityFilters) };
            foreach (OrganizationRequest request in RegisterSchemaPlan.Build().Concat<OrganizationRequest>(RegisterSchemaPlan.InspectionRequests()))
            {
                var serializer = new DataContractSerializer(request.GetType(), known);
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, request); stream.Position = 0;
                    var restored = Assert.IsAssignableFrom<OrganizationRequest>(serializer.ReadObject(stream));
                    Assert.Equal(request.RequestName, restored.RequestName);
                    Assert.Equal(request.Parameters.Count, restored.Parameters.Count);
                }
            }
        }
    }
}
