using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class OwnedDecisionTests
    {
        private static readonly Guid Organization = Guid.Parse("40000000-0000-0000-0000-000000000004");
        private static readonly Guid User = Guid.Parse("50000000-0000-0000-0000-000000000005");
        private static readonly Guid Owner = Guid.Parse("60000000-0000-0000-0000-000000000006");
        private static readonly Guid EventOwner = Guid.Parse("70000000-0000-0000-0000-000000000007");
        private static DecisionAppend Owned(Entity? payload = null, Guid? record = null, string version = "9", int sequence = 0,
            Guid? previous = null, Guid? owner = null, Guid? eventOwner = null) => new DecisionAppend(payload ?? DecisionAppendTests.Payload(),
                record ?? DecisionAppendTests.Record, version, sequence, previous, owner ?? Owner, eventOwner ?? EventOwner);
        private static string Digest(DecisionAppend intent) => DecisionConfirmationHash.Compute(Organization, intent);

        [Fact]
        public void OwnersAreExplicitIndependentAndFrozenBeforeAnyCreate()
        {
            DecisionAppend intent = Owned();
            Assert.Equal(Owner, intent.DecisionOwnerId); Assert.Equal(EventOwner, intent.EventOwnerId);
            Assert.Equal(new EntityReference("team", Owner), intent.ToEntity()["ownerid"]);
            Assert.Contains("ownerid", intent.Columns);
            Entity copy = intent.ToEntity(); ((EntityReference)copy["ownerid"]).Id = User;
            Assert.False(intent.Matches(copy)); Assert.Equal(Owner, ((EntityReference)intent.ToEntity()["ownerid"]).Id);
            Assert.Equal(Owner, Owned(eventOwner: Owner).EventOwnerId);
        }
        [Fact]
        public void UnspecifiedOwnersAreNotAnOperationalFallback()
        {
            Assert.Throws<ArgumentException>(() => Owned(owner: Guid.Empty));
            Assert.Throws<ArgumentException>(() => Owned(eventOwner: Guid.Empty));
            DecisionAppend original = DecisionAppendTests.Intent();
            Assert.Null(original.DecisionOwnerId); Assert.Null(original.EventOwnerId);
            Assert.Throws<ArgumentException>(() => Digest(original));
            Assert.Throws<ArgumentException>(() => DecisionConfirmationHash.Compute(Guid.Empty, Owned()));
            Assert.Throws<ArgumentNullException>(() => DecisionConfirmationHash.Compute(Organization, null!));
        }
        [Fact]
        public void ConfirmationDigestMatchesIndependent911ByteEncodingVector()
        {
            Assert.Equal("careops-decision-confirmation-v1", DecisionConfirmationHash.Version);
            Assert.Equal("4ec6ae54b5b62f30e3ee44f0d81122eb9b574734782c300983bd592ccd00895e", Digest(Owned()));
        }
        [Fact]
        public void EveryBusinessFieldChangesTheConfirmationDigest()
        {
            string original = Digest(Owned());
            foreach (string field in DecisionAppendTests.Payload().Attributes.Keys)
            {
                Entity payload = DecisionAppendTests.Payload(); object value = payload[field];
                if (field.EndsWith("hash", StringComparison.Ordinal)) payload[field] = new string('c', 64);
                else if (value is string text) payload[field] = text + "changed";
                else if (value is DateTime date) payload[field] = date.AddTicks(1);
                else payload[field] = new OptionSetValue(RegisterSchemaPlan.Unverified + 3);
                Assert.NotEqual(original, Digest(Owned(payload)));
            }
        }
        [Fact]
        public void BindingIncludesEnvironmentTargetVersionSequenceAndBothOwners()
        {
            string original = Digest(Owned());
            Assert.NotEqual(original, DecisionConfirmationHash.Compute(User, Owned()));
            Assert.NotEqual(original, Digest(Owned(record: User)));
            Assert.NotEqual(original, Digest(Owned(version: "10")));
            Assert.NotEqual(original, Digest(Owned(owner: User)));
            Assert.NotEqual(original, Digest(Owned(eventOwner: User)));
            Entity payload = DecisionAppendTests.Payload(); payload.Id = User;
            Assert.NotEqual(original, Digest(Owned(payload)));
            DecisionAppend later = Owned(sequence: 1, previous: DecisionAppendTests.Previous);
            Assert.NotEqual(original, Digest(later));
            Assert.NotEqual(Digest(later), Digest(Owned(sequence: 2, previous: DecisionAppendTests.Previous)));
            Assert.NotEqual(Digest(later), Digest(Owned(sequence: 1, previous: User)));
        }
        [Fact]
        public void OrderingCultureAndMutableTransportDoNotChangeTheFrozenDigest()
        {
            Entity payload = DecisionAppendTests.Payload(); DecisionAppend intent = Owned(payload); string hash = Digest(intent);
            var reverse = new Entity(payload.LogicalName, payload.Id);
            foreach (var pair in payload.Attributes.Reverse()) reverse[pair.Key] = pair.Value;
            Assert.Equal(hash, Digest(Owned(reverse)));
            payload["dft_scopetext"] = "changed"; intent.ToEntity()["dft_scopetext"] = "also changed";
            CultureInfo saved = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR"); Assert.Equal(hash, Digest(intent)); }
            finally { CultureInfo.CurrentCulture = saved; }
        }
        [Fact]
        public void CaseOnlyHashCannotConfirmChangedRestrictionsOrOwnership()
        {
            Entity a = DecisionAppendTests.Payload(), b = DecisionAppendTests.Payload(); b["dft_conditionstext"] = "Supervised only";
            Assert.Equal(a["dft_casehash"], b["dft_casehash"]);
            Assert.NotEqual(Digest(Owned(a)), Digest(Owned(b)));
            Assert.NotEqual(Digest(Owned(a)), Digest(Owned(a, owner: EventOwner)));
        }
        [Fact]
        public void OwnedReceiptRejectsOmittedWrongOrAliasedOwner()
        {
            DecisionAppend intent = Owned();
            object?[] invalid = { null, "team", new EntityReference("systemuser", Owner), new EntityReference("team", User),
                new EntityReference("team", Owner) { KeyAttributes = { ["name"] = "alias" } } };
            foreach (object? value in invalid) { Entity row = intent.ToEntity(); row["ownerid"] = value!; Assert.False(intent.Matches(row)); }
            Entity missing = intent.ToEntity(); missing.Attributes.Remove("ownerid"); Assert.False(intent.Matches(missing));
            Assert.False(intent.Matches(DecisionAppendTests.Intent().ToEntity()));
            Assert.False(DecisionAppendTests.Intent().Matches(intent.ToEntity()));
        }

        private sealed class Context : RealProxy
        {
            internal Context() : base(typeof(IPluginExecutionContext)) { }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                var values = new Dictionary<string, object?> { ["MessageName"] = RegisterJournalAppender.ApiName, ["Stage"] = 30,
                    ["Mode"] = 0, ["Depth"] = 1, ["IsInTransaction"] = true, ["ParentContext"] = null,
                    ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false, ["OrganizationId"] = Organization,
                    ["InitiatingUserId"] = User, ["UserId"] = User };
                return new ReturnMessage(values[call.MethodName.Substring(4)], null, 0, call.LogicalCallContext, call);
            }
        }
        private sealed class Store : IOrganizationService
        {
            internal readonly EntityCollection Decisions = new EntityCollection(), Events = new EntityCollection();
            internal readonly List<Entity> Created = new List<Entity>();
            internal readonly List<QueryExpression> Queries = new List<QueryExpression>();
            internal int Updates;
            public OrganizationResponse Execute(OrganizationRequest request)
            {
                if (request is WhoAmIRequest)
                { var r = new WhoAmIResponse(); r.Results["OrganizationId"] = Organization; r.Results["UserId"] = User; return r; }
                var update = Assert.IsType<UpdateRequest>(request); Assert.Equal(ConcurrencyBehavior.IfRowVersionMatches, update.ConcurrencyBehavior);
                Assert.DoesNotContain("ownerid", update.Target.Attributes.Keys); Updates++; return new UpdateResponse();
            }
            public Entity Retrieve(string table, Guid id, ColumnSet columns)
            { Assert.Equal(RegisterSchemaPlan.RecordTable, table); return new Entity(table, id) { RowVersion = "9", ["dft_latestsequence"] = 0 }; }
            public EntityCollection RetrieveMultiple(QueryBase query)
            { var q = Assert.IsType<QueryExpression>(query); Queries.Add(q); return q.EntityName == RegisterSchemaPlan.DecisionTable ? Decisions : Events; }
            public Guid Create(Entity row)
            { Created.Add(row); (row.LogicalName == RegisterSchemaPlan.DecisionTable ? Decisions : Events).Entities.Add(row); return row.Id; }
            public void Update(Entity row) => throw new NotSupportedException();
            public void Delete(string table, Guid id) => throw new NotSupportedException();
            public void Associate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
            public void Disassociate(string table, Guid id, Relationship relationship, EntityReferenceCollection rows) => throw new NotSupportedException();
        }
        [Fact]
        public void ActualJournalAndEventStagingUseDifferentOwnersWithoutLaterAssignment()
        {
            var s = new Store(); DecisionAppend intent = Owned();
            Assert.Equal(JournalAppendDisposition.StagedInTransaction, JournalWithOutbox.Stage(new Context().Value, s, intent));
            Assert.Equal(2, s.Created.Count); Assert.Equal(new EntityReference("team", Owner), s.Created[0]["ownerid"]);
            Assert.Equal(new EntityReference("team", EventOwner), s.Created[1]["ownerid"]);
            Assert.Equal(JournalAppendDisposition.AlreadyRecorded, JournalWithOutbox.Stage(new Context().Value, s, intent));
            Assert.Equal(2, s.Created.Count); Assert.Equal(1, s.Updates);
            Assert.All(s.Queries, q => Assert.Contains("ownerid", q.ColumnSet.Columns));
            Assert.DoesNotContain("dft_scopetext", s.Created[1].Attributes.Keys);
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void HistoricalOwnerChangesAreNotSilentlyRepairedOrReassigned(bool eventRow)
        {
            var s = new Store(); DecisionAppend intent = Owned();
            JournalWithOutbox.Stage(new Context().Value, s, intent);
            (eventRow ? s.Events : s.Decisions).Entities[0]["ownerid"] = new EntityReference("team", User);
            Assert.Throws<InvalidPluginExecutionException>(() => JournalWithOutbox.Stage(new Context().Value, s, intent));
            Assert.Equal(2, s.Created.Count); Assert.Equal(1, s.Updates);
        }
    }
}
