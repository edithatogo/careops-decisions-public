using System;
using System.Collections.ObjectModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Inert native schema requests for a Decisions-owned immutable event journal.</summary>
    public static class DecisionEventSchema
    {
        /// <summary>One stored event per recorded decision; consumers must own separate acknowledgement state.</summary>
        public const string Table = "dft_credentialingevent";
        /// <summary>A recorded event is not scope activation, permission or a command to send a message.</summary>
        public const string EventType = "credentialing.decision.recorded.v1";
        /// <summary>Current immutable envelope version.</summary>
        public const int Version = 1;
        private static readonly string[] EventKeyColumns = { "dft_eventkey" };

        /// <summary>Build fresh metadata only; no tenant provisioning, flow trigger or record creation occurs.</summary>
        public static ReadOnlyCollection<OrganizationRequest> Build() => Array.AsReadOnly(new OrganizationRequest[]
        {
            new CreateEntityRequest
            {
                Entity = new EntityMetadata
                {
                    SchemaName = Table, DisplayName = new Label("Credentialing decision event", 1033),
                    DisplayCollectionName = new Label("Credentialing decision events", 1033),
                    Description = new Label("Restricted immutable outbox envelope; not authority or a notification instruction.", 1033),
                    OwnershipType = OwnershipTypes.UserOwned, IsActivity = false, IsAuditEnabled = new BooleanManagedProperty(true)
                },
                PrimaryAttribute = Text("name", "Event identifier", 36),
                HasNotes = false, HasActivities = false, HasFeedback = false, SolutionUniqueName = RegisterSchemaPlan.Solution
            },
            Column(Text("eventkey", "Canonical event key", 32)),
            Column(Text("eventtype", "Event contract", 80)),
            Column(Text("organizationref", "Origin organization reference", 32)),
            Column(Describe(new IntegerAttributeMetadata { MinValue = Version, MaxValue = Version }, "eventversion", "Envelope version")),
            Column(Describe(new IntegerAttributeMetadata { MinValue = 1, MaxValue = int.MaxValue }, "sequence", "Decision sequence")),
            Column(Describe(new DateTimeAttributeMetadata { Format = DateTimeFormat.DateAndTime, DateTimeBehavior = DateTimeBehavior.UserLocal }, "recordedat", "Original recording instant")),
            Relation(RegisterSchemaPlan.RecordTable, "record", "Credentialing record"),
            Relation(RegisterSchemaPlan.DecisionTable, "decision", "Recorded decision"),
            new CreateEntityKeyRequest
            {
                EntityName = Table,
                EntityKey = new EntityKeyMetadata { SchemaName = "dft_credentialingevent_key", DisplayName = new Label("Canonical event key", 1033), KeyAttributes = (string[])EventKeyColumns.Clone() },
                SolutionUniqueName = RegisterSchemaPlan.Solution
            }
        });

        /// <summary>Inspect published schema and active key state during separately approved native qualification.</summary>
        public static RetrieveEntityRequest InspectionRequest() => new RetrieveEntityRequest
        {
            LogicalName = Table, EntityFilters = EntityFilters.All, RetrieveAsIfPublished = false
        };

        private static T Describe<T>(T field, string name, string label) where T : AttributeMetadata
        {
            field.SchemaName = "dft_" + name; field.DisplayName = new Label(label, 1033);
            field.RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired);
            field.IsAuditEnabled = new BooleanManagedProperty(true);
            return field;
        }
        private static StringAttributeMetadata Text(string name, string label, int length) =>
            Describe(new StringAttributeMetadata { MaxLength = length, FormatName = StringFormatName.Text }, name, label);
        private static CreateAttributeRequest Column(AttributeMetadata field) => new CreateAttributeRequest
        {
            EntityName = Table, Attribute = field, SolutionUniqueName = RegisterSchemaPlan.Solution
        };
        private static CreateOneToManyRequest Relation(string parent, string field, string label) => new CreateOneToManyRequest
        {
            Lookup = Describe(new LookupAttributeMetadata(), field, label),
            OneToManyRelationship = new OneToManyRelationshipMetadata
            {
                SchemaName = "dft_event_" + field, ReferencedEntity = parent, ReferencingEntity = Table,
                AssociatedMenuConfiguration = new AssociatedMenuConfiguration { Behavior = AssociatedMenuBehavior.DoNotDisplay },
                CascadeConfiguration = new CascadeConfiguration
                {
                    Delete = CascadeType.Restrict, Assign = CascadeType.NoCascade, Merge = CascadeType.NoCascade,
                    Reparent = CascadeType.NoCascade, Share = CascadeType.NoCascade, Unshare = CascadeType.NoCascade
                }
            },
            SolutionUniqueName = RegisterSchemaPlan.Solution
        };
    }
}
