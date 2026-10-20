using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Exact disabled registration for final-history mutation denial, not an installer.</summary>
    public static class HistoryMutationStepDefinition
    {
        /// <summary>
        /// Build one of the four required bindings: Update/Delete of decision/event.
        /// Inputs must be retrieved from the approved environment; shape checking is not provenance.
        /// All four active registrations require separate native acceptance before production.
        /// </summary>
        public static CreateRequest Build(Guid stepId, int rank, Entity pluginType, Entity message, Entity tableFilter)
        {
            if (stepId == Guid.Empty || rank < 1)
                throw new ArgumentException("A stable step identifier and an explicit positive execution order are required.");
            RequireRow(pluginType, "plugintype"); RequireRow(message, "sdkmessage"); RequireRow(tableFilter, "sdkmessagefilter");
            if (!pluginType.Attributes.TryGetValue("typename", out object name) || !(name is string handler) ||
                handler != typeof(RejectHistoryMutation).FullName)
                throw new ArgumentException("The exact immutable-history handler is required.", nameof(pluginType));
            if (!message.Attributes.TryGetValue("name", out object verb) || !(verb is string operation) ||
                (operation != "Update" && operation != "Delete"))
                throw new ArgumentException("Only exact Update or Delete metadata is supported.", nameof(message));
            if (!tableFilter.Attributes.TryGetValue("primaryobjecttypecode", out object code) || !(code is string table) ||
                (table != RegisterSchemaPlan.DecisionTable && table != DecisionEventSchema.Table))
                throw new ArgumentException("Only final decision and immutable event tables are supported.", nameof(tableFilter));
            if (!tableFilter.Attributes.TryGetValue("sdkmessageid", out object binding) || !(binding is EntityReference reference) ||
                reference.LogicalName != "sdkmessage" || reference.Id != message.Id || reference.KeyAttributes.Count != 0)
                throw new ArgumentException("The filter must reference this exact native message.", nameof(tableFilter));
            if (!tableFilter.Attributes.TryGetValue("iscustomprocessingstepallowed", out object allowed) || !(allowed is bool permitted) || !permitted ||
                !tableFilter.Attributes.TryGetValue("availability", out object available) || !(available is int deployment) || (deployment != 0 && deployment != 2))
                throw new ArgumentException("An inspected server-capable filter permitting custom processing is required.", nameof(tableFilter));
            if (tableFilter.Attributes.TryGetValue("secondaryobjecttypecode", out object secondary) && secondary != null &&
                (!(secondary is string secondaryTable) || secondaryTable.Length != 0))
                throw new ArgumentException("A secondary-table filter is not supported.", nameof(tableFilter));

            var step = new Entity("sdkmessageprocessingstep", stepId);
            step["name"] = "CareOps preserve " + table + " on " + operation;
            step["description"] = "Disabled pending qualified append-only history and records-management controls.";
            step["eventhandler"] = new EntityReference("plugintype", pluginType.Id);
            step["sdkmessageid"] = new EntityReference("sdkmessage", message.Id);
            step["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", tableFilter.Id);
            step["rank"] = rank;
            step["stage"] = new OptionSetValue(10); // Reject early, before normal database work.
            step["mode"] = new OptionSetValue(0);
            step["supporteddeployment"] = new OptionSetValue(0);
            step["statecode"] = new OptionSetValue(1); // Source metadata is NOT enabled protection.
            step["statuscode"] = new OptionSetValue(2);
            step["canbebypassed"] = false;
            step["asyncautodelete"] = false;
            step["iscustomizable"] = new BooleanManagedProperty(false);
            // No images, filtering attributes, impersonation, caller-configurable exemption or credentials.
            var request = new CreateRequest { Target = step };
            request["SolutionUniqueName"] = RegisterSchemaPlan.Solution;
            return request;
        }

        private static void RequireRow(Entity row, string expected)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.LogicalName != expected || row.Id == Guid.Empty || row.KeyAttributes.Count != 0)
                throw new ArgumentException("A canonical inspected native metadata row is required.", nameof(row));
        }
    }
}
