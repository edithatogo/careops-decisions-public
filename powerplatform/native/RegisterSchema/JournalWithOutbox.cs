using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Stage the journal and one immutable event using the SAME transaction-bound native service.</summary>
    public static class JournalWithOutbox
    {
        /// <summary>
        /// A protected host must first bind caller, case, human decision and current authority. This is not an endpoint.
        /// Failure escapes the ambient transaction; no notification, retry or compensating write is performed here.
        /// </summary>
        public static JournalAppendDisposition Stage(IPluginExecutionContext context, IOrganizationService service, DecisionAppend intent)
        {
            JournalAppendDisposition disposition = RegisterJournalAppender.Stage(context, service, intent);
            Entity expected = Envelope(context.OrganizationId, intent);
            if (disposition == JournalAppendDisposition.StagedInTransaction)
            {
                if (service.Create(expected) != intent.DecisionId) throw InvalidState();
            }
            else
            {
                var query = new QueryExpression(DecisionEventSchema.Table)
                {
                    ColumnSet = new ColumnSet(expected.Attributes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray()), TopCount = 2
                };
                query.Criteria.AddCondition(DecisionEventSchema.Table + "id", ConditionOperator.Equal, intent.DecisionId);
                EntityCollection rows = service.RetrieveMultiple(query);
                if (rows == null || rows.MoreRecords || rows.Entities.Count != 1 || !Matches(expected, rows.Entities[0]))
                    throw InvalidState();
            }
            return disposition;
        }

        private static Entity Envelope(Guid organization, DecisionAppend intent)
        {
            var row = new Entity(DecisionEventSchema.Table, intent.DecisionId);
            row["dft_name"] = intent.DecisionId.ToString("D"); row["dft_eventkey"] = intent.DecisionId.ToString("N");
            row["dft_eventtype"] = DecisionEventSchema.EventType; row["dft_eventversion"] = DecisionEventSchema.Version;
            row["dft_organizationref"] = organization.ToString("N");
            row["dft_record"] = new EntityReference(RegisterSchemaPlan.RecordTable, intent.RecordId);
            row["dft_decision"] = new EntityReference(RegisterSchemaPlan.DecisionTable, intent.DecisionId);
            row["dft_sequence"] = intent.Sequence; row["dft_recordedat"] = intent.ToEntity()["dft_recordedat"];
            if (intent.EventOwnerId.HasValue) row["ownerid"] = new EntityReference("team", intent.EventOwnerId.Value);
            return row;
        }

        private static bool Matches(Entity expected, Entity actual)
        {
            if (actual == null || actual.LogicalName != expected.LogicalName || actual.Id != expected.Id) return false;
            int primary = 0;
            if (actual.Attributes.TryGetValue(DecisionEventSchema.Table + "id", out object key))
            {
                if (!(key is Guid id) || id != expected.Id) return false;
                primary = 1;
            }
            if (actual.Attributes.Count != expected.Attributes.Count + primary) return false;
            return expected.Attributes.All(pair => actual.Attributes.TryGetValue(pair.Key, out object value) && Same(pair.Value, value));
        }

        private static bool Same(object expected, object actual)
        {
            if (actual == null || actual.GetType() != expected.GetType()) return false;
            if (expected is EntityReference a)
            {
                var b = (EntityReference)actual;
                return a.Id == b.Id && a.LogicalName == b.LogicalName && b.KeyAttributes.Count == 0;
            }
            if (expected is DateTime instant) return instant == (DateTime)actual && ((DateTime)actual).Kind == DateTimeKind.Utc;
            return expected.Equals(actual);
        }

        private static InvalidPluginExecutionException InvalidState() => new InvalidPluginExecutionException(
            "Credentialing event could not be reconciled. Abort this transaction; do not resend or repair within it.");
    }
}
