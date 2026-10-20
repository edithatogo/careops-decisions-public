using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Storage staging outcome, never authority, practice clearance or proof of final commit.</summary>
    public enum JournalAppendDisposition
    {
        /// <summary>Default or absent result; no success may be inferred.</summary>
        Unverified = 0,
        /// <summary>Changes were staged inside the current transaction; later failure can still roll them back.</summary>
        StagedInTransaction = 1,
        /// <summary>An exact existing receipt was read without rewriting the register.</summary>
        AlreadyRecorded = 2
    }

    /// <summary>Lower-level journal storage for a separately protected handler; not an exposed plug-in or API.</summary>
    public static class RegisterJournalAppender
    {
        /// <summary>Reserved handler name; this library does not register or implement the public endpoint.</summary>
        public const string ApiName = "dft_CommitCredentialingDecisionV1";
        private static readonly string[] RootColumns = { "dft_latestsequence", "dft_latestdecision" };
        private static readonly string[] PreviousColumns = { "dft_record", "dft_sequence", "dft_recordedat" };

        /// <summary>
        /// Stage a verified host intent in its existing synchronous native transaction. The host must authenticate,
        /// authorise and confirm the exact human decision first. Errors escape; never catch a data fault and continue.
        /// </summary>
        public static JournalAppendDisposition Stage(IPluginExecutionContext context, IOrganizationService service, DecisionAppend intent)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            if (context.MessageName != ApiName || context.Stage != 30 || context.Mode != 0 || !context.IsInTransaction ||
                context.Depth != 1 || context.ParentContext != null || context.IsExecutingOffline || context.IsOfflinePlayback ||
                context.OrganizationId == Guid.Empty || context.InitiatingUserId == Guid.Empty || context.UserId != context.InitiatingUserId)
                throw InvalidState();
            if (!(service.Execute(new WhoAmIRequest()) is WhoAmIResponse who) ||
                who.UserId != context.InitiatingUserId || who.OrganizationId != context.OrganizationId)
                throw InvalidState();

            // Query rather than catching a failed retrieve inside a transaction. An old exact receipt never replays writes.
            var replay = new QueryExpression(RegisterSchemaPlan.DecisionTable) { ColumnSet = new ColumnSet(intent.Columns), TopCount = 2 };
            replay.Criteria.AddCondition(RegisterSchemaPlan.DecisionTable + "id", ConditionOperator.Equal, intent.DecisionId);
            EntityCollection existing = service.RetrieveMultiple(replay);
            if (existing == null || existing.MoreRecords || existing.Entities.Count > 1) throw InvalidState();
            if (existing.Entities.Count == 1)
            {
                if (!intent.Matches(existing.Entities[0])) throw InvalidState();
                return JournalAppendDisposition.AlreadyRecorded;
            }

            Entity proposed = intent.ToEntity();
            Entity root = service.Retrieve(RegisterSchemaPlan.RecordTable, intent.RecordId, new ColumnSet(RootColumns));
            if (root == null || root.LogicalName != RegisterSchemaPlan.RecordTable || root.Id != intent.RecordId ||
                root.RowVersion != intent.ExpectedVersion || !root.Attributes.TryGetValue("dft_latestsequence", out object value) ||
                !(value is int sequence) || sequence != intent.ExpectedSequence)
                throw InvalidState();
            root.Attributes.TryGetValue("dft_latestdecision", out object previous);
            if (intent.PreviousDecision.HasValue)
            {
                if (!MatchesReference(previous, RegisterSchemaPlan.DecisionTable, intent.PreviousDecision.Value)) throw InvalidState();
                Entity predecessor = service.Retrieve(RegisterSchemaPlan.DecisionTable, intent.PreviousDecision.Value, new ColumnSet(PreviousColumns));
                if (predecessor == null || predecessor.LogicalName != RegisterSchemaPlan.DecisionTable || predecessor.Id != intent.PreviousDecision.Value ||
                    !predecessor.Attributes.TryGetValue("dft_record", out object record) || !MatchesReference(record, RegisterSchemaPlan.RecordTable, intent.RecordId) ||
                    !predecessor.Attributes.TryGetValue("dft_sequence", out object number) || !(number is int priorSequence) || priorSequence != sequence)
                    throw InvalidState();
                // Recording order is separate from legal effect. Equal instants use sequence as the tie-breaker.
                if (!predecessor.Attributes.TryGetValue("dft_recordedat", out object recorded) || !(recorded is DateTime priorRecorded) ||
                    priorRecorded.Kind != DateTimeKind.Utc || priorRecorded > (DateTime)proposed["dft_recordedat"])
                    throw InvalidState();
            }
            else if (previous != null) throw InvalidState();

            // Create before setting the lookup. A subsequent failed CAS must abort this entire native transaction.
            if (service.Create(proposed) != intent.DecisionId) throw InvalidState();
            var update = new Entity(RegisterSchemaPlan.RecordTable, intent.RecordId) { RowVersion = intent.ExpectedVersion };
            update["dft_latestsequence"] = intent.Sequence;
            update["dft_latestdecision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, intent.DecisionId);
            update["dft_projectedasof"] = null; // Old operational projection is not newly verified or practice clearance.
            if (!(service.Execute(new UpdateRequest { Target = update, ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches }) is UpdateResponse))
                throw InvalidState();
            return JournalAppendDisposition.StagedInTransaction;
        }

        private static bool MatchesReference(object value, string table, Guid id) =>
            value is EntityReference reference && reference.LogicalName == table && reference.Id == id && reference.KeyAttributes.Count == 0;

        private static InvalidPluginExecutionException InvalidState() => new InvalidPluginExecutionException(
            "Credentialing journal staging could not be verified. Abort this transaction; do not retry within it.");
    }
}
