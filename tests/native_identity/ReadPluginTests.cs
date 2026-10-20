using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using CareOps.Decisions.DataverseIdentity;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    internal sealed class ReadHost : IServiceProvider, IOrganizationServiceFactory, IOrganizationService
    {
        internal readonly FakeDataverse Identity = new FakeDataverse();
        internal static readonly Guid RecordId = Guid.Parse("50000000-0000-0000-0000-000000000005");
        internal Entity Record = new Entity(ReadCredentialingRecord.Table, RecordId) { RowVersion = "42" };
        internal ColumnSet? ReadColumns;
        internal int FactoryCalls, RecordReads;
        internal bool MissingContext, MissingFactory, NoSecondService;
        internal Exception? RecordFailure;
        internal ReadHost()
        {
            var c = Identity.Context;
            c.MessageName = ReadCredentialingRecord.ApiName; c.Stage = 30; c.Mode = 0; c.Depth = 1;
            c.InputParameters["Target"] = new EntityReference(ReadCredentialingRecord.Table, RecordId);
            foreach (string field in new[] { "recordkey", "practitionerref", "professionref", "facilityref", "scopeversionref", "scopetext", "conditionstext", "supervisiontext" })
                Record["dft_" + field] = "synthetic-" + field;
            foreach (string field in new[] { "validfrom", "validuntil", "sourcecheckedat", "sourcecurrentuntil", "projectedasof" })
                Record["dft_" + field] = new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
            Record["dft_recordedstate"] = new OptionSetValue(602170000);
            Record["dft_latestsequence"] = 1; Record["dft_projectionsequence"] = 0;
        }
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPluginExecutionContext)) return MissingContext ? null! : Identity.Context;
            Assert.Equal(typeof(IOrganizationServiceFactory), serviceType);
            return MissingFactory ? null! : this;
        }
        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            FactoryCalls++; Assert.Equal(FakeDataverse.UserId, userId);
            IOrganizationService first = Identity.CreateOrganizationService(userId);
            return first == null || (NoSecondService && FactoryCalls == 2) ? null! : this;
        }
        public OrganizationResponse Execute(OrganizationRequest request) => Identity.Execute(request);
        public Entity Retrieve(string entityName, Guid id, ColumnSet columns)
        {
            if (entityName == "systemuser") return Identity.Retrieve(entityName, id, columns);
            Assert.Equal(ReadCredentialingRecord.Table, entityName); Assert.Equal(RecordId, id);
            RecordReads++; ReadColumns = columns;
            if (RecordFailure != null) throw RecordFailure;
            return Record;
        }
        public EntityCollection RetrieveMultiple(QueryBase query) => Identity.RetrieveMultiple(query);
        public Guid Create(Entity entity) => throw new InvalidOperationException("Unexpected write");
        public void Update(Entity entity) => throw new InvalidOperationException("Unexpected write");
        public void Delete(string name, Guid id) => throw new InvalidOperationException("Unexpected write");
        public void Associate(string name, Guid id, Relationship r, EntityReferenceCollection refs) => throw new InvalidOperationException("Unexpected write");
        public void Disassociate(string name, Guid id, Relationship r, EntityReferenceCollection refs) => throw new InvalidOperationException("Unexpected write");
        internal void Run() => new ReadCredentialingRecord().Execute(this);
        internal void Denied()
        {
            var error = Assert.Throws<InvalidPluginExecutionException>(Run);
            Assert.Equal("Credentialing record read unavailable. No decision or scope has been changed.", error.Message);
            Assert.False((bool)Identity.Context.OutputParameters["ReadSucceeded"]);
            Assert.Single(Identity.Context.OutputParameters);
        }
    }

    public sealed class ReadPluginTests
    {
        [Fact]
        public void ReadsOneWhitelistedSnapshotAsCallerWithoutClearanceOrEvidenceDisclosure()
        {
            var h = new ReadHost(); h.Record["dft_actorref"] = "restricted";
            h.Record.FormattedValues["dft_scopetext"] = "not-trusted-format";
            h.Record.KeyAttributes["internal"] = "restricted";
            h.Record.RelatedEntities[new Relationship("restricted")] = new EntityCollection();
            h.Run();
            Assert.Equal(2, h.FactoryCalls); Assert.Equal(1, h.RecordReads);
            Assert.NotNull(h.ReadColumns); Assert.False(h.ReadColumns.AllColumns); Assert.Equal(16, h.ReadColumns.Columns.Count);
            var output = h.Identity.Context.OutputParameters;
            Assert.True((bool)output["ReadSucceeded"]); Assert.Equal(5, output.Count);
            Assert.Equal(ReadCredentialingRecord.Disposition, output["ReadDisposition"]);
            Assert.Equal(DateTimeKind.Utc, Assert.IsType<DateTime>(output["RetrievedAtUtc"]).Kind);
            Assert.Empty(Assert.IsType<string[]>(output["UnavailableFields"]));
            var row = Assert.IsType<Entity>(output["Record"]);
            Assert.Equal(ReadCredentialingRecord.Table, row.LogicalName); Assert.Equal(ReadHost.RecordId, row.Id);
            Assert.Equal("42", row.RowVersion); Assert.Equal(16, row.Attributes.Count);
            Assert.DoesNotContain("dft_actorref", row.Attributes.Keys);
            Assert.Empty(row.FormattedValues); Assert.Empty(row.KeyAttributes); Assert.Empty(row.RelatedEntities);
            foreach (string key in h.ReadColumns.Columns)
                if (key != "dft_recordedstate") Assert.Equal(h.Record[key], row[key]);
            ((OptionSetValue)row["dft_recordedstate"]).Value = 602170001;
            Assert.Equal(602170000, ((OptionSetValue)h.Record["dft_recordedstate"]).Value);
        }

        [Fact]
        public void MissingAndNullRestrictionsRemainUnavailableNotNoConditions()
        {
            var h = new ReadHost(); h.Record.Attributes.Remove("dft_conditionstext"); h.Record["dft_supervisiontext"] = null;
            h.Run(); var output = h.Identity.Context.OutputParameters;
            Assert.Equal(new[] { "dft_conditionstext", "dft_supervisiontext" }, Assert.IsType<string[]>(output["UnavailableFields"]));
            var row = Assert.IsType<Entity>(output["Record"]);
            Assert.False(row.Contains("dft_conditionstext")); Assert.False(row.Contains("dft_supervisiontext"));
            Assert.Equal(ReadCredentialingRecord.Disposition, output["ReadDisposition"]);
        }

        [Fact]
        public void EmptyTextIsPreservedWithoutConvertingItToAClinicalConclusion()
        {
            var h = new ReadHost(); h.Record["dft_conditionstext"] = ""; h.Record["dft_scopetext"] = "<untrusted-text>";
            h.Run(); var row = Assert.IsType<Entity>(h.Identity.Context.OutputParameters["Record"]);
            Assert.Equal("", row["dft_conditionstext"]); Assert.Equal("<untrusted-text>", row["dft_scopetext"]);
        }

        [Fact]
        public void NullOrMissingPlatformContextCannotBeUsed()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadCredentialingRecord().Execute(null!));
            var h = new ReadHost { MissingContext = true };
            Assert.Throws<InvalidPluginExecutionException>(h.Run); Assert.Equal(0, h.FactoryCalls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        [InlineData(4)] [InlineData(5)] [InlineData(6)]
        public void WrongRegistrationOrOfflineContextsFailWithoutDataAccess(int change)
        {
            var h = new ReadHost(); var c = h.Identity.Context;
            if (change == 0) c.MessageName = "Retrieve";
            if (change == 1) c.Stage = 40;
            if (change == 2) c.Mode = 1;
            if (change == 3) c.Depth = 2;
            if (change == 4) c.ParentContext = new RemoteExecutionContext();
            if (change == 5) c.IsExecutingOffline = true;
            if (change == 6) c.IsOfflinePlayback = true;
            c.OutputParameters["Record"] = new Entity("restricted"); c.OutputParameters["ReadSucceeded"] = true;
            h.Denied(); Assert.Equal(0, h.FactoryCalls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        [InlineData(4)] [InlineData(5)]
        public void RequiresOneExactBoundReferenceAndRejectsPrivilegeClaims(int change)
        {
            var h = new ReadHost(); var p = h.Identity.Context.InputParameters;
            if (change == 0) p.Clear();
            if (change == 1) { p.Clear(); p["Other"] = ReadHost.RecordId; }
            if (change == 2) p["Target"] = ReadHost.RecordId;
            if (change == 3) p["Target"] = new EntityReference("dft_credentialingdecision", ReadHost.RecordId);
            if (change == 4) p["Target"] = new EntityReference(ReadCredentialingRecord.Table, Guid.Empty);
            if (change == 5) ((EntityReference)p["Target"]).KeyAttributes["other"] = "value";
            h.Denied(); Assert.Equal(0, h.FactoryCalls);
        }

        [Fact]
        public void CallerCannotSupplyRoleOrOverrideTheAllowedColumns()
        {
            var h = new ReadHost(); h.Identity.Context.InputParameters["Role"] = "CMO";
            h.Denied(); Assert.Equal(0, h.FactoryCalls);
        }

        [Fact]
        public void MissingFactoryOrSecondCallerServiceDoesNotCauseElevation()
        {
            var h = new ReadHost { MissingFactory = true }; h.Denied(); Assert.Equal(0, h.FactoryCalls);
            h = new ReadHost { NoSecondService = true }; h.Denied(); Assert.Equal(2, h.FactoryCalls); Assert.Equal(0, h.RecordReads);
        }

        [Fact]
        public void IdentityFailureStopsBeforeRegisterReadAndHidesItsDetails()
        {
            var h = new ReadHost(); h.Identity.User["applicationid"] = FakeDataverse.Org;
            h.Denied(); Assert.Equal(0, h.RecordReads);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void NullWrongTableOrWrongRecordIsNotAnEmptyRegister(int change)
        {
            var h = new ReadHost();
            if (change == 0) h.Record = null!;
            if (change == 1) h.Record.LogicalName = "dft_credentialingdecision";
            if (change == 2) h.Record.Id = FakeDataverse.Org;
            h.Denied(); Assert.Equal(1, h.RecordReads);
        }

        [Theory]
        [InlineData(null)] [InlineData("")] [InlineData("0")] [InlineData("-1")]
        [InlineData(" 42")] [InlineData("4.2")] [InlineData("18446744073709551616")]
        public void UnversionedOrMalformedSnapshotsFailClosed(string? version)
        {
            var h = new ReadHost(); h.Record.RowVersion = version; h.Denied();
        }

        [Fact]
        public void EachWrongFieldTypeIsRejectedInsteadOfCoerced()
        {
            foreach (string key in new ReadHost().Record.Attributes.Keys.ToArray())
            {
                var h = new ReadHost(); h.Record[key] = true; h.Denied();
            }
        }

        [Fact]
        public void OversizedTextIsNotReturned()
        {
            var h = new ReadHost(); h.Record["dft_scopetext"] = new string('x', 32769); h.Denied();
            h = new ReadHost(); h.Record["dft_scopetext"] = new string('x', 32768); h.Run();
        }

        [Fact]
        public void DataverseFaultTimeoutAndPluginFailureDoNotExposeRawDetailsOrRetry()
        {
            Exception[] failures = {
                new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "restricted-details" }),
                new TimeoutException("restricted-details"), new InvalidPluginExecutionException("restricted-details")
            };
            foreach (Exception failure in failures)
            {
                var h = new ReadHost { RecordFailure = failure }; h.Denied(); Assert.Equal(1, h.RecordReads);
            }
        }

        [Fact]
        public void RecordedRevocationAndStaleValuesAreNotConvertedToApproval()
        {
            var h = new ReadHost(); h.Record["dft_recordedstate"] = new OptionSetValue(602170003);
            h.Record["dft_sourcecurrentuntil"] = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            h.Run(); var row = Assert.IsType<Entity>(h.Identity.Context.OutputParameters["Record"]);
            Assert.Equal(602170003, ((OptionSetValue)row["dft_recordedstate"]).Value);
            Assert.Equal(2020, ((DateTime)row["dft_sourcecurrentuntil"]).Year);
            Assert.Equal(ReadCredentialingRecord.Disposition, h.Identity.Context.OutputParameters["ReadDisposition"]);
        }
    }
}
