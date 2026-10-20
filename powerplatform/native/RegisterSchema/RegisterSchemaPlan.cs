using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Native SDK metadata requests for reviewed sandbox provisioning; never connects or executes.</summary>
    public static class RegisterSchemaPlan
    {
        /// <summary>Exact unique name from the existing solution manifest, not its folder/display name.</summary>
        public const string Solution = "decisionsworkspace";
        /// <summary>Stable register identity and current operational projection, owned by Decisions.</summary>
        public const string RecordTable = "dft_credentialingrecord";
        /// <summary>Decision history, with separate evidence access and explicit supersession.</summary>
        public const string DecisionTable = "dft_credentialingdecision";
        /// <summary>Non-authorising initial choice, using the existing publisher option prefix.</summary>
        public const int Unverified = 602170000;

        /// <summary>Build fresh, ordered metadata objects. No service, credentials, writes or records are accepted.</summary>
        public static ReadOnlyCollection<OrganizationRequest> Build()
        {
            var requests = new List<OrganizationRequest>
            {
                Table(RecordTable, "Credentialing record", "Credentialing register"),
                Table(DecisionTable, "Credentialing decision", "Credentialing decision history")
            };
            Add(requests, RecordTable, Text("recordkey", "Canonical record key", 64, true));
            Add(requests, RecordTable, Text("practitionerref", "Practitioner source reference", 256, true));
            Add(requests, RecordTable, Text("professionref", "Profession reference", 256, true));
            Add(requests, RecordTable, Text("facilityref", "Facility reference", 256, true));
            Projection(requests, RecordTable);
            Add(requests, RecordTable, Instant("sourcecheckedat", "Source last checked", false));
            Add(requests, RecordTable, Instant("sourcecurrentuntil", "Source verification valid until", false));
            Add(requests, RecordTable, Instant("projectedasof", "Projection as of", false));
            Add(requests, RecordTable, Number("latestsequence", "Latest recorded sequence", 0));
            Add(requests, RecordTable, Number("projectionsequence", "Projected decision sequence", 0));

            Add(requests, DecisionTable, Text("decisionkey", "Canonical decision key", 64, true));
            Add(requests, DecisionTable, Text("actorref", "Authenticated decision-maker reference", 256, true, true));
            Add(requests, DecisionTable, Text("function", "Delegated function", 256, true));
            Add(requests, DecisionTable, Text("profileversionref", "Delegation release reference", 256, true, true));
            Add(requests, DecisionTable, Text("profilehash", "Exact delegation content hash", 64, true, true));
            Add(requests, DecisionTable, Text("casehash", "Exact case and evidence content hash", 64, true, true));
            Add(requests, DecisionTable, Text("decisionevidenceref", "Decision evidence reference", 256, true, true));
            Add(requests, DecisionTable, Text("authorityevidenceref", "Authority evidence reference", 256, true, true));
            Projection(requests, DecisionTable);
            Add(requests, DecisionTable, Instant("effectiveat", "Decision effective at", true));
            Add(requests, DecisionTable, Instant("recordedat", "Decision recorded at", true));
            Add(requests, DecisionTable, Number("sequence", "Per-record decision sequence", 1));

            requests.Add(Relation("dft_record_decisions", RecordTable, DecisionTable, "record", "Credentialing record", true));
            requests.Add(Relation("dft_decision_supersession", DecisionTable, DecisionTable, "supersedes", "Superseded decision", false));
            requests.Add(Relation("dft_record_latestdecision", DecisionTable, RecordTable, "latestdecision", "Latest recorded decision", false));
            requests.Add(Key(RecordTable, "dft_credentialingrecord_key", "Canonical record key", "dft_recordkey"));
            requests.Add(Key(DecisionTable, "dft_credentialingdecision_key", "Canonical decision key", "dft_decisionkey"));
            requests.Add(Key(DecisionTable, "dft_credentialingdecision_sequence", "Record and sequence", "dft_record", "dft_sequence"));
            return requests.AsReadOnly();
        }

        /// <summary>Read back actual published metadata after approved provisioning; execution is not supplied here.</summary>
        public static ReadOnlyCollection<RetrieveEntityRequest> InspectionRequests() => Array.AsReadOnly(new[]
        {
            new RetrieveEntityRequest { LogicalName = RecordTable, EntityFilters = EntityFilters.All, RetrieveAsIfPublished = false },
            new RetrieveEntityRequest { LogicalName = DecisionTable, EntityFilters = EntityFilters.All, RetrieveAsIfPublished = false }
        });

        private static CreateEntityRequest Table(string name, string label, string plural) => new CreateEntityRequest
        {
            Entity = new EntityMetadata
            {
                SchemaName = name, DisplayName = new Label(label, 1033), DisplayCollectionName = new Label(plural, 1033),
                Description = new Label("Decisions-owned credentialing data; protected API and native qualification required before use.", 1033),
                OwnershipType = OwnershipTypes.UserOwned, IsActivity = false,
                IsAuditEnabled = new BooleanManagedProperty(true)
            },
            PrimaryAttribute = Text("name", "Record identifier", 100, true),
            HasNotes = false, HasActivities = false, HasFeedback = false, SolutionUniqueName = Solution
        };

        private static void Add(List<OrganizationRequest> requests, string table, AttributeMetadata column) =>
            requests.Add(new CreateAttributeRequest { EntityName = table, Attribute = column, SolutionUniqueName = Solution });

        private static void Projection(List<OrganizationRequest> requests, string table)
        {
            Add(requests, table, Text("scopeversionref", "Approved scope version reference", 256, true));
            Add(requests, table, Describe(new MemoAttributeMetadata { MaxLength = 32768 }, "scopetext", "Scope of clinical practice", true));
            Add(requests, table, Describe(new MemoAttributeMetadata { MaxLength = 32768 }, "conditionstext", "Conditions and limitations", false));
            Add(requests, table, Describe(new MemoAttributeMetadata { MaxLength = 32768 }, "supervisiontext", "Supervision requirements", false));
            Add(requests, table, Instant("validfrom", "Scope valid from", true));
            Add(requests, table, Instant("validuntil", "Scope valid until", true));
            var options = new OptionSetMetadata { IsGlobal = false, OptionSetType = OptionSetType.Picklist };
            string[] states = { "Unverified - not authority", "Granted", "Suspended", "Revoked", "Refused" };
            for (int i = 0; i < states.Length; i++) options.Options.Add(new OptionMetadata(new Label(states[i], 1033), Unverified + i));
            Add(requests, table, Describe(new PicklistAttributeMetadata { OptionSet = options, DefaultFormValue = Unverified },
                "recordedstate", "Recorded decision state", true));
        }

        private static T Describe<T>(T column, string name, string label, bool required, bool secured = false) where T : AttributeMetadata
        {
            column.SchemaName = "dft_" + name; column.DisplayName = new Label(label, 1033);
            column.RequiredLevel = new AttributeRequiredLevelManagedProperty(required ? AttributeRequiredLevel.ApplicationRequired : AttributeRequiredLevel.None);
            column.IsAuditEnabled = new BooleanManagedProperty(true); column.IsSecured = secured;
            return column;
        }

        private static StringAttributeMetadata Text(string name, string label, int length, bool required, bool secured = false) =>
            Describe(new StringAttributeMetadata { MaxLength = length, FormatName = StringFormatName.Text }, name, label, required, secured);

        private static DateTimeAttributeMetadata Instant(string name, string label, bool required) =>
            Describe(new DateTimeAttributeMetadata { Format = DateTimeFormat.DateAndTime, DateTimeBehavior = DateTimeBehavior.UserLocal }, name, label, required);

        private static IntegerAttributeMetadata Number(string name, string label, int minimum) =>
            Describe(new IntegerAttributeMetadata { MinValue = minimum, MaxValue = int.MaxValue }, name, label, true);

        private static CreateOneToManyRequest Relation(string schema, string parent, string child, string column, string label, bool required) =>
            new CreateOneToManyRequest
            {
                Lookup = Describe(new LookupAttributeMetadata(), column, label, required),
                OneToManyRelationship = new OneToManyRelationshipMetadata
                {
                    SchemaName = schema, ReferencedEntity = parent, ReferencingEntity = child,
                    AssociatedMenuConfiguration = new AssociatedMenuConfiguration { Behavior = AssociatedMenuBehavior.DoNotDisplay },
                    CascadeConfiguration = new CascadeConfiguration
                    {
                        Delete = CascadeType.Restrict, Assign = CascadeType.NoCascade, Merge = CascadeType.NoCascade,
                        Reparent = CascadeType.NoCascade, Share = CascadeType.NoCascade, Unshare = CascadeType.NoCascade
                    }
                },
                SolutionUniqueName = Solution
            };

        private static CreateEntityKeyRequest Key(string table, string schema, string label, params string[] columns) =>
            new CreateEntityKeyRequest
            {
                EntityName = table, EntityKey = new EntityKeyMetadata { SchemaName = schema, DisplayName = new Label(label, 1033), KeyAttributes = columns },
                SolutionUniqueName = Solution
            };
    }
}
