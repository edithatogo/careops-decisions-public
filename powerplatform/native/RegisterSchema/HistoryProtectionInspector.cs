using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Bounded read-only deployment check, not a clinical decision or a deployment approval.</summary>
    public static class HistoryProtectionInspector
    {
        private static readonly string[] StepColumns = { "eventhandler", "sdkmessageid", "sdkmessagefilterid", "rank",
            "stage", "mode", "supporteddeployment", "statecode", "statuscode", "canbebypassed", "asyncautodelete", "iscustomizable" };
        private static readonly string[] ForbiddenSettings = { "filteringattributes", "configuration", "impersonatinguserid", "sdkmessageprocessingstepsecureconfigid" };
        private const string Failure = "History protection configuration could not be verified. No configuration or credentialing record was changed.";

        /// <summary>
        /// Return the four observed active step IDs only after the exact metadata matrix matches.
        /// Use an approved metadata-readable service and qualified type ID; never run this per practitioner.
        /// This checks an observation, not assembly provenance, bypass privileges or future configuration stability.
        /// </summary>
        public static ReadOnlyCollection<Guid> Inspect(IOrganizationService service, Guid organization, Guid caller, Guid pluginTypeId, int rank)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (organization == Guid.Empty || caller == Guid.Empty || pluginTypeId == Guid.Empty || rank < 1)
                throw new ArgumentException("An approved organization, connection principal, qualified type and explicit rank are required.");
            try
            {
                if (!(service.Execute(new WhoAmIRequest()) is WhoAmIResponse who) || who.OrganizationId != organization || who.UserId != caller)
                    throw Rejected();
                Entity handler = service.Retrieve("plugintype", pluginTypeId, new ColumnSet("typename"));
                Canonical(handler, "plugintype", pluginTypeId);
                var query = new QueryExpression("sdkmessageprocessingstep")
                {
                    ColumnSet = new ColumnSet((string[])StepColumns.Clone()), TopCount = 5
                };
                // Do not filter by active state, table or message: extra/disabled/broad bindings must not disappear.
                query.Criteria.AddCondition("eventhandler", ConditionOperator.Equal, pluginTypeId);
                EntityCollection rows = service.RetrieveMultiple(query);
                if (rows == null || rows.MoreRecords || rows.Entities.Count != 4) throw Rejected();
                var ids = new HashSet<Guid>(); var pairs = new HashSet<string>(StringComparer.Ordinal);
                foreach (Entity step in rows.Entities)
                {
                    if (step == null) throw Rejected();
                    Canonical(step, "sdkmessageprocessingstep", step.Id);
                    if (!ids.Add(step.Id)) throw Rejected();
                    Guid messageId = Reference(step, "sdkmessageid", "sdkmessage");
                    Guid filterId = Reference(step, "sdkmessagefilterid", "sdkmessagefilter");
                    Entity message = service.Retrieve("sdkmessage", messageId, new ColumnSet("name"));
                    Entity filter = service.Retrieve("sdkmessagefilter", filterId, new ColumnSet("primaryobjecttypecode", "secondaryobjecttypecode",
                        "sdkmessageid", "iscustomprocessingstepallowed", "availability"));
                    Canonical(message, "sdkmessage", messageId); Canonical(filter, "sdkmessagefilter", filterId);
                    // Reuse the reviewed definition; only the required observed ACTIVE state differs.
                    Entity expected = HistoryMutationStepDefinition.Build(step.Id, rank, handler, message, filter).Target;
                    expected["statecode"] = new OptionSetValue(0); expected["statuscode"] = new OptionSetValue(1);
                    foreach (string column in StepColumns)
                    {
                        if (!step.Attributes.TryGetValue(column, out object actual) || !Same(expected[column], actual)) throw Rejected();
                    }
                    if (!pairs.Add((string)filter["primaryobjecttypecode"] + ":" + (string)message["name"])) throw Rejected();
                }
                // Check existence, not contents, of forbidden settings. Configuration may contain secrets or be very large.
                object[] stepIds = ids.Select(id => (object)id).ToArray();
                var settings = new QueryExpression("sdkmessageprocessingstep") { ColumnSet = new ColumnSet("sdkmessageprocessingstepid"), TopCount = 1 };
                settings.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, stepIds);
                var any = new FilterExpression(LogicalOperator.Or);
                foreach (string column in ForbiddenSettings) any.AddCondition(column, ConditionOperator.NotNull);
                settings.Criteria.AddFilter(any);
                RequireEmpty(service.RetrieveMultiple(settings));
                var images = new QueryExpression("sdkmessageprocessingstepimage") { ColumnSet = new ColumnSet("sdkmessageprocessingstepimageid"), TopCount = 1 };
                images.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, stepIds);
                RequireEmpty(service.RetrieveMultiple(images));
                return Array.AsReadOnly(ids.OrderBy(id => id).ToArray());
            }
            catch (Exception)
            {
                // Permission/configuration faults stay technical; no native fault details, enabling or repair.
                throw Rejected();
            }
        }

        private static void Canonical(Entity row, string table, Guid id)
        {
            if (row == null || row.LogicalName != table || id == Guid.Empty || row.Id != id || row.KeyAttributes.Count != 0) throw Rejected();
        }
        private static Guid Reference(Entity row, string column, string table)
        {
            if (!row.Attributes.TryGetValue(column, out object raw) || !(raw is EntityReference value) || value.LogicalName != table ||
                value.Id == Guid.Empty || value.KeyAttributes.Count != 0) throw Rejected();
            return value.Id;
        }
        private static bool Same(object expected, object actual)
        {
            if (actual == null || actual.GetType() != expected.GetType()) return false;
            if (expected is EntityReference a)
            {
                var b = (EntityReference)actual; return a.Id == b.Id && a.LogicalName == b.LogicalName && b.KeyAttributes.Count == 0;
            }
            if (expected is BooleanManagedProperty managed) return managed.Value == ((BooleanManagedProperty)actual).Value;
            return expected.Equals(actual);
        }
        private static void RequireEmpty(EntityCollection rows)
        {
            if (rows == null || rows.MoreRecords || rows.Entities.Count != 0) throw Rejected();
        }
        private static InvalidPluginExecutionException Rejected() => new InvalidPluginExecutionException(Failure);
    }
}
