using System;
using System.Linq;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Recover a caller's historical decision receipt without repeating or reauthorising the decision.</summary>
    public sealed class ReadOwnDecisionReceipt : IPlugin
    {
        /// <summary>Reserved record-bound read action; no registration is performed by this class.</summary>
        public const string ApiName = "dft_ReadOwnDecisionReceiptV1";
        /// <summary>A historical receipt is neither current scope nor proof that a new attempted payload was saved.</summary>
        public const string Disposition = "own-historical-record-only-not-current-scope";
        private static readonly string[] Columns = { "dft_actorref", "dft_record", "dft_decisionkey", "dft_recordedat",
            "dft_sequence", "dft_recordedstate", "dft_casehash" };
        private const string Failure = "Recorded decision receipt is unavailable. No decision was repeated or changed. Do not infer that the original save failed.";

        /// <summary>
        /// Accept Target and DecisionId only. Resolve the current platform caller and reuse its verified service.
        /// Reading one's existing accessible record does not require authority to make that decision again.
        /// Never call a writer, repair an event, retry a save or grant current clinical scope from this result.
        /// </summary>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            ParameterCollection? output = null;
            try
            {
                if (!(serviceProvider.GetService(typeof(IPluginExecutionContext)) is IPluginExecutionContext context)) throw Unavailable();
                output = context.OutputParameters;
                if (output == null) throw Unavailable();
                Reset(output);
                if (context.MessageName != ApiName || context.Stage != 30 || context.Mode != 0 || context.Depth != 1 ||
                    context.ParentContext != null || context.IsExecutingOffline || context.IsOfflinePlayback) throw Unavailable();
                ParameterCollection parameters = context.InputParameters;
                if (parameters == null || parameters.Count != 2 || !parameters.TryGetValue("Target", out object supplied) ||
                    !(supplied is EntityReference target) || target.LogicalName != RegisterSchemaPlan.RecordTable ||
                    target.Id == Guid.Empty || target.KeyAttributes.Count != 0 || !parameters.TryGetValue("DecisionId", out object suppliedId) ||
                    !(suppliedId is Guid decision) || decision == Guid.Empty) throw Unavailable();
                Guid record = target.Id; // Freeze both identifiers before native calls or mutable form input can change.
                if (!(serviceProvider.GetService(typeof(IOrganizationServiceFactory)) is IOrganizationServiceFactory factory)) throw Unavailable();
                CallerBinding binding = CallerResolver.ResolveBinding(context, factory);
                IOrganizationService service = binding.Service;
                string actor = binding.Caller.Person.ToString("D");
                Entity root = service.Retrieve(RegisterSchemaPlan.RecordTable, record, new ColumnSet(RegisterSchemaPlan.RecordTable + "id"));
                Canonical(root, RegisterSchemaPlan.RecordTable, record);
                var query = new QueryExpression(RegisterSchemaPlan.DecisionTable)
                {
                    ColumnSet = new ColumnSet((string[])Columns.Clone()), TopCount = 2
                };
                query.Criteria.AddCondition(RegisterSchemaPlan.DecisionTable + "id", ConditionOperator.Equal, decision);
                query.Criteria.AddCondition("dft_record", ConditionOperator.Equal, record);
                query.Criteria.AddCondition("dft_actorref", ConditionOperator.Equal, actor);
                EntityCollection rows = service.RetrieveMultiple(query);
                if (rows == null || rows.MoreRecords || rows.Entities.Count != 1) throw Unavailable();
                Entity row = rows.Entities[0];
                Canonical(row, RegisterSchemaPlan.DecisionTable, decision);
                if (!row.Attributes.TryGetValue("dft_record", out object relation) || !(relation is EntityReference parent) ||
                    parent.LogicalName != RegisterSchemaPlan.RecordTable || parent.Id != record || parent.KeyAttributes.Count != 0) throw Unavailable();
                Match(row, "dft_actorref", actor); Match(row, "dft_decisionkey", decision.ToString("N"));
                if (!row.Attributes.TryGetValue("dft_sequence", out object rawSequence) || !(rawSequence is int sequence) || sequence < 1) throw Unavailable();
                if (!row.Attributes.TryGetValue("dft_recordedstate", out object rawState) || !(rawState is OptionSetValue state) ||
                    state.Value < RegisterSchemaPlan.Unverified + 1 || state.Value > RegisterSchemaPlan.Unverified + 4) throw Unavailable();
                int outcome = state.Value; // Freeze the primitive before the final native access check.
                DateTime retrieved = DateTime.UtcNow;
                if (!row.Attributes.TryGetValue("dft_recordedat", out object rawTime) || !(rawTime is DateTime recorded) ||
                    recorded.Kind != DateTimeKind.Utc || recorded > retrieved) throw Unavailable();
                if (!row.Attributes.TryGetValue("dft_casehash", out object rawHash) || !(rawHash is string hash) ||
                    hash.Length != 64 || hash.Any(c => !"0123456789abcdef".Contains(c))) throw Unavailable();
                // Root/decision sharing is independent. A later root-access loss must not be ignored.
                Entity checkedRoot = service.Retrieve(RegisterSchemaPlan.RecordTable, record, new ColumnSet(RegisterSchemaPlan.RecordTable + "id"));
                Canonical(checkedRoot, RegisterSchemaPlan.RecordTable, record);
                retrieved = DateTime.UtcNow;

                // Primitives only: do not return the native Entity, private reasons, restrictions, owner or authority evidence.
                output["RecordId"] = record; output["DecisionId"] = decision; output["RecordedAtUtc"] = recorded;
                output["Sequence"] = sequence; output["RecordedOutcome"] = outcome; output["CaseHash"] = hash;
                output["RetrievedAtUtc"] = retrieved; output["ReadDisposition"] = Disposition; output["ReadSucceeded"] = true;
            }
            catch (Exception)
            {
                if (output != null) Reset(output);
                // No distinction between absent, inaccessible, malformed or failed reads; never infer an unsuccessful save.
                throw Unavailable();
            }
        }

        private static void Canonical(Entity row, string table, Guid id)
        { if (row == null || row.LogicalName != table || row.Id != id || row.KeyAttributes.Count != 0) throw Unavailable(); }
        private static void Match(Entity row, string field, string expected)
        { if (!row.Attributes.TryGetValue(field, out object value) || !(value is string text) || text != expected) throw Unavailable(); }
        private static void Reset(ParameterCollection output)
        { output.Clear(); output["ReadSucceeded"] = false; }
        private static InvalidPluginExecutionException Unavailable() => new InvalidPluginExecutionException(Failure);
    }
}
