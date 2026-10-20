using System.Collections.Generic;
using System.Collections.ObjectModel;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Native metadata for original human-confirmation source records, not a capture endpoint or installer.</summary>
    public static class ConfirmationSourceSchema
    {
        /// <summary>Decisions-owned standard source table. It is not another operational credentialing register.</summary>
        public const string Table = "dft_credentialingconfirmation";
        /// <summary>Exact supported capture representation; a matching string alone is not provenance.</summary>
        public const string Protocol = "careops-confirmation-source-v1";
        private static readonly string[] KeyColumns = { "dft_confirmationkey" };
        private static readonly string[] Columns = { "dft_confirmationkey", "dft_protocol", "dft_actorref", "dft_originaldigest",
            "dft_originaldocument", "dft_confirmedutc", "dft_currentuntilutc", "dft_function", "dft_pathway", "dft_policyref",
            "dft_fencetoken", "dft_recordedstate", "dft_withdrawn", "dft_record", "createdby", "createdonbehalfby", "statecode" };
        /// <summary>Fresh minimal source-read column list, including native creator and state checks.</summary>
        public static string[] ReadColumns => (string[])Columns.Clone();

        /// <summary>Construct fresh SDK metadata requests only; never connects, creates records or changes privileges.</summary>
        public static ReadOnlyCollection<OrganizationRequest> Build()
        {
            var requests = new List<OrganizationRequest>
            {
                new CreateEntityRequest
                {
                    Entity = new EntityMetadata { SchemaName = Table, DisplayName = new Label("Original credentialing confirmation", 1033),
                        DisplayCollectionName = new Label("Original credentialing confirmations", 1033), OwnershipType = OwnershipTypes.UserOwned,
                        IsActivity = false, IsAuditEnabled = new BooleanManagedProperty(true) },
                    // Primary names and alternate-key columns cannot use column-level security. These contain opaque IDs only.
                    PrimaryAttribute = Text("dft_name", "Confirmation identifier", 100, false), HasNotes = false, HasActivities = false,
                    HasFeedback = false, SolutionUniqueName = RegisterSchemaPlan.Solution
                }
            };
            Add(requests, Text("dft_confirmationkey", "Canonical confirmation key", 32, false));
            Add(requests, Text("dft_protocol", "Capture protocol", 48));
            Add(requests, Text("dft_actorref", "Original canonical decision-maker", 36));
            Add(requests, Text("dft_originaldigest", "Original full confirmation digest", 64));
            Add(requests, Describe(new MemoAttributeMetadata { MaxLength = DecisionIntentDocument.MaximumBytes }, "dft_originaldocument", "Original confirmed intent"));
            Add(requests, Text("dft_confirmedutc", "Original confirmation UTC instant", 28));
            Add(requests, Text("dft_currentuntilutc", "Confirmation current until UTC", 28));
            Add(requests, Text("dft_function", "Confirmed policy function", 256));
            Add(requests, Text("dft_pathway", "Confirmed pathway", 256));
            Add(requests, Text("dft_policyref", "Approved outcome-rule version reference", 256));
            Add(requests, Text("dft_fencetoken", "Technical transaction fence token", 32));
            var options = new OptionSetMetadata { IsGlobal = false, OptionSetType = OptionSetType.Picklist };
            string[] labels = { "Unverified - not authority", "Granted", "Suspended", "Revoked", "Refused" };
            for (int i = 0; i < labels.Length; i++) options.Options.Add(new OptionMetadata(new Label(labels[i], 1033), RegisterSchemaPlan.Unverified + i));
            Add(requests, Describe(new PicklistAttributeMetadata { OptionSet = options, DefaultFormValue = RegisterSchemaPlan.Unverified },
                "dft_recordedstate", "Original confirmed outcome"));
            Add(requests, Describe(new BooleanAttributeMetadata
            {
                DefaultValue = true,
                OptionSet = new BooleanOptionSetMetadata { TrueOption = new OptionMetadata(new Label("Not available for recording", 1033), 1),
                    FalseOption = new OptionMetadata(new Label("Subject to original-source checks", 1033), 0) }
            }, "dft_withdrawn", "Withdrawn or not released for recording"));
            var lookup = new LookupAttributeMetadata { SchemaName = "dft_record", DisplayName = new Label("Credentialing record", 1033),
                RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired),
                IsAuditEnabled = new BooleanManagedProperty(true), IsSecured = false };
            requests.Add(new CreateOneToManyRequest
            {
                Lookup = lookup, SolutionUniqueName = RegisterSchemaPlan.Solution,
                OneToManyRelationship = new OneToManyRelationshipMetadata
                {
                    SchemaName = "dft_record_confirmations", ReferencedEntity = RegisterSchemaPlan.RecordTable, ReferencingEntity = Table,
                    AssociatedMenuConfiguration = new AssociatedMenuConfiguration { Behavior = AssociatedMenuBehavior.DoNotDisplay },
                    CascadeConfiguration = new CascadeConfiguration { Delete = CascadeType.Restrict, Assign = CascadeType.NoCascade,
                        Merge = CascadeType.NoCascade, Reparent = CascadeType.NoCascade, Share = CascadeType.NoCascade, Unshare = CascadeType.NoCascade }
                }
            });
            requests.Add(new CreateEntityKeyRequest { EntityName = Table, SolutionUniqueName = RegisterSchemaPlan.Solution,
                EntityKey = new EntityKeyMetadata { SchemaName = "dft_confirmation_key", DisplayName = new Label("Canonical confirmation key", 1033),
                    KeyAttributes = (string[])KeyColumns.Clone() } });
            return requests.AsReadOnly();
        }
        private static void Add(List<OrganizationRequest> requests, AttributeMetadata column) => requests.Add(
            new CreateAttributeRequest { EntityName = Table, Attribute = column, SolutionUniqueName = RegisterSchemaPlan.Solution });
        private static StringAttributeMetadata Text(string name, string label, int maximum, bool secured = true) =>
            Describe(new StringAttributeMetadata { MaxLength = maximum, FormatName = StringFormatName.Text }, name, label, secured);
        private static T Describe<T>(T column, string name, string label, bool secured = true) where T : AttributeMetadata
        {
            column.SchemaName = name; column.DisplayName = new Label(label, 1033);
            column.RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired);
            column.IsAuditEnabled = new BooleanManagedProperty(true); column.IsSecured = secured; return column;
        }
    }
}
