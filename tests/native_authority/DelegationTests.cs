using System;
using System.Collections.Generic;
using System.Linq;
using CareOps.Decisions.Authority;
using Xunit;

namespace CareOps.Decisions.Authority.Tests
{
    public sealed class DelegationTests
    {
        private static readonly Guid Org = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid Editor = Guid.Parse("30000000-0000-0000-0000-000000000003");
        private static readonly Guid Approver = Guid.Parse("40000000-0000-0000-0000-000000000004");
        private static readonly Guid Actor = Guid.Parse("50000000-0000-0000-0000-000000000005");
        private static readonly Guid Practitioner = Guid.Parse("60000000-0000-0000-0000-000000000006");
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
        private static readonly string[] One = { "one" };
        private static readonly string[] Empty = Array.Empty<string>();
        private static Grant G(string id = "g1", string function = "approve", string role = "position-1",
            string[]? professions = null, string[]? facilities = null, string[]? pathways = null,
            string[]? conditions = null, bool independent = false) => new Grant(id, function, role,
                professions ?? One, facilities ?? One, pathways ?? One, conditions ?? Empty, independent);
        private static Profile P(Guid? org = null, Guid? id = null, int revision = 1, Guid? editor = null,
            string instrument = "source-1", string? sourceHash = null, DateTimeOffset? from = null,
            DateTimeOffset? until = null, Grant[]? grants = null) => new Profile(org ?? Org, id ?? Id,
                revision, editor ?? Editor, instrument, sourceHash ?? new string('a', 64),
                from ?? Now.AddDays(-1), until ?? Now.AddDays(10), grants ?? new[] { G() });
        private static Release R(Profile? p = null, string? hash = null, Guid? approver = null,
            DateTimeOffset? approved = null, DateTimeOffset? until = null, bool verified = true,
            bool withdrawn = false) => new Release(hash ?? DelegationGate.ContentHash(p ?? P()),
                approver ?? Approver, "approval-1", approved ?? Now.AddHours(-1), until ?? Now.AddHours(2), verified, withdrawn);
        private static Request Q(Guid? org = null, Guid? actor = null, Guid? practitioner = null,
            string role = "position-1", string function = "approve", string profession = "one",
            string facility = "one", string pathway = "one", string[]? conditions = null,
            bool? conflict = false, bool? committee = false, DateTimeOffset? from = null,
            DateTimeOffset? until = null) => new Request(org ?? Org, actor ?? Actor, practitioner ?? Practitioner,
                role, "occupancy-1", from ?? Now.AddHours(-2), until ?? Now.AddHours(1), function,
                profession, facility, pathway, conditions ?? Empty, conflict, committee);
        private static Outcome Check(Profile? p = null, Release? r = null, Request? q = null,
            DateTimeOffset? at = null) => DelegationGate.Check(p ?? P(), r ?? R(p), q ?? Q(), at ?? Now);

        [Fact]
        public void PermittedMeansOneFunctionNotACredentialingDecision()
        {
            Assert.Equal(Outcome.Permitted, Check());
            Assert.Equal("occupancy-1", Q().OccupancyReference);
            Assert.Equal("approval-1", R().Reference);
        }

        [Fact]
        public void OrganizationBoundaryCannotBeCrossed()
        {
            Assert.Equal(Outcome.WrongOrganization, Check(q: Q(org: Id)));
        }

        [Fact]
        public void WithdrawalUnknownSourceAndSelfApprovalDeny()
        {
            foreach (Release value in new[] { R(verified: false), R(withdrawn: true), R(approver: Editor) })
                Assert.Equal(Outcome.InvalidRelease, Check(r: value));
        }

        [Fact]
        public void ApprovalCurrentnessUsesInclusiveStartExclusiveEnd()
        {
            Assert.Equal(Outcome.InvalidRelease, Check(r: R(approved: Now.AddTicks(1))));
            Assert.Equal(Outcome.InvalidRelease, Check(r: R(until: Now)));
            Assert.Equal(Outcome.Permitted, Check(r: R(approved: Now, until: Now.AddTicks(1))));
        }

        [Fact]
        public void ProfileEffectiveTimeIsCheckedSeparatelyFromApproval()
        {
            foreach (Profile value in new[] { P(from: Now.AddTicks(1)), P(until: Now) })
                Assert.Equal(Outcome.ProfileNotEffective, Check(p: value));
            Assert.Equal(Outcome.Permitted, Check(p: P(from: Now, until: Now.AddTicks(1))));
        }

        [Fact]
        public void ActingOccupancyIsRecheckedAtEveryAction()
        {
            Assert.Equal(Outcome.OccupancyNotEffective, Check(q: Q(from: Now.AddTicks(1))));
            Assert.Equal(Outcome.OccupancyNotEffective, Check(q: Q(until: Now)));
            Assert.Equal(Outcome.Permitted, Check(q: Q(from: Now, until: Now.AddTicks(1))));
        }

        [Fact]
        public void SelfDecisionAndMissingConflictEvidenceDeny()
        {
            Assert.Equal(Outcome.Conflict, Check(q: Q(actor: Practitioner)));
            Assert.Equal(Outcome.Conflict, Check(q: Q(conflict: true)));
            Assert.Equal(Outcome.Conflict, Check(q: Q(conflict: null)));
        }

        [Fact]
        public void EveryScopeDimensionMustMatchExactly()
        {
            foreach (Request value in new[] { Q(role: "CMO"), Q(function: "revoke"), Q(profession: "two"),
                Q(facility: "two"), Q(pathway: "two"), Q(role: "POSITION-1") })
                Assert.Equal(Outcome.MissingOrAmbiguousGrant, Check(q: value));
            Assert.Equal(Outcome.MissingOrAmbiguousGrant, Check(p: P(grants: new[] { G(), G(id: "g2") })));
        }

        [Fact]
        public void IndependenceRequiresConfirmedNonparticipation()
        {
            Profile p = P(grants: new[] { G(independent: true) });
            Assert.Equal(Outcome.Permitted, Check(p: p));
            Assert.Equal(Outcome.IndependenceRequired, Check(p: p, q: Q(committee: true)));
            Assert.Equal(Outcome.IndependenceRequired, Check(p: p, q: Q(committee: null)));
            Assert.Equal(Outcome.Permitted, Check(q: Q(committee: null)));
        }

        [Fact]
        public void AllConditionsNeedEvidenceButAdditionalEvidenceIsAllowed()
        {
            Profile p = P(grants: new[] { G(conditions: new[] { "c1", "c2" }) });
            Assert.Equal(Outcome.ConditionsUnconfirmed, Check(p: p));
            Assert.Equal(Outcome.ConditionsUnconfirmed, Check(p: p, q: Q(conditions: new[] { "c1" })));
            Assert.Equal(Outcome.Permitted, Check(p: p, q: Q(conditions: new[] { "c1", "c2", "extra" })));
        }

        [Fact]
        public void EveryEditableFieldInvalidatesPreviousApproval()
        {
            var changed = new[] { P(org: Id), P(id: Org), P(revision: 2), P(editor: Actor),
                P(instrument: "source-2"), P(sourceHash: new string('b',64)),
                P(from: Now.AddDays(-2)), P(until: Now.AddDays(20)),
                P(grants: new[] { G(id: "g2") }), P(grants: new[] { G(function: "revoke") }),
                P(grants: new[] { G(role: "other") }), P(grants: new[] { G(professions: new[] { "two" }) }),
                P(grants: new[] { G(facilities: new[] { "two" }) }), P(grants: new[] { G(pathways: new[] { "two" }) }),
                P(grants: new[] { G(conditions: One) }), P(grants: new[] { G(independent: true) }),
                P(grants: new[] { G(), G(id: "g2", function: "revoke") }) };
            foreach (Profile value in changed)
            {
                Assert.NotEqual(DelegationGate.ContentHash(P()), DelegationGate.ContentHash(value));
                Assert.Equal(Outcome.InvalidRelease, Check(p: value, r: R(), q: Q(org: value.Organization)));
            }
        }

        [Fact]
        public void EquivalentOrderAndOffsetDoNotChangeTheDigest()
        {
            var a = new[] { "a", "b" }; var b = new[] { "b", "a" };
            Profile p1 = P(grants: new[] { G(professions: a, facilities: a, pathways: a, conditions: a), G(id: "g2") });
            Profile p2 = P(from: p1.From.ToOffset(TimeSpan.FromHours(10)), until: p1.Until.ToOffset(TimeSpan.FromHours(-5)),
                grants: new[] { G(id: "g2"), G(professions: b, facilities: b, pathways: b, conditions: b) });
            Assert.Equal(DelegationGate.ContentHash(p1), DelegationGate.ContentHash(p2));
            Assert.Equal(DelegationGate.ContentHash(P()), DelegationGate.ContentHash(P()));
        }

        [Fact]
        public void CollectionsAreCopiedAndCannotBeChangedThroughTheirViews()
        {
            var fields = new[] { "one" }; Grant grant = G(professions: fields); fields[0] = "two";
            Assert.Equal("one", grant.Professions[0]);
            var grants = new[] { grant }; Profile p = P(grants: grants); grants[0] = G(id: "other");
            Assert.Equal("g1", p.Grants[0].Id);
            Assert.Throws<NotSupportedException>(() => ((IList<Grant>)p.Grants)[0] = G());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)grant.Professions)[0] = "two");
        }

        [Fact]
        public void MissingArgumentsAreNotPermissions()
        {
            Assert.Throws<ArgumentNullException>(() => DelegationGate.ContentHash(null!));
            Assert.Throws<ArgumentNullException>(() => DelegationGate.Check(null!, R(), Q(), Now));
            Assert.Throws<ArgumentNullException>(() => DelegationGate.Check(P(), null!, Q(), Now));
            Assert.Throws<ArgumentNullException>(() => DelegationGate.Check(P(), R(), null!, Now));
        }

        [Fact]
        public void RejectsMalformedIdentifiers()
        {
            foreach (string value in new[] { null!, "", " ", " leading", "trailing ", "bad\nidentifier", new string('a',257), "\ud800" })
                Assert.ThrowsAny<ArgumentException>(() => G(id: value));
            Assert.Equal(new string('a',256), G(id: new string('a',256)).Id);
        }

        [Fact]
        public void RejectsMalformedHashes()
        {
            foreach (string value in new[] { "", "abc", new string('A',64), new string('g',64) })
            {
                Assert.Throws<ArgumentException>(() => P(sourceHash: value));
                Assert.Throws<ArgumentException>(() => R(hash: value));
            }
            Assert.Throws<ArgumentException>(() => new Release(null!, Approver, "r", Now, Now.AddDays(1), true, false));
        }

        [Fact]
        public void RejectsMalformedPeriodsAndIdentities()
        {
            foreach (Func<Profile> make in new Func<Profile>[] { () => P(org: Guid.Empty), () => P(id: Guid.Empty),
                () => P(editor: Guid.Empty), () => P(revision: 0), () => P(from: Now, until: Now),
                () => P(from: Now, until: Now.AddDays(-1)) }) Assert.Throws<ArgumentException>(make);
            Assert.Throws<ArgumentException>(() => R(approver: Guid.Empty));
            Assert.Throws<ArgumentException>(() => R(approved: Now, until: Now));
            foreach (Func<Request> make in new Func<Request>[] { () => Q(org: Guid.Empty), () => Q(actor: Guid.Empty),
                () => Q(practitioner: Guid.Empty), () => Q(from: Now, until: Now) }) Assert.Throws<ArgumentException>(make);
        }

        [Fact]
        public void GrantSetsMustBeBoundedUniqueAndRequiredWhereAppropriate()
        {
            Assert.Throws<ArgumentNullException>(() => new Grant("g","f","r",null!,One,One,Empty,false));
            Assert.Throws<ArgumentException>(() => G(professions: Empty));
            Assert.Throws<ArgumentException>(() => G(professions: new[] { "one", "one" }));
            Assert.Throws<ArgumentException>(() => G(conditions: Enumerable.Repeat("x",129).ToArray()));
            Assert.Equal(128, G(professions: Enumerable.Range(0,128).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray()).Professions.Count);
            Assert.Throws<ArgumentNullException>(() => new Profile(Org,Id,1,Editor,"s",new string('a',64),Now,Now.AddDays(1),null!));
            Assert.Throws<ArgumentException>(() => P(grants: Array.Empty<Grant>()));
            Assert.Throws<ArgumentException>(() => P(grants: Enumerable.Repeat(G(),129).ToArray()));
            Assert.Throws<ArgumentException>(() => P(grants: new[] { G(), G() }));
            Assert.Throws<ArgumentException>(() => P(grants: new Grant[] { null! }));
        }
    }
}
