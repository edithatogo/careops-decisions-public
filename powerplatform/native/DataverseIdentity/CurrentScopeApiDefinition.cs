using System;
using System.Linq;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Build one native API definition with its response contract. Does not connect, register or grant access.</summary>
    public static class CurrentScopeApiDefinition
    {
        /// <summary>SDK relationship used for a single deep-create definition, not a second runtime endpoint.</summary>
        public const string ResponseRelationship = "customapi_customapiresponseproperty";
        private const string PrivilegeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";

        /// <summary>
        /// Prepare metadata from previously inspected plug-in and privilege rows in an approved environment.
        /// Row shape validation is not provenance, package verification or approval to execute this request.
        /// </summary>
        public static CreateRequest Build(Guid apiId, Entity pluginType, Entity executePrivilege)
        {
            if (apiId == Guid.Empty) throw new ArgumentException("A stable API identifier is required.", nameof(apiId));
            RequireRow(pluginType, "plugintype");
            RequireRow(executePrivilege, "privilege");
            if (!pluginType.Attributes.TryGetValue("typename", out object type) ||
                !(type is string name) || name != typeof(ReadCurrentRecordedScope).FullName)
                throw new ArgumentException("The exact inspected current-scope plug-in type is required.", nameof(pluginType));
            if (!executePrivilege.Attributes.TryGetValue("name", out object privilege) || !(privilege is string privilegeName) ||
                privilegeName.Length <= 3 || privilegeName.Length > 100 || !privilegeName.StartsWith("prv", StringComparison.Ordinal) ||
                privilegeName.Any(c => PrivilegeCharacters.IndexOf(c) < 0))
                throw new ArgumentException("An explicit existing execute privilege is required.", nameof(executePrivilege));

            var api = new Entity("customapi", apiId);
            api["uniquename"] = ReadCurrentRecordedScope.ApiName;
            api["name"] = ReadCurrentRecordedScope.ApiName;
            api["displayname"] = "Read current recorded credentialing scope";
            api["description"] = "Caller-bound recorded history and verification currentness; not practice clearance.";
            api["bindingtype"] = new OptionSetValue(1); // Entity; the platform supplies implicit Target.
            api["boundentitylogicalname"] = RegisterSchemaPlan.RecordTable;
            api["allowedcustomprocessingsteptype"] = new OptionSetValue(0); // No extension steps.
            api["isfunction"] = false;
            api["isprivate"] = true; // Metadata visibility only; never an authorisation control.
            api["workflowsdkstepenabled"] = false;
            api["iscustomizable"] = new BooleanManagedProperty(false);
            api["plugintypeid"] = new EntityReference("plugintype", pluginType.Id);
            api["executeprivilegename"] = privilegeName;

            var responses = new EntityCollection();
            Add(responses, "ReadSucceeded", 0);
            Add(responses, "RootVersion", 10);
            Add(responses, "AsOfUtc", 1);
            Add(responses, "CurrentnessVerified", 0);
            Add(responses, "RecordedStatus", 10);
            Add(responses, "LatestSequence", 7);
            Add(responses, "SelectedSequence", 7);
            Add(responses, "HasEffectiveDecision", 0);
            Add(responses, "SelectedDecision", 3, RegisterSchemaPlan.DecisionTable);
            Add(responses, "ReadDisposition", 10);
            api.RelatedEntities.Add(new Relationship(ResponseRelationship), responses);
            var request = new CreateRequest { Target = api };
            request["SolutionUniqueName"] = RegisterSchemaPlan.Solution;
            return request;
        }

        private static void Add(EntityCollection rows, string name, int type, string? table = null)
        {
            var row = new Entity("customapiresponseproperty");
            row["uniquename"] = name;
            row["name"] = ReadCurrentRecordedScope.ApiName + "." + name;
            row["displayname"] = name;
            row["description"] = "Recorded-scope response field; retain the full response interpretation.";
            row["type"] = new OptionSetValue(type);
            row["iscustomizable"] = new BooleanManagedProperty(false);
            if (table != null) row["logicalentityname"] = table;
            rows.Entities.Add(row);
        }

        private static void RequireRow(Entity row, string table)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.LogicalName != table || row.Id == Guid.Empty || row.KeyAttributes.Count != 0)
                throw new ArgumentException("An inspected native row with its canonical identifier is required.", nameof(row));
        }
    }
}
