using System;
using CareOps.Decisions.Authority;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using AuthorityRequest = CareOps.Decisions.Authority.Request;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>
    /// Composed write core using trusted current HOST snapshots. Not a registered API or a snapshot source.
    /// Joins real caller resolution, native identity/owner reads, exact human content and delegation checks before staging.
    /// </summary>
    public static class HumanConfirmedJournal
    {
        private const string Failure = "Credentialing decision could not be verified for recording. No automatic reapproval or repair was attempted.";

        /// <summary>
        /// Stage only the same confirmed, currently authorised intent under the SAME caller service.
        /// The future protected source adapter must resolve/lock the supplied confirmation and authority records.
        /// A passing constructor or caller-generated hash is not that source. Do not expose these arguments over an API.
        /// </summary>
        public static JournalAppendDisposition Stage(IPluginExecutionContext context, IOrganizationServiceFactory factory,
            DecisionAppend intent, Profile profile, Release release, AuthorityRequest authority, HumanDecisionConfirmation confirmation)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (authority == null) throw new ArgumentNullException(nameof(authority));
            if (confirmation == null) throw new ArgumentNullException(nameof(confirmation));
            try
            {
                Require(context.MessageName == RegisterJournalAppender.ApiName && context.Stage == 30 && context.Mode == 0 &&
                    context.IsInTransaction && context.Depth == 1 && context.ParentContext == null && !context.IsExecutingOffline && !context.IsOfflinePlayback);
                ParameterCollection parameters = context.InputParameters;
                if (parameters == null || !parameters.TryGetValue("Target", out object raw) || !(raw is EntityReference target))
                    throw new InvalidPluginExecutionException(Failure);
                Require(target.LogicalName == RegisterSchemaPlan.RecordTable && target.Id == intent.RecordId && target.KeyAttributes.Count == 0);
                // Freeze before SDK calls; never retain a mutable caller Target as the storage target.
                Guid recordId = target.Id;
                string digest = DecisionConfirmationHash.Compute(context.OrganizationId, intent); // Rejects unowned fixtures.
                CallerBinding binding = CallerResolver.ResolveBinding(context, factory);
                CallerPerson caller = binding.Caller; IOrganizationService service = binding.Service;
                Require(profile.Organization == caller.Organization && authority.Organization == caller.Organization &&
                    confirmation.Organization == caller.Organization && authority.Actor == caller.Person && confirmation.ActorPerson == caller.Person);
                Entity payload = intent.ToEntity();
                Match(payload, "dft_actorref", caller.Person.ToString("D"));
                Match(payload, "dft_profilehash", release.Hash); Match(payload, "dft_profileversionref", release.Reference);
                Match(payload, "dft_function", confirmation.RequiredFunction);
                Match(payload, "dft_authorityevidenceref", authority.OccupancyReference);
                Match(payload, "dft_decisionevidenceref", confirmation.EvidenceReference);
                Require(authority.Function == confirmation.RequiredFunction && authority.Pathway == confirmation.Pathway &&
                    ((OptionSetValue)payload["dft_recordedstate"]).Value == confirmation.RecordedState && digest == confirmation.Digest);

                Entity root = service.Retrieve(RegisterSchemaPlan.RecordTable, recordId,
                    new ColumnSet("dft_practitionerref", "dft_professionref", "dft_facilityref"));
                Canonical(root, RegisterSchemaPlan.RecordTable, recordId);
                Match(root, "dft_practitionerref", authority.Practitioner.ToString("D"));
                Match(root, "dft_professionref", authority.Profession); Match(root, "dft_facilityref", authority.Facility);
                // The sealed owner-bound intent and digest above guarantee both values; no nullable-owner fallback exists.
                Guid decisionOwner = intent.DecisionOwnerId!.Value, eventOwner = intent.EventOwnerId!.Value;
                Owner(service, decisionOwner);
                if (eventOwner != decisionOwner) Owner(service, eventOwner);

                // Server clock after preparation reads: not supplied by the caller or an agent.
                DateTimeOffset at = DateTimeOffset.UtcNow;
                Require(!confirmation.Withdrawn && confirmation.ConfirmedAt <= at && at < confirmation.CurrentUntil);
                DateTime recorded = (DateTime)payload["dft_recordedat"];
                Require(confirmation.ConfirmedAt.UtcDateTime <= recorded && recorded <= at.UtcDateTime);
                // Current authority cannot retrospectively validate a human action made before that authority began.
                Require(DelegationGate.Check(profile, release, authority, confirmation.ConfirmedAt) == Outcome.Permitted);
                Require(DelegationGate.Check(profile, release, authority, at) == Outcome.Permitted);
                // Root CAS, exact old receipts and event staging stay in their existing tested implementation.
                return JournalWithOutbox.Stage(context, service, intent);
            }
            catch (Exception)
            {
                // Must escape the native transaction, including a failure after staging. Never compensate, retry or send here.
                throw new InvalidPluginExecutionException(Failure);
            }
        }

        private static void Owner(IOrganizationService service, Guid id)
        {
            Entity team = service.Retrieve("team", id, new ColumnSet("teamtype", "isdefault", "businessunitid"));
            Canonical(team, "team", id);
            Require(team.Attributes.TryGetValue("teamtype", out object type) && type is OptionSetValue);
            int value = ((OptionSetValue)team["teamtype"]).Value;
            Require(value == 0 || value == 2 || value == 3); // Owner / Entra security group / Office group, never access team.
            Require(team.Attributes.TryGetValue("isdefault", out object flag) && flag is bool isDefault && !isDefault);
            Require(team.Attributes.TryGetValue("businessunitid", out object unit) && unit is EntityReference);
            var reference = (EntityReference)team["businessunitid"];
            Require(reference.LogicalName == "businessunit" && reference.Id != Guid.Empty && reference.KeyAttributes.Count == 0);
            // Team kind is not permission: the protected access profile and native Create/Assign privileges must also apply.
        }
        private static void Canonical(Entity row, string table, Guid id) =>
            Require(row != null && row.LogicalName == table && row.Id == id && row.KeyAttributes.Count == 0);
        private static void Match(Entity row, string name, string expected) =>
            Require(row.Attributes.TryGetValue(name, out object value) && value is string text && text == expected);
        private static void Require(bool condition)
        { if (!condition) throw new InvalidPluginExecutionException(Failure); }
    }
}
