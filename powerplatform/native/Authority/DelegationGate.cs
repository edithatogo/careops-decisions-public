using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CareOps.Decisions.Authority
{
    /// <summary>Source-supplied grant. Identifiers are exact, case-sensitive values, not job-title aliases.</summary>
    public sealed class Grant
    {
        /// <summary>Create a defensively copied, bounded grant.</summary>
        public Grant(string id, string function, string role, IReadOnlyCollection<string> professions,
            IReadOnlyCollection<string> facilities, IReadOnlyCollection<string> pathways,
            IReadOnlyCollection<string> conditions, bool independentOfCommittee)
        {
            Id = Guard.Id(id); Function = Guard.Id(function); Role = Guard.Id(role);
            Professions = Guard.Copy(professions, false); Facilities = Guard.Copy(facilities, false);
            Pathways = Guard.Copy(pathways, false); Conditions = Guard.Copy(conditions, true);
            IndependentOfCommittee = independentOfCommittee;
        }
        /// <summary>Grant identity.</summary>
        public string Id { get; }
        /// <summary>Delegated function.</summary>
        public string Function { get; }
        /// <summary>Position identifier, not a display title.</summary>
        public string Role { get; }
        /// <summary>Permitted professions.</summary>
        public ReadOnlyCollection<string> Professions { get; }
        /// <summary>Permitted facilities.</summary>
        public ReadOnlyCollection<string> Facilities { get; }
        /// <summary>Permitted pathways.</summary>
        public ReadOnlyCollection<string> Pathways { get; }
        /// <summary>Conditions requiring independent evidence.</summary>
        public ReadOnlyCollection<string> Conditions { get; }
        /// <summary>Whether committee participation excludes this grant.</summary>
        public bool IndependentOfCommittee { get; }
    }

    /// <summary>Immutable exact-content profile; construction does not approve it.</summary>
    public sealed class Profile
    {
        /// <summary>Freeze a revision with a source hash and half-open effective period.</summary>
        public Profile(Guid organization, Guid id, int revision, Guid editor, string instrument,
            string sourceHash, DateTimeOffset from, DateTimeOffset until, IReadOnlyCollection<Grant> grants)
        {
            if (organization == Guid.Empty || id == Guid.Empty || editor == Guid.Empty || revision < 1)
                throw new ArgumentException("Profile identity and a positive revision are required.");
            Guard.Period(from, until);
            if (grants == null) throw new ArgumentNullException(nameof(grants));
            if (grants.Count < 1 || grants.Count > 128) throw new ArgumentException("One to 128 grants required.");
            var copy = grants.ToArray();
            if (copy.Any(g => g == null) || copy.Select(g => g.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("Unique, non-null grants required.");
            Organization = organization; Id = id; Revision = revision; Editor = editor;
            Instrument = Guard.Id(instrument); SourceHash = Guard.Hash(sourceHash);
            From = from; Until = until;
            Grants = Array.AsReadOnly(copy.OrderBy(g => g.Id, StringComparer.Ordinal).ToArray());
        }
        /// <summary>Owning Dataverse organization.</summary>
        public Guid Organization { get; }
        /// <summary>Stable profile identity.</summary>
        public Guid Id { get; }
        /// <summary>Positive content revision.</summary>
        public int Revision { get; }
        /// <summary>Authenticated editor identity.</summary>
        public Guid Editor { get; }
        /// <summary>Controlled instrument reference.</summary>
        public string Instrument { get; }
        /// <summary>Verified source content hash, not proof of source authenticity.</summary>
        public string SourceHash { get; }
        /// <summary>Inclusive effective instant.</summary>
        public DateTimeOffset From { get; }
        /// <summary>Exclusive effective end.</summary>
        public DateTimeOffset Until { get; }
        /// <summary>Sorted, immutable grants.</summary>
        public ReadOnlyCollection<Grant> Grants { get; }
    }

    /// <summary>Trusted server snapshot of release evidence; never bind this object from a UI or agent.</summary>
    public sealed class Release
    {
        /// <summary>Create a snapshot; all fields must be resolved from protected records by the host.</summary>
        public Release(string hash, Guid approver, string reference, DateTimeOffset approvedAt,
            DateTimeOffset currentUntil, bool sourceVerified, bool withdrawn)
        {
            Hash = Guard.Hash(hash); Reference = Guard.Id(reference); Guard.Period(approvedAt, currentUntil);
            if (approver == Guid.Empty) throw new ArgumentException("Approver identity required.");
            Approver = approver; ApprovedAt = approvedAt; CurrentUntil = currentUntil;
            SourceVerified = sourceVerified; Withdrawn = withdrawn;
        }
        /// <summary>Exact native profile hash.</summary>
        public string Hash { get; }
        /// <summary>Authenticated approver identity.</summary>
        public Guid Approver { get; }
        /// <summary>Approval record reference.</summary>
        public string Reference { get; }
        /// <summary>Approval instant.</summary>
        public DateTimeOffset ApprovedAt { get; }
        /// <summary>Exclusive verification expiry.</summary>
        public DateTimeOffset CurrentUntil { get; }
        /// <summary>Protected source verification result; not a caller assertion.</summary>
        public bool SourceVerified { get; }
        /// <summary>Whether the release has been withdrawn.</summary>
        public bool Withdrawn { get; }
    }

    /// <summary>Authenticated host context. A principal identifier by itself is not authentication.</summary>
    public sealed class Request
    {
        /// <summary>Freeze a requested function and the independently resolved occupancy/conflict evidence.</summary>
        public Request(Guid organization, Guid actor, Guid practitioner, string role, string occupancyReference,
            DateTimeOffset occupancyFrom, DateTimeOffset occupancyUntil, string function,
            string profession, string facility, string pathway, IReadOnlyCollection<string> conditions,
            bool? conflict, bool? participatedInCommittee)
        {
            if (organization == Guid.Empty || actor == Guid.Empty || practitioner == Guid.Empty)
                throw new ArgumentException("Organization, actor and practitioner identities required.");
            Guard.Period(occupancyFrom, occupancyUntil);
            Organization = organization; Actor = actor; Practitioner = practitioner; Role = Guard.Id(role);
            OccupancyReference = Guard.Id(occupancyReference); OccupancyFrom = occupancyFrom; OccupancyUntil = occupancyUntil;
            Function = Guard.Id(function); Profession = Guard.Id(profession); Facility = Guard.Id(facility);
            Pathway = Guard.Id(pathway); Conditions = Guard.Copy(conditions, true);
            Conflict = conflict; ParticipatedInCommittee = participatedInCommittee;
        }
        /// <summary>Authenticated organization.</summary>
        public Guid Organization { get; }
        /// <summary>Authenticated human actor.</summary>
        public Guid Actor { get; }
        /// <summary>Practitioner whose case is being considered.</summary>
        public Guid Practitioner { get; }
        /// <summary>Verified occupied position.</summary>
        public string Role { get; }
        /// <summary>Protected occupancy evidence.</summary>
        public string OccupancyReference { get; }
        /// <summary>Inclusive occupancy start.</summary>
        public DateTimeOffset OccupancyFrom { get; }
        /// <summary>Exclusive occupancy end.</summary>
        public DateTimeOffset OccupancyUntil { get; }
        /// <summary>Requested function.</summary>
        public string Function { get; }
        /// <summary>Case profession.</summary>
        public string Profession { get; }
        /// <summary>Case facility.</summary>
        public string Facility { get; }
        /// <summary>Case pathway.</summary>
        public string Pathway { get; }
        /// <summary>Conditions resolved from protected evidence.</summary>
        public ReadOnlyCollection<string> Conditions { get; }
        /// <summary>Unknown conflict evidence denies this action.</summary>
        public bool? Conflict { get; }
        /// <summary>Unknown participation cannot satisfy an independence requirement.</summary>
        public bool? ParticipatedInCommittee { get; }
    }

    /// <summary>Permission result for one action, never a practitioner suitability finding.</summary>
    public enum Outcome
    {
        /// <summary>Default, missing or uninitialised result; never permission.</summary>
        Unverified = 0,
        /// <summary>All supplied, trusted constraints match. Callers must require this exact value.</summary>
        Permitted = 1,
        /// <summary>Organization mismatch.</summary>
        WrongOrganization,
        /// <summary>Withdrawn, unverified, stale, altered or self-approved configuration.</summary>
        InvalidRelease,
        /// <summary>Profile is not currently effective.</summary>
        ProfileNotEffective,
        /// <summary>Position occupancy is not currently effective.</summary>
        OccupancyNotEffective,
        /// <summary>Self-decision or unknown/positive conflict.</summary>
        Conflict,
        /// <summary>Zero or multiple matching grants.</summary>
        MissingOrAmbiguousGrant,
        /// <summary>Independence evidence is missing or contradicted.</summary>
        IndependenceRequired,
        /// <summary>Required conditions are not confirmed.</summary>
        ConditionsUnconfirmed
    }

    /// <summary>Pure bounded predicates for a protected Dataverse adapter; not an authentication service.</summary>
    public static class DelegationGate
    {
        /// <summary>Versioned binary hash of all editable authority fields, with exact ordinal set ordering.</summary>
        public static string ContentHash(Profile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, new UTF8Encoding(false, true), true))
                {
                    writer.Write("careops-native-authority-v1");
                    writer.Write(profile.Organization.ToByteArray()); writer.Write(profile.Id.ToByteArray());
                    writer.Write(profile.Revision); writer.Write(profile.Editor.ToByteArray());
                    writer.Write(profile.Instrument); writer.Write(profile.SourceHash);
                    writer.Write(profile.From.UtcDateTime.Ticks); writer.Write(profile.Until.UtcDateTime.Ticks);
                    writer.Write(profile.Grants.Count);
                    foreach (Grant grant in profile.Grants)
                    {
                        writer.Write(grant.Id); writer.Write(grant.Function); writer.Write(grant.Role);
                        foreach (ReadOnlyCollection<string> set in new[] { grant.Professions, grant.Facilities, grant.Pathways, grant.Conditions })
                        {
                            writer.Write(set.Count);
                            foreach (string value in set) writer.Write(value);
                        }
                        writer.Write(grant.IndependentOfCommittee);
                    }
                }
                using (var hash = SHA256.Create())
                    return BitConverter.ToString(hash.ComputeHash(buffer.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>Recheck exact approval binding and decision-time constraints; no state or prior decision is changed.</summary>
        public static Outcome Check(Profile profile, Release release, Request request, DateTimeOffset at)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Organization != profile.Organization) return Outcome.WrongOrganization;
            if (!release.SourceVerified || release.Withdrawn || release.Approver == profile.Editor ||
                release.Hash != ContentHash(profile) || at < release.ApprovedAt || at >= release.CurrentUntil)
                return Outcome.InvalidRelease;
            if (at < profile.From || at >= profile.Until) return Outcome.ProfileNotEffective;
            if (at < request.OccupancyFrom || at >= request.OccupancyUntil) return Outcome.OccupancyNotEffective;
            if (request.Actor == request.Practitioner || request.Conflict != false) return Outcome.Conflict;
            Grant[] matches = profile.Grants.Where(g => g.Function == request.Function && g.Role == request.Role &&
                g.Professions.Contains(request.Profession) && g.Facilities.Contains(request.Facility) &&
                g.Pathways.Contains(request.Pathway)).ToArray();
            if (matches.Length != 1) return Outcome.MissingOrAmbiguousGrant;
            Grant grant = matches[0];
            if (grant.IndependentOfCommittee && request.ParticipatedInCommittee != false) return Outcome.IndependenceRequired;
            if (!grant.Conditions.All(request.Conditions.Contains)) return Outcome.ConditionsUnconfirmed;
            return Outcome.Permitted;
        }
    }

    internal static class Guard
    {
        internal static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value != value.Trim() || value.Any(char.IsControl))
                throw new ArgumentException("Nonempty bounded exact identifier required.");
            // Reject invalid UTF-16 instead of allowing distinct inputs to hash as replacement characters.
            new UTF8Encoding(false, true).GetByteCount(value);
            return value;
        }
        internal static string Hash(string value)
        {
            if (value == null || value.Length != 64 || value.Any(c => !"0123456789abcdef".Contains(c)))
                throw new ArgumentException("Lowercase SHA-256 required.");
            return value;
        }
        internal static void Period(DateTimeOffset from, DateTimeOffset until)
        {
            if (from >= until) throw new ArgumentException("Nonempty half-open period required.");
        }
        internal static ReadOnlyCollection<string> Copy(IReadOnlyCollection<string> values, bool emptyAllowed)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count > 128 || (!emptyAllowed && values.Count == 0))
                throw new ArgumentException("Bounded nonempty identifiers required unless explicitly optional.");
            string[] copy = values.Select(Id).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Duplicate identifier.");
            return Array.AsReadOnly(copy);
        }
    }
}
