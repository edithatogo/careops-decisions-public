using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CareOps.Decisions.Authority
{
    /// <summary>One exact difference for review, not a legal interpretation or a permission.</summary>
    public sealed class ProfileChange
    {
        internal ProfileChange(string grantId, string field, string[]? before, string[]? after)
        {
            GrantId = grantId; Field = field;
            Before = before == null ? null : Array.AsReadOnly((string[])before.Clone());
            After = after == null ? null : Array.AsReadOnly((string[])after.Clone());
        }
        /// <summary>Exact grant identity, or empty for a profile-level field. Not a path.</summary>
        public string GrantId { get; }
        /// <summary>Stable field name for rendering with an approved display label.</summary>
        public string Field { get; }
        /// <summary>Null means absent grant, empty means a present empty set.</summary>
        public ReadOnlyCollection<string>? Before { get; }
        /// <summary>Null means removed grant, empty means a present empty set.</summary>
        public ReadOnlyCollection<string>? After { get; }
    }

    /// <summary>Immutable changes bound to both full profiles. Does not approve the new version.</summary>
    public sealed class ProfileComparison
    {
        internal ProfileComparison(Profile before, Profile after, List<ProfileChange> changes)
        {
            BeforeHash = DelegationGate.ContentHash(before);
            AfterHash = DelegationGate.ContentHash(after);
            Changes = changes.AsReadOnly();
        }
        /// <summary>Complete before-profile digest under careops-native-authority-v1.</summary>
        public string BeforeHash { get; }
        /// <summary>Complete after-profile digest; approval must still bind the whole profile.</summary>
        public string AfterHash { get; }
        /// <summary>Profile fields followed by ordinal grant/field order, including removals.</summary>
        public ReadOnlyCollection<ProfileChange> Changes { get; }
    }

    /// <summary>Deterministic preparation for the existing configuration approval, with no extra queue.</summary>
    public static class ProfileChanges
    {
        /// <summary>Compare two revisions of one profile; reject unrelated organization/profile identities.</summary>
        public static ProfileComparison Compare(Profile before, Profile after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (before.Organization != after.Organization || before.Id != after.Id)
                throw new ArgumentException("Compare revisions of the same organization and profile only.");
            var changes = new List<ProfileChange>();
            Append(changes, "", ProfileFields(before), ProfileFields(after));
            var oldGrants = before.Grants.ToDictionary(g => g.Id, StringComparer.Ordinal);
            var newGrants = after.Grants.ToDictionary(g => g.Id, StringComparer.Ordinal);
            foreach (string id in oldGrants.Keys.Union(newGrants.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
            {
                oldGrants.TryGetValue(id, out Grant? oldGrant);
                newGrants.TryGetValue(id, out Grant? newGrant);
                Append(changes, id, GrantFields(oldGrant), GrantFields(newGrant));
            }
            return new ProfileComparison(before, after, changes);
        }

        private static Dictionary<string, string[]> ProfileFields(Profile profile) =>
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["revision"] = new[] { profile.Revision.ToString(CultureInfo.InvariantCulture) },
                ["editor"] = new[] { profile.Editor.ToString("D") },
                ["instrument"] = new[] { profile.Instrument },
                ["sourceHash"] = new[] { profile.SourceHash },
                ["from"] = new[] { profile.From.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) },
                ["until"] = new[] { profile.Until.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) }
            };

        private static Dictionary<string, string[]> GrantFields(Grant? grant)
        {
            if (grant == null) return new Dictionary<string, string[]>(StringComparer.Ordinal);
            return new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["function"] = new[] { grant.Function }, ["role"] = new[] { grant.Role },
                ["professions"] = grant.Professions.ToArray(), ["facilities"] = grant.Facilities.ToArray(),
                ["pathways"] = grant.Pathways.ToArray(), ["conditions"] = grant.Conditions.ToArray(),
                ["independentOfCommittee"] = new[] { grant.IndependentOfCommittee ? "true" : "false" }
            };
        }

        private static void Append(List<ProfileChange> changes, string grantId,
            Dictionary<string, string[]> before, Dictionary<string, string[]> after)
        {
            foreach (string field in before.Keys.Union(after.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
            {
                before.TryGetValue(field, out string[]? oldValue);
                after.TryGetValue(field, out string[]? newValue);
                if (oldValue != null && newValue != null && oldValue.SequenceEqual(newValue, StringComparer.Ordinal))
                    continue;
                changes.Add(new ProfileChange(grantId, field, oldValue, newValue));
            }
        }
    }
}
