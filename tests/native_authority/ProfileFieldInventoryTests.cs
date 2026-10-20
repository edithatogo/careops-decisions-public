using System;
using System.Linq;
using CareOps.Decisions.Authority;
using Xunit;

namespace CareOps.Decisions.Authority.Tests
{
    public sealed class ProfileFieldInventoryTests
    {
        [Fact]
        public void NewAuthorityFieldsRequireAnExplicitDigestAndComparisonReview()
        {
            // A future public field must not silently evade the exact-content review view.
            Assert.Equal(new[] { "Editor", "From", "Grants", "Id", "Instrument", "Organization", "Revision", "SourceHash", "Until" },
                typeof(Profile).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(new[] { "Conditions", "Facilities", "Function", "Id", "IndependentOfCommittee", "Pathways", "Professions", "Role" },
                typeof(Grant).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Empty(typeof(Profile).GetFields());
            Assert.Empty(typeof(Grant).GetFields());
        }
    }
}
