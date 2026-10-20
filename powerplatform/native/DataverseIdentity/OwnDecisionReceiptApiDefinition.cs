using System;
using System.Linq;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Native metadata for the single historical-receipt reader. No connection, registration or permission grant.</summary>
    public static class OwnDecisionReceiptApiDefinition
    {
        /// <summary>Native relationship for the one explicit DecisionId input; Target is platform-generated.</summary>
        public const string RequestRelationship = "customapi_customapirequestparameter";
        /// <summary>Native relationship for the nine primitive receipt fields.</summary>
        public const string ResponseRelationship = "customapi_customapiresponseproperty";
        private const string PrivilegeCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";

        /// <summary>Construct metadata only from canonical inspected type/privilege rows. Row shape is not provenance.</summary>
        public static CreateRequest Build(Guid apiId, Entity pluginType, Entity executePrivilege)
        {
            if (apiId == Guid.Empty) throw new ArgumentException("A stable API identifier is required.", nameof(apiId));
            RequireRow(pluginType, "plugintype"); RequireRow(executePrivilege, "privilege");
            if (!pluginType.Attributes.TryGetValue("typename", out object type) || !(type is string name) ||
                name != typeof(ReadOwnDecisionReceipt).FullName)
                throw new ArgumentException("The exact inspected receipt-reader type is required.", nameof(pluginType));
            if (!executePrivilege.Attributes.TryGetValue("name", out object value) || !(value is string privilege) ||
                privilege.Length <= 3 || privilege.Length > 100 || !privilege.StartsWith("prv", StringComparison.Ordinal) ||
                privilege.Any(c => PrivilegeCharacters.IndexOf(c) < 0))
                throw new ArgumentException("An explicit existing execute privilege is required.", nameof(executePrivilege));

            var api = new Entity("customapi", apiId);
            api["uniquename"] = ReadOwnDecisionReceipt.ApiName; api["name"] = ReadOwnDecisionReceipt.ApiName;
            api["displayname"] = "Read own recorded credentialing decision receipt";
            api["description"] = "Historical receipt only; unavailable does not prove a save failed. Never reapprove or infer current scope.";
            api["bindingtype"] = new OptionSetValue(1); api["boundentitylogicalname"] = RegisterSchemaPlan.RecordTable;
            api["allowedcustomprocessingsteptype"] = new OptionSetValue(0);
            api["isfunction"] = false; api["isprivate"] = true; // Discovery setting, not a security or activation control.
            api["workflowsdkstepenabled"] = false; api["iscustomizable"] = new BooleanManagedProperty(false);
            api["plugintypeid"] = new EntityReference("plugintype", pluginType.Id);
            api["executeprivilegename"] = privilege;

            var input = Parameter("customapirequestparameter", "DecisionId", 12);
            input["isoptional"] = false;
            api.RelatedEntities.Add(new Relationship(RequestRelationship), new EntityCollection(new[] { input }));
            var responses = new EntityCollection();
            responses.Entities.Add(Parameter("customapiresponseproperty", "RecordId", 12));
            responses.Entities.Add(Parameter("customapiresponseproperty", "DecisionId", 12));
            responses.Entities.Add(Parameter("customapiresponseproperty", "RecordedAtUtc", 1));
            responses.Entities.Add(Parameter("customapiresponseproperty", "Sequence", 7));
            responses.Entities.Add(Parameter("customapiresponseproperty", "RecordedOutcome", 7));
            responses.Entities.Add(Parameter("customapiresponseproperty", "CaseHash", 10));
            responses.Entities.Add(Parameter("customapiresponseproperty", "RetrievedAtUtc", 1));
            responses.Entities.Add(Parameter("customapiresponseproperty", "ReadDisposition", 10));
            responses.Entities.Add(Parameter("customapiresponseproperty", "ReadSucceeded", 0));
            api.RelatedEntities.Add(new Relationship(ResponseRelationship), responses);
            var request = new CreateRequest { Target = api }; request["SolutionUniqueName"] = RegisterSchemaPlan.Solution;
            return request;
        }

        private static Entity Parameter(string table, string name, int type)
        {
            var row = new Entity(table);
            row["uniquename"] = name; row["name"] = ReadOwnDecisionReceipt.ApiName + "." + name;
            row["displayname"] = name;
            row["description"] = "Historical receipt contract; retain its disposition and current caller access restrictions.";
            row["type"] = new OptionSetValue(type); row["iscustomizable"] = new BooleanManagedProperty(false);
            return row;
        }
        private static void RequireRow(Entity row, string table)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.LogicalName != table || row.Id == Guid.Empty || row.KeyAttributes.Count != 0)
                throw new ArgumentException("A canonical inspected native metadata row is required.", nameof(row));
        }
    }
}
