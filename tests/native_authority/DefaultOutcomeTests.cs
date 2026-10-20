using System;
using CareOps.Decisions.Authority;
using Xunit;

namespace CareOps.Decisions.Authority.Tests
{
    public sealed class DefaultOutcomeTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(999)]
        public void OnlyExplicitPermissionCanBeAccepted(int unverified)
        {
            Assert.Equal(Outcome.Unverified, default(Outcome));
            Assert.NotEqual(Outcome.Permitted, (Outcome)unverified);
        }

        [Fact]
        public void NativeV1DigestMatchesIndependentBinaryEncodingVector()
        {
            var one = new[] { "one" };
            var profile = new Profile(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000002"), 1,
                Guid.Parse("30000000-0000-0000-0000-000000000003"), "source-1", new string('a', 64),
                new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero),
                new[] { new Grant("g1", "approve", "position-1", one, one, one, Array.Empty<string>(), false) });
            // Independently encoded 225-byte vector: little-endian GUIDs/counts/ticks,
            // UTF-8 strings with BinaryWriter 7-bit lengths, ordinal sorted sets.
            Assert.Equal("647e33349d20777b99caae8939e503f6d695351abb68182719256b7692171ff2",
                DelegationGate.ContentHash(profile));
        }
    }
}
