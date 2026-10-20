using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CareOps.Decisions.Authority;
using Xunit;

namespace CareOps.Decisions.Authority.Tests
{
    public sealed class ProfileChangesTests
    {
        private static readonly Guid Org = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid Editor = Guid.Parse("30000000-0000-0000-0000-000000000003");
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
        private static readonly string[] One = { "one" };
        private static Grant G(string id = "g1", string function = "approve", string role = "position-1",
            string[]? professions = null, string[]? facilities = null, string[]? pathways = null,
            string[]? conditions = null, bool independent = false) => new Grant(id, function, role,
                professions ?? One, facilities ?? One, pathways ?? One, conditions ?? Array.Empty<string>(), independent);
        private static Profile P(int revision = 1, Guid? editor = null, string instrument = "source-1",
            string? hash = null, DateTimeOffset? from = null, DateTimeOffset? until = null,
            Grant[]? grants = null, Guid? org = null, Guid? id = null) => new Profile(org ?? Org, id ?? Id,
                revision, editor ?? Editor, instrument, hash ?? new string('a', 64), from ?? Start,
                until ?? Start.AddDays(10), grants ?? new[] { G() });

        [Fact]
        public void UnchangedProfileHasNoWorkToReview()
        {
            Profile p = P(); ProfileComparison result = ProfileChanges.Compare(p, p);
            Assert.Empty(result.Changes);
            Assert.Equal(DelegationGate.ContentHash(p), result.BeforeHash);
            Assert.Equal(result.BeforeHash, result.AfterHash);
        }

        [Fact]
        public void EveryEditableProfileFieldHasAnExactBeforeAndAfter()
        {
            Profile before = P();
            Profile after = P(2, Org, "source-2", new string('b', 64), Start.AddDays(-1), Start.AddDays(20));
            ProfileComparison result = ProfileChanges.Compare(before, after);
            Assert.Equal(new[] { "editor", "from", "instrument", "revision", "sourceHash", "until" }, result.Changes.Select(x => x.Field));
            Assert.All(result.Changes, change => Assert.Empty(change.GrantId));
            var rows = result.Changes.ToDictionary(x => x.Field);
            Assert.Equal(new[] { "1" }, rows["revision"].Before);
            Assert.Equal(new[] { "2" }, rows["revision"].After);
            Assert.Equal(new[] { Editor.ToString("D") }, rows["editor"].Before);
            Assert.Equal(new[] { Org.ToString("D") }, rows["editor"].After);
            Assert.Equal(new[] { "source-1" }, rows["instrument"].Before);
            Assert.Equal(new[] { "source-2" }, rows["instrument"].After);
            Assert.Equal(new[] { new string('a', 64) }, rows["sourceHash"].Before);
            Assert.Equal(new[] { new string('b', 64) }, rows["sourceHash"].After);
            Assert.Equal(new[] { "2026-09-24T00:00:00.0000000+00:00" }, rows["from"].Before);
            Assert.Equal(new[] { "2026-09-23T00:00:00.0000000+00:00" }, rows["from"].After);
            Assert.Equal(new[] { "2026-10-04T00:00:00.0000000+00:00" }, rows["until"].Before);
            Assert.Equal(new[] { "2026-10-14T00:00:00.0000000+00:00" }, rows["until"].After);
            Assert.Equal(DelegationGate.ContentHash(before), result.BeforeHash);
            Assert.Equal(DelegationGate.ContentHash(after), result.AfterHash);
        }

        [Fact]
        public void EveryGrantFieldIncludingRemovedConditionsIsShown()
        {
            Profile before = P(grants: new[] { G(conditions: new[] { "review-required" }) });
            Profile after = P(grants: new[] { G(function: "revoke", role: "other", professions: new[] { "two" },
                facilities: new[] { "site-2" }, pathways: new[] { "temporary" }, independent: true) });
            ProfileComparison result = ProfileChanges.Compare(before, after);
            Assert.Equal(new[] { "conditions", "facilities", "function", "independentOfCommittee", "pathways", "professions", "role" }, result.Changes.Select(x => x.Field));
            Assert.All(result.Changes, change => Assert.Equal("g1", change.GrantId));
            var rows = result.Changes.ToDictionary(x => x.Field);
            Assert.Equal(new[] { "review-required" }, rows["conditions"].Before);
            Assert.NotNull(rows["conditions"].After);
            Assert.Empty(rows["conditions"].After!);
            Assert.Equal(new[] { "one" }, rows["facilities"].Before);
            Assert.Equal(new[] { "site-2" }, rows["facilities"].After);
            Assert.Equal(new[] { "approve" }, rows["function"].Before);
            Assert.Equal(new[] { "revoke" }, rows["function"].After);
            Assert.Equal(new[] { "false" }, rows["independentOfCommittee"].Before);
            Assert.Equal(new[] { "true" }, rows["independentOfCommittee"].After);
            Assert.Equal(new[] { "one" }, rows["pathways"].Before);
            Assert.Equal(new[] { "temporary" }, rows["pathways"].After);
            Assert.Equal(new[] { "one" }, rows["professions"].Before);
            Assert.Equal(new[] { "two" }, rows["professions"].After);
            Assert.Equal(new[] { "position-1" }, rows["role"].Before);
            Assert.Equal(new[] { "other" }, rows["role"].After);
        }

        [Fact]
        public void AddedAndRemovedGrantsIncludeAllFieldsAndDistinguishAbsentFromEmpty()
        {
            ProfileComparison result = ProfileChanges.Compare(P(), P(grants: new[] { G(id: "g2") }));
            Assert.Equal(14, result.Changes.Count);
            Assert.Equal(Enumerable.Repeat("g1", 7).Concat(Enumerable.Repeat("g2", 7)), result.Changes.Select(x => x.GrantId));
            Assert.All(result.Changes.Take(7), row => { Assert.NotNull(row.Before); Assert.Null(row.After); });
            Assert.All(result.Changes.Skip(7), row => { Assert.Null(row.Before); Assert.NotNull(row.After); });
            Assert.Empty(result.Changes.First(x => x.GrantId == "g1" && x.Field == "conditions").Before!);
            Assert.Empty(result.Changes.First(x => x.GrantId == "g2" && x.Field == "conditions").After!);
        }

        [Fact]
        public void SetReorderingAndEquivalentOffsetsCreateNoSpuriousWork()
        {
            string[] a = { "a", "b" }; string[] b = { "b", "a" };
            Profile before = P(grants: new[] { G(professions: a, facilities: a, pathways: a, conditions: a), G(id: "g2") });
            Profile after = P(from: Start.ToOffset(TimeSpan.FromHours(10)), until: Start.AddDays(10).ToOffset(TimeSpan.FromHours(-5)),
                grants: new[] { G(id: "g2"), G(professions: b, facilities: b, pathways: b, conditions: b) });
            ProfileComparison result = ProfileChanges.Compare(before, after);
            Assert.Empty(result.Changes);
            Assert.Equal(result.BeforeHash, result.AfterHash);
        }

        [Fact]
        public void NewRevisionAloneIsVisibleAndNotAutomaticallyApproved()
        {
            ProfileComparison result = ProfileChanges.Compare(P(), P(revision: 2));
            Assert.Equal("revision", Assert.Single(result.Changes).Field);
            Assert.NotEqual(result.BeforeHash, result.AfterHash);
        }

        [Fact]
        public void ComparisonMayDescribeRollbackButNeverReleasesIt()
        {
            ProfileComparison result = ProfileChanges.Compare(P(revision: 2), P());
            Assert.Equal(new[] { "2" }, Assert.Single(result.Changes).Before);
            Assert.Equal(new[] { "1" }, result.Changes[0].After);
        }

        [Fact]
        public void UnrelatedProfilesAndMissingInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ProfileChanges.Compare(null!, P()));
            Assert.Throws<ArgumentNullException>(() => ProfileChanges.Compare(P(), null!));
            Assert.Throws<ArgumentException>(() => ProfileChanges.Compare(P(), P(org: Id)));
            Assert.Throws<ArgumentException>(() => ProfileChanges.Compare(P(), P(id: Org)));
        }

        [Fact]
        public void ReturnedChangesCannotBeMutated()
        {
            string[] values = { "new" }; Profile before = P(); Profile after = P(grants: new[] { G(conditions: values) });
            ProfileComparison result = ProfileChanges.Compare(before, after); values[0] = "different";
            ProfileChange row = Assert.Single(result.Changes);
            Assert.Equal(new[] { "new" }, row.After);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)row.After!)[0] = "changed");
            Assert.Throws<NotSupportedException>(() => ((IList<string>)row.Before!).Add("changed"));
            Assert.Throws<NotSupportedException>(() => ((IList<ProfileChange>)result.Changes).Clear());
            Assert.Equal(DelegationGate.ContentHash(after), result.AfterHash);
        }

        [Fact]
        public void GrantIdentifiersAreNotInterpretedAsPathsOrHtml()
        {
            const string grantId = "g/<review>";
            ProfileComparison result = ProfileChanges.Compare(P(grants: new[] { G(id: grantId) }),
                P(grants: new[] { G(id: grantId, role: "<changed>") }));
            ProfileChange row = Assert.Single(result.Changes);
            Assert.Equal(grantId, row.GrantId);
            Assert.Equal("role", row.Field);
            Assert.Equal(new[] { "<changed>" }, row.After);
        }

        [Fact]
        public void AmbientCultureCannotAlterDatesOrOrdering()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Profile before = P(); Profile after = P(revision: 2, from: Start.AddDays(-1));
                ProfileComparison result = ProfileChanges.Compare(before, after);
                Assert.Equal(new[] { "from", "revision" }, result.Changes.Select(x => x.Field));
                Assert.Equal(new[] { "2026-09-23T00:00:00.0000000+00:00" }, result.Changes[0].After);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
    }
}
