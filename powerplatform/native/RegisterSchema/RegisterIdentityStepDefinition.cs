using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Construct disabled native registration metadata; never install, enable, or grant privileges.</summary>
    public static class RegisterIdentityStepDefinition
    {
        /// <summary>Native relationship for creating a step and both required images together.</summary>
        public const string ImageRelationship = "sdkmessageprocessingstepid_sdkmessageprocessingstepimage";

        /// <summary>
        /// Prepare one disabled step from inspected native handler/message/filter rows.
        /// A valid row shape is not proof of source, environment, assembly integrity or deployment approval.
        /// </summary>
        public static CreateRequest Build(Guid stepId, Guid beforeImageId, Guid afterImageId, int rank,
            Entity pluginType, Entity updateMessage, Entity tableFilter)
        {
            if (stepId == Guid.Empty || beforeImageId == Guid.Empty || afterImageId == Guid.Empty || beforeImageId == afterImageId || rank < 1)
                throw new ArgumentException("Stable step/image identifiers, distinct images and an explicit positive execution order are required.");
            RequireRow(pluginType, "plugintype"); RequireRow(updateMessage, "sdkmessage"); RequireRow(tableFilter, "sdkmessagefilter");
            RequireText(pluginType, "typename", typeof(ProtectRegisterIdentity).FullName!);
            RequireText(updateMessage, "name", "Update");
            RequireText(tableFilter, "primaryobjecttypecode", RegisterSchemaPlan.RecordTable);
            if (!tableFilter.Attributes.TryGetValue("sdkmessageid", out object message) || !(message is EntityReference reference) ||
                reference.LogicalName != "sdkmessage" || reference.Id != updateMessage.Id || reference.KeyAttributes.Count != 0)
                throw new ArgumentException("The exact Update filter and message binding is required.", nameof(tableFilter));
            if (!tableFilter.Attributes.TryGetValue("iscustomprocessingstepallowed", out object allowed) || !(allowed is bool permitted) || !permitted ||
                !tableFilter.Attributes.TryGetValue("availability", out object availability) || !(availability is int deployment) || (deployment != 0 && deployment != 2))
                throw new ArgumentException("A server-capable native filter permitting custom processing is required.", nameof(tableFilter));
            if (tableFilter.Attributes.TryGetValue("secondaryobjecttypecode", out object secondary) && secondary != null &&
                (!(secondary is string secondaryName) || secondaryName.Length != 0))
                throw new ArgumentException("A secondary-entity filter cannot register this single-table guard.", nameof(tableFilter));

            var step = new Entity("sdkmessageprocessingstep", stepId);
            step["name"] = "CareOps preserve credentialing record identity";
            step["description"] = "Disabled pending approved native image, rollback and bypass qualification.";
            step["eventhandler"] = new EntityReference("plugintype", pluginType.Id);
            step["sdkmessageid"] = new EntityReference("sdkmessage", updateMessage.Id);
            step["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", tableFilter.Id);
            step["rank"] = rank;
            step["stage"] = new OptionSetValue(40); // PostOperation: compare actual pre/post identity.
            step["mode"] = new OptionSetValue(0); // Synchronous; cannot detect a violation after commit.
            step["supporteddeployment"] = new OptionSetValue(0); // Server only.
            step["statecode"] = new OptionSetValue(1); // Explicitly DISABLED. Never an active protection receipt.
            step["statuscode"] = new OptionSetValue(2);
            step["canbebypassed"] = false; // Requested metadata, not a guarantee against administrators.
            step["asyncautodelete"] = false;
            step["iscustomizable"] = new BooleanManagedProperty(false);
            // No filtering attributes: all updates must preserve identity, including extension changes.
            // No impersonatinguserid: the guard needs no elevated service connection.
            var images = new EntityCollection();
            images.Entities.Add(Image(beforeImageId, ProtectRegisterIdentity.BeforeAlias, 0));
            images.Entities.Add(Image(afterImageId, ProtectRegisterIdentity.AfterAlias, 1));
            step.RelatedEntities.Add(new Relationship(ImageRelationship), images);
            var request = new CreateRequest { Target = step };
            request["SolutionUniqueName"] = RegisterSchemaPlan.Solution;
            return request;
        }

        private static Entity Image(Guid id, string alias, int type)
        {
            var image = new Entity("sdkmessageprocessingstepimage", id);
            image["name"] = alias;
            image["entityalias"] = alias;
            image["imagetype"] = new OptionSetValue(type);
            image["messagepropertyname"] = "Target";
            image["attributes"] = string.Join(",", ProtectRegisterIdentity.ImageColumns);
            image["iscustomizable"] = new BooleanManagedProperty(false);
            return image;
        }

        private static void RequireRow(Entity row, string logicalName)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.LogicalName != logicalName || row.Id == Guid.Empty || row.KeyAttributes.Count != 0)
                throw new ArgumentException("An inspected native metadata row with its canonical identifier is required.", nameof(row));
        }

        private static void RequireText(Entity row, string column, string expected)
        {
            if (!row.Attributes.TryGetValue(column, out object raw) || !(raw is string value) || value != expected)
                throw new ArgumentException("Exact native handler, message and table metadata are required.", nameof(row));
        }
    }
}
