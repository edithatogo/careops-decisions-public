using System;
using System.Collections.Generic;
using CareOps.Decisions.DataverseIdentity;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    // Simulated SDK responses only: not tests of Dataverse's authentication/security engine.
    internal sealed class FakeDataverse : IOrganizationServiceFactory, IOrganizationService
    {
        internal static readonly Guid Org = Guid.Parse("10000000-0000-0000-0000-000000000001");
        internal static readonly Guid UserId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        internal static readonly Guid DirectoryId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        internal static readonly Guid PersonId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        internal RemoteExecutionContext Context = new RemoteExecutionContext { OrganizationId = Org, UserId = UserId, InitiatingUserId = UserId };
        internal readonly WhoAmIResponse Who = new WhoAmIResponse();
        internal readonly EntityKeyMetadata Key = new EntityKeyMetadata
        {
            LogicalName = CallerResolver.IdentityKey, KeyAttributes = new[] { CallerResolver.DirectoryColumn }
        };
        internal Entity User = new Entity("systemuser", UserId)
        {
            ["isdisabled"] = false, ["accessmode"] = new OptionSetValue(0), ["azureactivedirectoryobjectid"] = DirectoryId
        };
        internal readonly Entity Person = new Entity(CallerResolver.PersonTable, PersonId)
        {
            ["statecode"] = new OptionSetValue(0), [CallerResolver.DirectoryColumn] = DirectoryId.ToString("D")
        };
        internal EntityCollection Rows = new EntityCollection();
        internal bool NoService, WrongWhoType, WrongKeyType, NullKey;
        internal int FailCall = -1;
        internal readonly List<string> Calls = new List<string>();
        internal Guid? FactoryUser;
        internal QueryExpression? Query;
        internal ColumnSet? UserColumns;
        internal RetrieveEntityKeyRequest? KeyRequest;
        internal FakeDataverse()
        {
            Who.Results["UserId"] = UserId; Who.Results["OrganizationId"] = Org;
            SetKeyProperty(nameof(EntityKeyMetadata.EntityLogicalName), CallerResolver.PersonTable);
            SetKeyProperty(nameof(EntityKeyMetadata.EntityKeyIndexStatus), EntityKeyIndexStatus.Active);
            Rows.Entities.Add(Person);
        }
        // Server-populated SDK properties have internal setters. Reflection is limited to
        // this response fixture; production uses only the supported read-only SDK properties.
        internal void SetKeyProperty(string name, object value)
        {
            var property = typeof(EntityKeyMetadata).GetProperty(name);
            Assert.NotNull(property); Assert.True(property.CanWrite); property.SetValue(Key, value);
        }
        private void Call(string name)
        {
            Calls.Add(name);
            if (Calls.Count == FailCall) throw new InvalidOperationException("Synthetic unavailable SDK operation");
        }
        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            Call("service"); FactoryUser = userId; return NoService ? null! : this;
        }
        public OrganizationResponse Execute(OrganizationRequest request)
        {
            Call(request.RequestName);
            if (request is WhoAmIRequest) return WrongWhoType ? new OrganizationResponse() : Who;
            KeyRequest = Assert.IsType<RetrieveEntityKeyRequest>(request);
            if (WrongKeyType) return new OrganizationResponse();
            var response = new RetrieveEntityKeyResponse();
            response.Results["EntityKeyMetadata"] = NullKey ? null : Key;
            return response;
        }
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            Call("retrieve-user"); Assert.Equal("systemuser", entityName); Assert.Equal(UserId, id);
            UserColumns = columnSet; return User;
        }
        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            Call("query-person"); Query = Assert.IsType<QueryExpression>(query); return Rows;
        }
        public Guid Create(Entity entity) => throw new InvalidOperationException("Unexpected write");
        public void Update(Entity entity) => throw new InvalidOperationException("Unexpected write");
        public void Delete(string entityName, Guid id) => throw new InvalidOperationException("Unexpected write");
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new InvalidOperationException("Unexpected write");
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new InvalidOperationException("Unexpected write");
    }

    public sealed class ResolverTests
    {
        private static CallerPerson Resolve(FakeDataverse f) => CallerResolver.Resolve(f.Context, f);
        private static void Denied(FakeDataverse f) => Assert.Throws<InvalidPluginExecutionException>(() => Resolve(f));

        [Fact]
        public void UsesActualCallerAndCanonicalPersonNamespaceWithMinimalReads()
        {
            var f = new FakeDataverse(); CallerPerson result = Resolve(f);
            Assert.Equal(FakeDataverse.Org, result.Organization); Assert.Equal(FakeDataverse.UserId, result.PlatformUser);
            Assert.Equal(FakeDataverse.DirectoryId, result.DirectoryObject); Assert.Equal(FakeDataverse.PersonId, result.Person);
            Assert.NotEqual(result.PlatformUser, result.Person); Assert.Equal(FakeDataverse.UserId, f.FactoryUser);
            Assert.Equal(new[] { "service", "WhoAmI", "retrieve-user", "RetrieveEntityKey", "query-person" }, f.Calls);
            Assert.NotNull(f.UserColumns); Assert.False(f.UserColumns.AllColumns); Assert.Equal(4, f.UserColumns.Columns.Count);
            Assert.NotNull(f.KeyRequest); Assert.False(f.KeyRequest.RetrieveAsIfPublished);
            Assert.Equal(CallerResolver.PersonTable, f.KeyRequest.EntityLogicalName);
            Assert.Equal(CallerResolver.IdentityKey, f.KeyRequest.LogicalName);
            Assert.NotNull(f.Query); Assert.Equal(2, f.Query.TopCount); Assert.Equal(CallerResolver.PersonTable, f.Query.EntityName);
            Assert.False(f.Query.ColumnSet.AllColumns); Assert.Equal(2, f.Query.ColumnSet.Columns.Count);
            var filter = Assert.Single(f.Query.Criteria.Conditions);
            Assert.Equal(CallerResolver.DirectoryColumn, filter.AttributeName); Assert.Equal(ConditionOperator.Equal, filter.Operator);
            Assert.Equal(FakeDataverse.DirectoryId.ToString("D"), Assert.Single(filter.Values));
        }

        [Fact]
        public void BrowserClaimsAreIgnoredAndDoNotCreateAuthority()
        {
            var f = new FakeDataverse(); f.Context.InputParameters["Actor"] = FakeDataverse.PersonId;
            f.Context.InputParameters["Approved"] = true; f.Context.InputParameters["Role"] = "CMO";
            Assert.Equal(FakeDataverse.PersonId, Resolve(f).Person);
            Assert.Equal(3, f.Context.InputParameters.Count);
        }

        [Fact]
        public void NullHostServicesFailBeforeAnyRead()
        {
            var f = new FakeDataverse();
            Assert.Throws<ArgumentNullException>(() => CallerResolver.Resolve(null!, f));
            Assert.Throws<ArgumentNullException>(() => CallerResolver.Resolve(f.Context, null!));
            Assert.Empty(f.Calls);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void RejectsMissingContextAndImpersonation(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.Context.OrganizationId = Guid.Empty;
            if (change == 1) f.Context.InitiatingUserId = Guid.Empty;
            if (change == 2) f.Context.UserId = FakeDataverse.PersonId;
            if (change == 3) f.Context.UserId = Guid.Empty;
            Denied(f); Assert.Empty(f.Calls);
        }

        [Fact]
        public void NullServiceDoesNotEscalateToSystem()
        {
            var f = new FakeDataverse { NoService = true }; Denied(f);
            Assert.Equal(new[] { "service" }, f.Calls); Assert.Equal(FakeDataverse.UserId, f.FactoryUser);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void WhoAmIMustMatchBothOrganizationAndInitiatingUser(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.WrongWhoType = true;
            if (change == 1) f.Who.Results["UserId"] = FakeDataverse.PersonId;
            if (change == 2) f.Who.Results["OrganizationId"] = FakeDataverse.PersonId;
            Denied(f); Assert.Equal(2, f.Calls.Count);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
        public void UserMustBeEnabledInteractiveAndExactlyIdentified(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.User = null!;
            if (change == 1) f.User.LogicalName = "contact";
            if (change == 2) f.User.Id = FakeDataverse.PersonId;
            if (change == 3) f.User.Attributes.Remove("isdisabled");
            if (change == 4) f.User["isdisabled"] = "false";
            if (change == 5) f.User["isdisabled"] = true;
            if (change == 6) f.User.Attributes.Remove("accessmode");
            if (change == 7) f.User["accessmode"] = 0;
            if (change == 8) f.User["accessmode"] = new OptionSetValue(4);
            Denied(f); Assert.Equal(3, f.Calls.Count);
        }

        [Theory]
        [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void NonHumanDecisionAccessModesAreNotAccepted(int mode)
        {
            var f = new FakeDataverse(); f.User["accessmode"] = new OptionSetValue(mode); Denied(f);
        }

        [Fact]
        public void ApplicationIdentityCannotMasqueradeAsAReviewer()
        {
            var f = new FakeDataverse(); f.User["applicationid"] = FakeDataverse.DirectoryId; Denied(f);
            f.User["applicationid"] = "not-a-guid"; Denied(f);
            f.User["applicationid"] = Guid.Empty; Assert.Equal(FakeDataverse.PersonId, Resolve(f).Person);
            f.User["applicationid"] = null; Assert.Equal(FakeDataverse.PersonId, Resolve(f).Person);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)]
        public void MissingDirectoryIdentityIsNotReplacedWithEmail(int change)
        {
            var f = new FakeDataverse(); f.User["internalemailaddress"] = "synthetic@example.invalid";
            if (change == 0) f.User.Attributes.Remove("azureactivedirectoryobjectid");
            if (change == 1) f.User["azureactivedirectoryobjectid"] = "not-a-guid";
            if (change == 2) f.User["azureactivedirectoryobjectid"] = Guid.Empty;
            Denied(f); Assert.Equal(3, f.Calls.Count);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
        public void RequiresPublishedActiveSingleColumnIdentityIndex(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.WrongKeyType = true;
            if (change == 1) f.NullKey = true;
            if (change == 2) f.SetKeyProperty(nameof(EntityKeyMetadata.EntityLogicalName), "contact");
            if (change == 3) f.Key.LogicalName = "other";
            if (change == 4) f.SetKeyProperty(nameof(EntityKeyMetadata.EntityKeyIndexStatus), EntityKeyIndexStatus.Pending);
            if (change == 5) f.Key.KeyAttributes = null!;
            if (change == 6) f.Key.KeyAttributes = Array.Empty<string>();
            if (change == 7) f.Key.KeyAttributes = new[] { CallerResolver.DirectoryColumn, "statecode" };
            if (change == 8) f.Key.KeyAttributes = new[] { "dft_workemail" };
            Denied(f); Assert.Equal(4, f.Calls.Count);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void MissingPartialOrAmbiguousQueryResultsNeverSelectFirstPerson(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.Rows = null!;
            if (change == 1) f.Rows.MoreRecords = true;
            if (change == 2) f.Rows.Entities.Clear();
            if (change == 3) f.Rows.Entities.Add(f.Person);
            Denied(f);
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
        public void PersonRecordMustBeActiveAndMatchTheDirectoryExactly(int change)
        {
            var f = new FakeDataverse();
            if (change == 0) f.Rows.Entities[0] = null!;
            if (change == 1) f.Person.LogicalName = "contact";
            if (change == 2) f.Person.Id = Guid.Empty;
            if (change == 3) f.Person.Attributes.Remove("statecode");
            if (change == 4) f.Person["statecode"] = 0;
            if (change == 5) f.Person["statecode"] = new OptionSetValue(1);
            if (change == 6) f.Person.Attributes.Remove(CallerResolver.DirectoryColumn);
            if (change == 7) f.Person[CallerResolver.DirectoryColumn] = FakeDataverse.DirectoryId;
            if (change == 8) f.Person[CallerResolver.DirectoryColumn] = "malformed";
            Denied(f);
        }

        [Fact]
        public void ParsedButDifferentDirectoryIdentityIsRejected()
        {
            var f = new FakeDataverse(); f.Person[CallerResolver.DirectoryColumn] = FakeDataverse.UserId.ToString("D"); Denied(f);
        }

        [Theory]
        [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void FailuresPropagateWithoutPrivilegeEscalationOrRetries(int failedCall)
        {
            var f = new FakeDataverse { FailCall = failedCall };
            Assert.Throws<InvalidOperationException>(() => Resolve(f)); Assert.Equal(failedCall, f.Calls.Count);
        }
    }
}
