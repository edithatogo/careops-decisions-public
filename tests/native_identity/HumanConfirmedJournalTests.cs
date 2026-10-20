using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.Authority;
using CareOps.Decisions.DataverseIdentity;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using AuthorityRequest = CareOps.Decisions.Authority.Request;

namespace CareOps.Decisions.DataverseIdentityTests
{
    public sealed class HumanConfirmedJournalTests
    {
        private static Guid Id(int n) => Guid.Parse("10000000-0000-0000-0000-" + n.ToString("D12", CultureInfo.InvariantCulture));
        private const string Failure = "Credentialing decision could not be verified for recording. No automatic reapproval or repair was attempted.";
        private sealed class Context : RealProxy
        {
            internal readonly Dictionary<string, object?> Fields = new Dictionary<string, object?>
            {
                ["MessageName"] = RegisterJournalAppender.ApiName, ["Stage"] = 30, ["Mode"] = 0, ["Depth"] = 1,
                ["IsInTransaction"] = true, ["ParentContext"] = null, ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false,
                ["OrganizationId"] = Id(1), ["InitiatingUserId"] = Id(2), ["UserId"] = Id(2),
                ["InputParameters"] = new ParameterCollection { ["Target"] = new EntityReference(RegisterSchemaPlan.RecordTable, Id(5)) }
            };
            internal Context() : base(typeof(IPluginExecutionContext)) { }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                return new ReturnMessage(Fields[call.MethodName.Substring(4)], null, 0, call.LogicalCallContext, call);
            }
        }
        private sealed class Store : IOrganizationService, IOrganizationServiceFactory
        {
            internal readonly List<Guid?> RequestedUsers = new List<Guid?>();
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<Entity> Created = new List<Entity>();
            internal readonly EntityCollection Decisions = new EntityCollection(), Events = new EntityCollection();
            internal readonly Dictionary<Guid, Entity> Teams = new Dictionary<Guid, Entity>();
            internal Entity Root = new Entity(RegisterSchemaPlan.RecordTable, Id(5)) { RowVersion = "9", ["dft_latestsequence"] = 0,
                ["dft_practitionerref"] = Id(4).ToString("D"), ["dft_professionref"] = "medical", ["dft_facilityref"] = "synthetic-site" };
            internal Entity User = new Entity("systemuser", Id(2)) { ["isdisabled"] = false, ["accessmode"] = new OptionSetValue(0),
                ["azureactivedirectoryobjectid"] = Id(3) };
            internal string? FailAt;
            internal Action? OnRootRead;
            internal int Updates;
            internal bool MissingService;
            internal Store()
            {
                foreach (int n in new[] { 7, 8 }) Teams.Add(Id(n), new Entity("team", Id(n)) { ["teamtype"] = new OptionSetValue(0),
                    ["isdefault"] = false, ["businessunitid"] = new EntityReference("businessunit", Id(9)) });
            }
            private void Mark(string phase) { Calls.Add(phase); if (phase == FailAt) throw new TimeoutException("synthetic private service error"); }
            public IOrganizationService CreateOrganizationService(Guid? userId)
            { RequestedUsers.Add(userId); Assert.Equal(Id(2), userId); return MissingService ? null! : this; }
            public OrganizationResponse Execute(OrganizationRequest request)
            {
                if (request is WhoAmIRequest)
                { Mark("whoami"); var r = new WhoAmIResponse(); r.Results["OrganizationId"] = Id(1); r.Results["UserId"] = Id(2); return r; }
                if (request is RetrieveEntityKeyRequest)
                {
                    Mark("key"); var r = new RetrieveEntityKeyResponse();
                    // Reuse the existing server-response fixture for SDK properties with internal setters.
                    r.Results["EntityKeyMetadata"] = new CareOps.Decisions.DataverseIdentity.Tests.FakeDataverse().Key;
                    return r;
                }
                Mark("root-cas"); var update = Assert.IsType<UpdateRequest>(request);
                Assert.Equal(ConcurrencyBehavior.IfRowVersionMatches, update.ConcurrencyBehavior);
                Assert.Equal("9", update.Target.RowVersion); Assert.Equal(Id(5), update.Target.Id);
                Assert.DoesNotContain("ownerid", update.Target.Attributes.Keys);
                Root.RowVersion = "10"; Root["dft_latestsequence"] = 1; Root["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, Id(6));
                Updates++; return new UpdateResponse();
            }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            {
                Assert.False(columns.AllColumns);
                if (table == "systemuser") { Mark("user"); Assert.Equal(Id(2), id); return User; }
                if (table == "team") { Mark("team"); Assert.Equal(new[] { "teamtype", "isdefault", "businessunitid" }, columns.Columns); return Teams[id]; }
                Assert.Equal(RegisterSchemaPlan.RecordTable, table); Assert.Equal(Id(5), id); Mark("root"); OnRootRead?.Invoke(); return Root;
            }
            public EntityCollection RetrieveMultiple(QueryBase value)
            {
                var query = Assert.IsType<QueryExpression>(value); Assert.False(query.ColumnSet.AllColumns);
                if (query.EntityName == CallerResolver.PersonTable)
                { Mark("person"); return new EntityCollection(new[] { new Entity(CallerResolver.PersonTable, Id(10))
                    { [CallerResolver.DirectoryColumn] = Id(3).ToString("D"), ["statecode"] = new OptionSetValue(0) } }); }
                if (query.EntityName == RegisterSchemaPlan.DecisionTable) { Mark("query-decision"); return Decisions; }
                Assert.Equal(DecisionEventSchema.Table, query.EntityName); Mark("query-event"); return Events;
            }
            public Guid Create(Entity row)
            {
                Mark(row.LogicalName == RegisterSchemaPlan.DecisionTable ? "create-decision" : "create-event"); Created.Add(row);
                (row.LogicalName == RegisterSchemaPlan.DecisionTable ? Decisions : Events).Entities.Add(row); return row.Id;
            }
            public void Update(Entity row) => throw new NotSupportedException("No blind update");
            public void Delete(string table, Guid id) => throw new NotSupportedException("No compensation");
            public void Associate(string table, Guid id, Relationship rel, EntityReferenceCollection rows) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship rel, EntityReferenceCollection rows) => throw new NotSupportedException();
        }
        private sealed class Fixture
        {
            internal readonly DateTimeOffset At = DateTimeOffset.UtcNow;
            internal readonly Context Context = new Context();
            internal readonly Store Store = new Store();
            internal readonly Profile Profile;
            internal Release Release;
            internal AuthorityRequest Authority;
            internal Entity Payload;
            internal DecisionAppend Intent;
            internal HumanDecisionConfirmation Confirmation;
            internal Fixture()
            {
                Profile = new Profile(Id(1), Id(11), 1, Id(12), "synthetic-instrument", new string('a', 64), At.AddDays(-1), At.AddDays(1),
                    new[] { new Grant("synthetic-grant", "scope-approve", "synthetic-position", new[] { "medical" }, new[] { "synthetic-site" },
                        new[] { "renewal" }, new[] { "peer-review" }, true) });
                Release = new Release(DelegationGate.ContentHash(Profile), Id(13), "synthetic-release", At.AddHours(-1), At.AddDays(1), true, false);
                Authority = Request();
                Payload = new Entity(RegisterSchemaPlan.DecisionTable, Id(6))
                {
                    ["dft_actorref"] = Id(10).ToString("D"), ["dft_function"] = "scope-approve", ["dft_profileversionref"] = "synthetic-release",
                    ["dft_decisionevidenceref"] = "synthetic-confirmation", ["dft_authorityevidenceref"] = "synthetic-occupancy",
                    ["dft_scopeversionref"] = "synthetic-scope", ["dft_profilehash"] = Release.Hash, ["dft_casehash"] = new string('b', 64),
                    ["dft_scopetext"] = "Synthetic scope only", ["dft_conditionstext"] = "Synthetic restriction", ["dft_supervisiontext"] = "",
                    ["dft_validfrom"] = At.AddDays(-1).UtcDateTime, ["dft_validuntil"] = At.AddDays(1).UtcDateTime,
                    ["dft_effectiveat"] = At.AddMinutes(-1).UtcDateTime, ["dft_recordedat"] = At.AddMinutes(-1).UtcDateTime,
                    ["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 1)
                };
                Intent = Owned(); Confirmation = Confirm();
            }
            internal AuthorityRequest Request(Guid? org = null, Guid? actor = null, Guid? practitioner = null, string function = "scope-approve",
                string pathway = "renewal", bool? conflict = false, string role = "synthetic-position") => new AuthorityRequest(org ?? Id(1), actor ?? Id(10),
                    practitioner ?? Id(4), role, "synthetic-occupancy", At.AddHours(-1), At.AddDays(1), function, "medical", "synthetic-site", pathway,
                    new[] { "peer-review" }, conflict, false);
            internal DecisionAppend Owned(Guid? eventOwner = null) => new DecisionAppend(Payload, Id(5), "9", 0, null, Id(7), eventOwner ?? Id(8));
            internal HumanDecisionConfirmation Confirm(Guid? org = null, Guid? actor = null, string? digest = null, int state = 602170001,
                string reference = "synthetic-confirmation", string function = "scope-approve", string pathway = "renewal",
                DateTimeOffset? confirmed = null, DateTimeOffset? until = null, bool withdrawn = false) => new HumanDecisionConfirmation(org ?? Id(1), actor ?? Id(10),
                    reference, digest ?? DecisionConfirmationHash.Compute(Id(1), Intent), state, function, pathway,
                    confirmed ?? At.AddMinutes(-2), until ?? At.AddHours(1), withdrawn);
            internal JournalAppendDisposition Run() => HumanConfirmedJournal.Stage(Context.Value, Store, Intent, Profile, Release, Authority, Confirmation);
            internal void Denied()
            { var ex = Assert.Throws<InvalidPluginExecutionException>(() => Run()); Assert.Equal(Failure, ex.Message); Assert.Null(ex.InnerException); }
        }

        [Fact]
        public void OneCallerBindingConnectsRealAuthorityChecksAndTheExistingJournalWithoutReassignment()
        {
            var f = new Fixture(); Assert.Equal(JournalAppendDisposition.StagedInTransaction, f.Run());
            Assert.Equal(new Guid?[] { Id(2) }, f.Store.RequestedUsers);
            Assert.Equal(1, f.Store.Calls.Count(x => x == "person")); Assert.Equal(2, f.Store.Created.Count);
            Assert.Equal(Id(10).ToString("D"), f.Store.Created[0]["dft_actorref"]);
            Assert.Equal(new EntityReference("team", Id(7)), f.Store.Created[0]["ownerid"]);
            Assert.Equal(new EntityReference("team", Id(8)), f.Store.Created[1]["ownerid"]);
            Assert.Equal(1, f.Store.Updates);
            Assert.Equal(JournalAppendDisposition.AlreadyRecorded, f.Run());
            Assert.Equal(2, f.Store.Created.Count); Assert.Equal(1, f.Store.Updates);
        }
        [Fact]
        public void TheCallerServiceReturnedByBindingIsExactlyTheOneUsedForIdentityResolution()
        {
            var f = new Fixture(); CallerBinding binding = CallerResolver.ResolveBinding(f.Context.Value, f.Store);
            Assert.Same(f.Store, binding.Service); Assert.Equal(Id(10), binding.Caller.Person);
            Assert.Equal(Id(2), binding.Caller.PlatformUser); Assert.Equal(Id(3), binding.Caller.DirectoryObject);
            Assert.Equal(Id(1), binding.Caller.Organization); Assert.Single(f.Store.RequestedUsers);
        }
        [Fact]
        public void MissingRequiredComponentsCannotStartAnyServiceCall()
        {
            var f = new Fixture();
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(null!, f.Store, f.Intent, f.Profile, f.Release, f.Authority, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, null!, f.Intent, f.Profile, f.Release, f.Authority, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, null!, f.Profile, f.Release, f.Authority, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, f.Intent, null!, f.Release, f.Authority, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, f.Intent, f.Profile, null!, f.Authority, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, f.Intent, f.Profile, f.Release, null!, f.Confirmation));
            Assert.Throws<ArgumentNullException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, f.Intent, f.Profile, f.Release, f.Authority, null!));
            Assert.Empty(f.Store.Calls);
        }
        [Theory]
        [InlineData("MessageName", "Other")] [InlineData("Stage", 20)] [InlineData("Mode", 1)]
        [InlineData("IsInTransaction", false)] [InlineData("Depth", 2)]
        [InlineData("IsExecutingOffline", true)] [InlineData("IsOfflinePlayback", true)]
        public void UnsupportedPipelineNeverStartsTheWrite(string field, object value)
        { var f = new Fixture(); f.Context.Fields[field] = value; f.Denied(); Assert.Empty(f.Store.Calls); }
        [Fact]
        public void ParentOrImpersonatedContextAndUnownedFixtureAreNotAccepted()
        {
            var f = new Fixture(); f.Context.Fields["ParentContext"] = f.Context.Value; f.Denied(); Assert.Empty(f.Store.Calls);
            f = new Fixture(); f.Context.Fields["UserId"] = Id(99); f.Denied(); Assert.Empty(f.Store.Calls);
            f = new Fixture(); f.Intent = new DecisionAppend(f.Payload, Id(5), "9", 0, null); f.Denied(); Assert.Empty(f.Store.Calls);
        }
        [Fact]
        public void TargetMustBeTheExactSavedCanonicalRegisterRecord()
        {
            object?[] wrong = { null, new Entity(RegisterSchemaPlan.RecordTable, Id(5)), new EntityReference("other", Id(5)),
                new EntityReference(RegisterSchemaPlan.RecordTable, Id(99)), new EntityReference(RegisterSchemaPlan.RecordTable, Id(5)) { KeyAttributes = { ["x"] = "alias" } } };
            foreach (object? value in wrong)
            { var f = new Fixture(); ((ParameterCollection)f.Context.Fields["InputParameters"]!)["Target"] = value!; f.Denied(); Assert.Empty(f.Store.Calls); }
            var missing = new Fixture(); ((ParameterCollection)missing.Context.Fields["InputParameters"]!).Clear(); missing.Denied();
            missing = new Fixture(); missing.Context.Fields["InputParameters"] = null; missing.Denied();
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void EveryOrganizationAndCanonicalPersonBindingMustAgree(int change)
        {
            var f = new Fixture(); Profile profile = f.Profile;
            if (change == 0) profile = new Profile(Id(99), profile.Id, 1, profile.Editor, profile.Instrument, profile.SourceHash, profile.From, profile.Until, profile.Grants);
            if (change == 1) f.Authority = f.Request(org: Id(99));
            if (change == 2) f.Confirmation = f.Confirm(org: Id(99));
            if (change == 3) f.Authority = f.Request(actor: Id(2));
            if (change == 4) f.Confirmation = f.Confirm(actor: Id(2));
            Assert.Throws<InvalidPluginExecutionException>(() => HumanConfirmedJournal.Stage(f.Context.Value, f.Store, f.Intent, profile, f.Release, f.Authority, f.Confirmation));
            Assert.Empty(f.Store.Created);
        }
        [Theory]
        [InlineData("dft_actorref")] [InlineData("dft_profilehash")] [InlineData("dft_profileversionref")]
        [InlineData("dft_function")] [InlineData("dft_authorityevidenceref")] [InlineData("dft_decisionevidenceref")]
        public void RehashingAlteredSourceOrActorBindingsDoesNotAuthoriseThem(string field)
        {
            var f = new Fixture(); f.Payload[field] = field == "dft_profilehash" ? new string('c', 64) : "different";
            f.Intent = f.Owned(); f.Confirmation = f.Confirm(); f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void OutcomeFunctionPathwayAndExactHumanDigestAreMandatory(int change)
        {
            var f = new Fixture();
            if (change == 0) f.Authority = f.Request(function: "different");
            if (change == 1) f.Authority = f.Request(pathway: "different");
            if (change == 2) f.Confirmation = f.Confirm(state: 602170003);
            if (change == 3) f.Confirmation = f.Confirm(digest: new string('c', 64));
            f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Fact]
        public void ApprovedContentCannotBeChangedAfterHumanConfirmationEvenWithTheSameCaseHash()
        {
            var f = new Fixture(); f.Payload["dft_conditionstext"] = ""; f.Intent = f.Owned();
            f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Fact]
        public void NativeRootIdentityCannotBeSwappedOrInferredFromMissingFields()
        {
            foreach (string field in new[] { "dft_practitionerref", "dft_professionref", "dft_facilityref" })
            {
                foreach (object? value in new object?[] { null, 1, "different" })
                { var f = new Fixture(); f.Store.Root[field] = value!; f.Denied(); Assert.Empty(f.Store.Created); }
                var missing = new Fixture(); missing.Store.Root.Attributes.Remove(field); missing.Denied();
            }
            for (int change = 0; change < 4; change++)
            {
                var f = new Fixture();
                if (change == 0) f.Store.Root = null!;
                if (change == 1) f.Store.Root.LogicalName = "other";
                if (change == 2) f.Store.Root.Id = Id(99);
                if (change == 3) f.Store.Root.KeyAttributes.Add("id", "unresolved");
                f.Denied(); Assert.Empty(f.Store.Created);
            }
        }
        [Theory]
        [InlineData(0)] [InlineData(2)] [InlineData(3)]
        public void OwnerCapableTeamsAreResolvedNativelyAndSameOwnerDoesNotRequireDuplicateReads(int teamType)
        {
            var f = new Fixture(); f.Store.Teams[Id(7)]["teamtype"] = new OptionSetValue(teamType);
            f.Intent = f.Owned(eventOwner: Id(7)); f.Confirmation = f.Confirm();
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, f.Run()); Assert.Equal(1, f.Store.Calls.Count(x => x == "team"));
        }
        [Fact]
        public void MissingAccessOrUnknownTeamKindsCannotBecomeOwners()
        {
            foreach (object? value in new object?[] { null, 0, new OptionSetValue(1), new OptionSetValue(4) })
            { var f = new Fixture(); f.Store.Teams[Id(7)]["teamtype"] = value!; f.Denied(); Assert.Empty(f.Store.Created); }
            var missing = new Fixture(); missing.Store.Teams[Id(7)].Attributes.Remove("teamtype"); missing.Denied();
            foreach (object? value in new object?[] { null, "false", true })
            { var f = new Fixture(); f.Store.Teams[Id(7)]["isdefault"] = value!; f.Denied(); }
            missing = new Fixture(); missing.Store.Teams[Id(7)].Attributes.Remove("isdefault"); missing.Denied();
        }
        [Fact]
        public void OwnerBusinessUnitMustBeCanonicalAndExplicit()
        {
            object?[] wrong = { null, "unit", new EntityReference("other", Id(9)), new EntityReference("businessunit", Guid.Empty),
                new EntityReference("businessunit", Id(9)) { KeyAttributes = { ["name"] = "alias" } } };
            foreach (object? value in wrong) { var f = new Fixture(); f.Store.Teams[Id(7)]["businessunitid"] = value!; f.Denied(); Assert.Empty(f.Store.Created); }
            var missing = new Fixture(); missing.Store.Teams[Id(7)].Attributes.Remove("businessunitid"); missing.Denied();
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
        public void ConfirmationMustBeCurrentAndPrecedeTheOriginalRecordingInstant(int change)
        {
            var f = new Fixture();
            if (change == 0) f.Confirmation = f.Confirm(withdrawn: true);
            if (change == 1) f.Confirmation = f.Confirm(confirmed: f.At.AddDays(1), until: f.At.AddDays(2));
            if (change == 2) f.Confirmation = f.Confirm(until: f.At.AddMinutes(-1));
            if (change == 3) { f.Payload["dft_recordedat"] = f.At.AddMinutes(-3).UtcDateTime; f.Intent = f.Owned(); f.Confirmation = f.Confirm(); }
            if (change == 4) { f.Payload["dft_recordedat"] = f.At.AddDays(1).UtcDateTime; f.Intent = f.Owned(); f.Confirmation = f.Confirm(); }
            f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Fact]
        public void ActualDelegationKernelDeniesWithdrawnReleaseConflictsSelfDecisionOrWrongRole()
        {
            var f = new Fixture(); f.Release = new Release(f.Release.Hash, Id(13), f.Release.Reference, f.At.AddHours(-1), f.At.AddDays(1), true, true);
            f.Denied(); Assert.Empty(f.Store.Created);
            f = new Fixture(); f.Authority = f.Request(conflict: null); f.Denied(); Assert.Empty(f.Store.Created);
            f = new Fixture(); f.Authority = f.Request(practitioner: Id(10)); f.Store.Root["dft_practitionerref"] = Id(10).ToString("D");
            f.Denied(); Assert.Empty(f.Store.Created);
            f = new Fixture(); f.Authority = f.Request(role: "different"); f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Fact]
        public void MissingServiceAndApplicationPrincipalsCannotUseTheHumanWriteCore()
        {
            var f = new Fixture(); f.Store.MissingService = true; f.Denied(); Assert.Empty(f.Store.Created);
            f = new Fixture(); f.Store.User["applicationid"] = Id(99); f.Denied(); Assert.Empty(f.Store.Created);
        }
        [Theory]
        [InlineData("whoami")] [InlineData("user")] [InlineData("key")] [InlineData("person")]
        [InlineData("root")] [InlineData("team")] [InlineData("query-decision")]
        [InlineData("create-decision")] [InlineData("root-cas")] [InlineData("create-event")]
        public void ErrorsEscapeForNativeRollbackWithoutRetryCompensationOrPrivateFaultDetails(string phase)
        {
            var f = new Fixture(); f.Store.FailAt = phase; f.Denied(); Assert.Equal(phase, f.Store.Calls.Last());
            Assert.Equal(1, f.Store.Calls.Count(x => x == phase));
        }
        [Fact]
        public void MutationOfIncomingTargetAfterResolutionCannotRedirectTheWrite()
        {
            var f = new Fixture();
            f.Store.OnRootRead = () => ((EntityReference)((ParameterCollection)f.Context.Fields["InputParameters"]!)["Target"]).Id = Id(99);
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, f.Run());
            Assert.All(f.Store.Created, row => Assert.Equal(Id(5), ((EntityReference)row["dft_record"]).Id));
        }
        [Fact]
        public void AWithdrawnConfirmationDoesNotResubmitOrAlterAnAlreadyRecordedDecision()
        {
            var f = new Fixture(); f.Run(); f.Confirmation = f.Confirm(withdrawn: true); f.Denied();
            Assert.Equal(2, f.Store.Created.Count); Assert.Equal(1, f.Store.Updates);
        }
        [Fact]
        public void ConfirmationConstructorRejectsUnboundAndMalformedHostSnapshots()
        {
            var f = new Fixture();
            Assert.Throws<ArgumentException>(() => f.Confirm(org: Guid.Empty)); Assert.Throws<ArgumentException>(() => f.Confirm(actor: Guid.Empty));
            Assert.Throws<ArgumentException>(() => f.Confirm(confirmed: f.At, until: f.At));
            foreach (string? digest in new string?[] { null, "", new string('A', 64), new string('g', 64), new string('a', 65) })
                Assert.Throws<ArgumentException>(() => new HumanDecisionConfirmation(Id(1), Id(10), "r", digest!, 602170001, "f", "p", f.At, f.At.AddDays(1), false));
            Assert.Throws<ArgumentException>(() => f.Confirm(state: 602170000)); Assert.Throws<ArgumentException>(() => f.Confirm(state: 602170005));
            foreach (string? text in new string?[] { null, "", " ", " x", "x ", "a\nb", new string('x', 257), "\uD800" })
            {
                Assert.ThrowsAny<ArgumentException>(() => f.Confirm(reference: text!));
                Assert.ThrowsAny<ArgumentException>(() => f.Confirm(function: text!));
                Assert.ThrowsAny<ArgumentException>(() => f.Confirm(pathway: text!));
            }
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void AuthorityMustCoverBothTheHumanActionAndTheCurrentRecordingAttempt(int change)
        {
            var f = new Fixture(); Profile profile = f.Profile;
            if (change == 0)
                f.Release = new Release(f.Release.Hash, Id(13), f.Release.Reference, f.At.AddMinutes(-1), f.At.AddDays(1), true, false);
            if (change == 1)
                f.Authority = new AuthorityRequest(Id(1), Id(10), Id(4), "synthetic-position", "synthetic-occupancy",
                    f.At.AddMinutes(-1), f.At.AddDays(1), "scope-approve", "medical", "synthetic-site", "renewal", new[] { "peer-review" }, false, false);
            if (change == 2)
            {
                profile = new Profile(profile.Organization, profile.Id, profile.Revision, profile.Editor, profile.Instrument,
                    profile.SourceHash, f.At.AddMinutes(-1), profile.Until, profile.Grants);
                f.Release = new Release(DelegationGate.ContentHash(profile), Id(13), f.Release.Reference, f.At.AddHours(-1), f.At.AddDays(1), true, false);
                f.Payload["dft_profilehash"] = f.Release.Hash; f.Intent = f.Owned(); f.Confirmation = f.Confirm();
            }
            if (change == 3)
                f.Release = new Release(f.Release.Hash, Id(13), f.Release.Reference, f.At.AddHours(-1), f.At.AddSeconds(-30), true, false);
            var error = Assert.Throws<InvalidPluginExecutionException>(() => HumanConfirmedJournal.Stage(
                f.Context.Value, f.Store, f.Intent, profile, f.Release, f.Authority, f.Confirmation));
            Assert.Equal(Failure, error.Message); Assert.Empty(f.Store.Created); Assert.Equal(0, f.Store.Updates);
        }
    }
}
