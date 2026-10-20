using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class NativeConfirmationSourceTests
    {
        internal static readonly Guid SourceId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        internal static readonly Guid RecordId = Guid.Parse("60000000-0000-0000-0000-000000000006");
        internal static readonly Guid DecisionId = Guid.Parse("70000000-0000-0000-0000-000000000007");
        internal static readonly Guid OwnerId = Guid.Parse("80000000-0000-0000-0000-000000000008");
        internal static readonly Guid TokenId = Guid.Parse("90000000-0000-0000-0000-000000000009");
        private const string Failure = "The original credentialing confirmation source is unavailable. No decision was repeated or changed.";
        internal sealed class Store : IOrganizationServiceFactory, IOrganizationService
        {
            internal readonly FakeDataverse Identity = new FakeDataverse();
            internal readonly DateTime At = DateTime.UtcNow;
            internal Entity Source = new Entity(ConfirmationSourceSchema.Table, SourceId) { RowVersion = "12" };
            internal Entity Payload = new Entity(RegisterSchemaPlan.DecisionTable, DecisionId);
            internal DecisionAppend Intent;
            internal ColumnSet? SourceColumns;
            internal int SourceReads;
            internal bool FailRead;
            internal Store()
            {
                foreach (string name in new[] { "function", "profileversionref", "authorityevidenceref", "scopeversionref" }) Payload["dft_" + name] = "synthetic-" + name;
                Payload["dft_actorref"] = FakeDataverse.PersonId.ToString("D");
                Payload["dft_decisionevidenceref"] = NativeConfirmationSource.EvidenceReference(SourceId);
                Payload["dft_profilehash"] = new string('a', 64); Payload["dft_casehash"] = new string('b', 64);
                Payload["dft_scopetext"] = "Synthetic source scope only"; Payload["dft_conditionstext"] = ""; Payload["dft_supervisiontext"] = "Synthetic supervision";
                Payload["dft_validfrom"] = At.AddDays(-1); Payload["dft_validuntil"] = At.AddDays(1);
                Payload["dft_effectiveat"] = At.AddMinutes(-1); Payload["dft_recordedat"] = At.AddMinutes(-1);
                Payload["dft_recordedstate"] = new OptionSetValue(602170001);
                Intent = NewIntent(); SaveDocument();
                Source["dft_confirmationkey"] = SourceId.ToString("N"); Source["dft_protocol"] = ConfirmationSourceSchema.Protocol;
                Source["dft_actorref"] = FakeDataverse.PersonId.ToString("D"); Source["dft_function"] = "synthetic-function";
                Source["dft_pathway"] = "synthetic-pathway"; Source["dft_policyref"] = "synthetic-rule-v1";
                Source["dft_confirmedutc"] = At.AddMinutes(-2).ToString("O", CultureInfo.InvariantCulture);
                Source["dft_currentuntilutc"] = At.AddHours(1).ToString("O", CultureInfo.InvariantCulture);
                Source["dft_fencetoken"] = TokenId.ToString("N"); Source["dft_withdrawn"] = false;
                Source["dft_recordedstate"] = new OptionSetValue(602170001); Source["statecode"] = new OptionSetValue(0);
                Source["createdby"] = new EntityReference("systemuser", FakeDataverse.UserId);
                Source["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, RecordId);
            }
            internal DecisionAppend NewIntent(Guid? record = null) => new DecisionAppend(Payload, record ?? RecordId, "42", 0, null, OwnerId, OwnerId);
            internal void SaveDocument()
            {
                Source["dft_originaldocument"] = DecisionIntentDocument.Encode(FakeDataverse.Org, Intent);
                Source["dft_originaldigest"] = DecisionConfirmationHash.Compute(FakeDataverse.Org, Intent);
            }
            internal CallerBinding Binding() => CallerResolver.ResolveBinding(Identity.Context, this);
            public IOrganizationService CreateOrganizationService(Guid? id)
            { Assert.Equal(FakeDataverse.UserId, id); return this; }
            public OrganizationResponse Execute(OrganizationRequest request) => Identity.Execute(request);
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                if (table == "systemuser") return Identity.Retrieve(table, id, columns);
                Assert.Equal(ConfirmationSourceSchema.Table, table); Assert.Equal(SourceId, id);
                SourceReads++; SourceColumns = columns; if (FailRead) throw new InvalidOperationException("private native error"); return Source;
            }
            public EntityCollection RetrieveMultiple(QueryBase request) => Identity.RetrieveMultiple(request);
            public Guid Create(Entity row) => throw new NotSupportedException("No source capture or save");
            public void Update(Entity row) => throw new NotSupportedException("No source locking in this reader");
            public void Delete(string table, Guid id) => throw new NotSupportedException();
            public void Associate(string table, Guid id, Relationship r, EntityReferenceCollection rows) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship r, EntityReferenceCollection rows) => throw new NotSupportedException();
            internal NativeConfirmation Read() => NativeConfirmationSource.Read(Binding(), RecordId, SourceId);
        }
        private static void Denied(Store store)
        {
            var error = Assert.Throws<InvalidPluginExecutionException>(() => store.Read());
            Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
        }

        [Fact]
        public void OriginalSourceIsReadOnceWithoutRebuildingFromCurrentCaseData()
        {
            var s = new Store(); NativeConfirmation value = s.Read();
            Assert.Equal(SourceId, value.SourceId); Assert.Equal("12", value.RowVersion); Assert.Equal("synthetic-rule-v1", value.PolicyReference);
            Assert.Equal(TokenId.ToString("N"), value.FenceToken); Assert.True(s.Intent.Matches(value.Intent.ToEntity()));
            Assert.Equal(FakeDataverse.Org, value.Confirmation.Organization); Assert.Equal(FakeDataverse.PersonId, value.Confirmation.ActorPerson);
            Assert.Equal(NativeConfirmationSource.EvidenceReference(SourceId), value.Confirmation.EvidenceReference);
            Assert.Equal(DecisionConfirmationHash.Compute(FakeDataverse.Org, s.Intent), value.Confirmation.Digest);
            Assert.Equal(602170001, value.Confirmation.RecordedState); Assert.Equal("synthetic-function", value.Confirmation.RequiredFunction);
            Assert.Equal("synthetic-pathway", value.Confirmation.Pathway); Assert.False(value.Confirmation.Withdrawn);
            Assert.Equal(new DateTimeOffset(s.At.AddMinutes(-2)), value.Confirmation.ConfirmedAt);
            Assert.Equal(new DateTimeOffset(s.At.AddHours(1)), value.Confirmation.CurrentUntil);
            Assert.Equal(1, s.SourceReads); Assert.NotNull(s.SourceColumns); Assert.False(s.SourceColumns.AllColumns);
            Assert.Equal(ConfirmationSourceSchema.ReadColumns, s.SourceColumns.Columns);
            s.Source["dft_pathway"] = "changed"; s.Source["dft_originaldocument"] = "changed";
            Assert.Equal("synthetic-pathway", value.Confirmation.Pathway); Assert.True(s.Intent.Matches(value.Intent.ToEntity()));
        }
        [Fact]
        public void RequiredBindingAndIdsCannotBeGuessedOrDefaulted()
        {
            var s = new Store(); CallerBinding binding = s.Binding();
            Assert.Throws<ArgumentNullException>(() => NativeConfirmationSource.Read(null!, RecordId, SourceId));
            Assert.Throws<ArgumentException>(() => NativeConfirmationSource.Read(binding, Guid.Empty, SourceId));
            Assert.Throws<ArgumentException>(() => NativeConfirmationSource.Read(binding, RecordId, Guid.Empty));
            Assert.Throws<ArgumentException>(() => NativeConfirmationSource.EvidenceReference(Guid.Empty)); Assert.Equal(0, s.SourceReads);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void ExactCanonicalNativeSourceRowIsRequired(int change)
        {
            var s = new Store(); if (change == 0) s.Source = null!;
            if (change == 1) s.Source.LogicalName = "other"; if (change == 2) s.Source.Id = OwnerId;
            if (change == 3) s.Source.KeyAttributes.Add("name", "alias"); Denied(s);
        }
        [Theory]
        [InlineData(null)] [InlineData("")] [InlineData("0")] [InlineData("012")] [InlineData("-1")] [InlineData("18446744073709551616")]
        public void APositiveCanonicalNativeVersionIsMandatory(string? version)
        { var s = new Store(); s.Source.RowVersion = version; Denied(s); }
        [Fact]
        public void MissingNullOrCoercedRequiredFieldsNeverProduceConfirmation()
        {
            foreach (string column in ConfirmationSourceSchema.ReadColumns.Where(x => x != "createdonbehalfby"))
            {
                var missing = new Store(); missing.Source.Attributes.Remove(column); Denied(missing);
                foreach (object? value in new object?[] { null, 1 }) { var s = new Store(); s.Source[column] = value!; Denied(s); }
            }
        }
        [Fact]
        public void WithdrawalInactiveProtocolIdentityAndCreatorMismatchAreRejected()
        {
            var changes = new Dictionary<string, object>
            {
                ["dft_withdrawn"] = true, ["statecode"] = new OptionSetValue(1), ["dft_protocol"] = "other-version",
                ["dft_confirmationkey"] = RecordId.ToString("N"), ["dft_actorref"] = FakeDataverse.UserId.ToString("D"),
                ["createdby"] = new EntityReference("systemuser", OwnerId), ["createdonbehalfby"] = new EntityReference("systemuser", OwnerId),
                ["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, OwnerId)
            };
            foreach (var change in changes) { var s = new Store(); s.Source[change.Key] = change.Value; Denied(s); }
            var explicitNull = new Store(); explicitNull.Source["createdonbehalfby"] = null!; Assert.Equal(SourceId, explicitNull.Read().SourceId);
            foreach (string field in new[] { "createdby", "dft_record" })
            {
                var wrong = new Store(); ((EntityReference)wrong.Source[field]).LogicalName = "wrong"; Denied(wrong);
                wrong = new Store(); ((EntityReference)wrong.Source[field]).KeyAttributes.Add("name", "alias"); Denied(wrong);
            }
        }
        [Fact]
        public void EvenInternallyConsistentDocumentMustMatchOriginalSourceActorRecordAndEvidence()
        {
            foreach (string field in new[] { "dft_actorref", "dft_decisionevidenceref", "dft_function" })
            {
                var s = new Store(); s.Payload[field] = "different"; s.Intent = s.NewIntent(); s.SaveDocument(); Denied(s);
            }
            var root = new Store(); root.Intent = root.NewIntent(OwnerId); root.SaveDocument(); Denied(root);
            var outcome = new Store(); outcome.Source["dft_recordedstate"] = new OptionSetValue(602170002); Denied(outcome);
            var mismatch = new Store(); mismatch.Source["dft_originaldigest"] = new string('c', 64); Denied(mismatch);
        }
        [Fact]
        public void ReferenceFieldsAreBoundedExactAndDoNotNormaliseInvalidUtf16()
        {
            foreach (string field in new[] { "dft_originaldigest", "dft_function", "dft_pathway", "dft_policyref", "dft_fencetoken" })
            foreach (string value in new[] { "", " ", " x", "x ", "a\nb", new string('x', 257), "\uD800" })
            { var s = new Store(); s.Source[field] = value; Denied(s); }
            foreach (string value in new[] { "not-guid", Guid.Empty.ToString("N"), TokenId.ToString("D"), "AAAAAAAA000000000000000000000009" })
            { var s = new Store(); s.Source["dft_fencetoken"] = value; Denied(s); }
        }
        [Fact]
        public void OriginalTimeIsExactUtcAndMustRemainCurrentWithoutChangingTheDecision()
        {
            foreach (string field in new[] { "dft_confirmedutc", "dft_currentuntilutc" })
            foreach (string value in new[] { "invalid", "2026-09-25T00:00:00.0000000", "2026-09-25T00:00:00.0000000+00:00", "2026-09-25T00:00:00Z" })
            { var s = new Store(); s.Source[field] = value; Denied(s); }
            var expired = new Store(); expired.Source["dft_currentuntilutc"] = expired.At.AddMinutes(-1).ToString("O", CultureInfo.InvariantCulture); Denied(expired);
            var future = new Store(); future.Source["dft_confirmedutc"] = future.At.AddMinutes(1).ToString("O", CultureInfo.InvariantCulture); Denied(future);
            var ordering = new Store(); ordering.Source["dft_confirmedutc"] = ordering.At.ToString("O", CultureInfo.InvariantCulture); Denied(ordering);
            var futureRecorded = new Store(); futureRecorded.Payload["dft_recordedat"] = futureRecorded.At.AddHours(1);
            futureRecorded.Intent = futureRecorded.NewIntent(); futureRecorded.SaveDocument(); Denied(futureRecorded);
        }
        [Fact]
        public void NativeFailureCannotLeakPrivateEvidenceOrTriggerARepair()
        { var s = new Store { FailRead = true }; Denied(s); Assert.Equal(1, s.SourceReads); }
        [Fact]
        public void ReadColumnListIsIsolatedFromEarlierRequests()
        {
            string[] fields = ConfirmationSourceSchema.ReadColumns; fields[0] = "*";
            var s = new Store(); s.Read(); s.SourceColumns!.Columns.Clear(); s.Read(); Assert.Equal(17, s.SourceColumns!.Columns.Count);
        }
        [Fact]
        public void SchemaIsModuleOwnedAuditedBoundedAndDefaultsToUnavailable()
        {
            var requests = ConfirmationSourceSchema.Build(); Assert.Equal(16, requests.Count);
            CreateEntityRequest table = Assert.Single(requests.OfType<CreateEntityRequest>());
            Assert.Equal(ConfirmationSourceSchema.Table, table.Entity.SchemaName); Assert.Equal(OwnershipTypes.UserOwned, table.Entity.OwnershipType);
            Assert.True(table.Entity.IsAuditEnabled.Value); Assert.False(table.HasNotes); Assert.False(table.HasActivities); Assert.False(table.HasFeedback);
            Assert.False(table.PrimaryAttribute.IsSecured);
            var attributes = requests.OfType<CreateAttributeRequest>().Select(x => x.Attribute).ToArray(); Assert.Equal(13, attributes.Length);
            Assert.All(attributes, a =>
            {
                Assert.Equal(a.SchemaName != "dft_confirmationkey", a.IsSecured);
                Assert.True(a.IsAuditEnabled.Value); Assert.Equal(AttributeRequiredLevel.ApplicationRequired, a.RequiredLevel.Value);
            });
            Assert.Equal(DecisionIntentDocument.MaximumBytes, Assert.Single(attributes.OfType<MemoAttributeMetadata>()).MaxLength);
            Assert.True(Assert.Single(attributes.OfType<BooleanAttributeMetadata>()).DefaultValue);
            Assert.Equal(1, Assert.Single(attributes.OfType<BooleanAttributeMetadata>()).OptionSet.TrueOption.Value);
            Assert.Equal(0, Assert.Single(attributes.OfType<BooleanAttributeMetadata>()).OptionSet.FalseOption.Value);
            Assert.Equal(RegisterSchemaPlan.Unverified, Assert.Single(attributes.OfType<PicklistAttributeMetadata>()).DefaultFormValue);
            var relation = Assert.Single(requests.OfType<CreateOneToManyRequest>());
            Assert.False(relation.Lookup.IsSecured);
            Assert.Equal(RegisterSchemaPlan.RecordTable, relation.OneToManyRelationship.ReferencedEntity);
            Assert.Equal(ConfirmationSourceSchema.Table, relation.OneToManyRelationship.ReferencingEntity);
            Assert.Equal(CascadeType.Restrict, relation.OneToManyRelationship.CascadeConfiguration.Delete);
            Assert.Equal(CascadeType.NoCascade, relation.OneToManyRelationship.CascadeConfiguration.Share);
            Assert.Equal(new[] { "dft_confirmationkey" }, Assert.Single(requests.OfType<CreateEntityKeyRequest>()).EntityKey.KeyAttributes);
            Assert.All(requests, request => Assert.Equal(RegisterSchemaPlan.Solution, request["SolutionUniqueName"]));
        }
        [Fact]
        public void SchemaAndSourceReadUseTheSameActualColumnInventory()
        {
            var custom = ConfirmationSourceSchema.Build().OfType<CreateAttributeRequest>().Select(x => x.Attribute.SchemaName).Append("dft_record");
            Assert.Equal(custom.OrderBy(x => x, StringComparer.Ordinal), ConfirmationSourceSchema.ReadColumns.Where(x => x.StartsWith("dft_", StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal));
            var before = ConfirmationSourceSchema.Build(); ((CreateAttributeRequest)before[1]).Attribute.SchemaName = "changed";
            Assert.Equal("dft_confirmationkey", ((CreateAttributeRequest)ConfirmationSourceSchema.Build()[1]).Attribute.SchemaName);
        }
        [Fact]
        public void OfficialSdkMetadataRoundtripIsNotMistakenForNativeInstallation()
        {
            Type[] known = { typeof(EntityMetadata), typeof(StringAttributeMetadata), typeof(MemoAttributeMetadata), typeof(PicklistAttributeMetadata),
                typeof(BooleanAttributeMetadata), typeof(LookupAttributeMetadata), typeof(OneToManyRelationshipMetadata), typeof(EntityKeyMetadata) };
            foreach (OrganizationRequest request in ConfirmationSourceSchema.Build())
            {
                var serializer = new DataContractSerializer(request.GetType(), known);
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, request); stream.Position = 0;
                    var restored = Assert.IsAssignableFrom<OrganizationRequest>(serializer.ReadObject(stream));
                    Assert.Equal(request.RequestName, restored.RequestName); Assert.Equal(request.Parameters.Count, restored.Parameters.Count);
                }
            }
        }
    }
}
