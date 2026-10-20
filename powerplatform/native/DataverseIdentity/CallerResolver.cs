using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>A mapping resolved under caller access; not delegated decision authority.</summary>
    public sealed class CallerPerson
    {
        internal CallerPerson(Guid organization, Guid user, Guid directoryObject, Guid person)
        { Organization = organization; PlatformUser = user; DirectoryObject = directoryObject; Person = person; }
        /// <summary>Dataverse organization from the execution context and WhoAmI.</summary>
        public Guid Organization { get; }
        /// <summary>Dataverse systemuser identifier; do not compare it directly to practitioner IDs.</summary>
        public Guid PlatformUser { get; }
        /// <summary>Directory object identifier read from systemuser, not supplied by the caller.</summary>
        public Guid DirectoryObject { get; }
        /// <summary>Canonical dft_orgperson identifier to compare with the case practitioner.</summary>
        public Guid Person { get; }
    }

    /// <summary>The resolved caller and the SAME verified caller-scoped service, not a service-account fallback.</summary>
    public sealed class CallerBinding
    {
        internal CallerBinding(CallerPerson caller, IOrganizationService service) { Caller = caller; Service = service; }
        /// <summary>Canonical person and native identity already resolved by CallerResolver.</summary>
        public CallerPerson Caller { get; }
        /// <summary>Exact service used for WhoAmI/person resolution; reuse it through the transaction.</summary>
        public IOrganizationService Service { get; }
    }

    /// <summary>Read-only Dataverse SDK binding for direct interactive human operations.</summary>
    public static class CallerResolver
    {
        /// <summary>Existing Decisions-owned person table, not a sibling-module dependency.</summary>
        public const string PersonTable = "dft_orgperson";
        /// <summary>Existing string column, verified against the authenticated directory object.</summary>
        public const string DirectoryColumn = "dft_aadobjectid";
        /// <summary>Required published active single-column key. Provision and verify separately.</summary>
        public const string IdentityKey = "dft_orgperson_aadidentity";

        /// <summary>Backward-compatible caller-only read, with all existing identity checks preserved.</summary>
        public static CallerPerson Resolve(IPluginExecutionContext context, IOrganizationServiceFactory factory) => ResolveBinding(context, factory).Caller;

        /// <summary>Resolve identity and return the same scoped service without duplicate connection or user lookup.</summary>
        public static CallerBinding ResolveBinding(IPluginExecutionContext context, IOrganizationServiceFactory factory)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (context.OrganizationId == Guid.Empty || context.InitiatingUserId == Guid.Empty || context.UserId != context.InitiatingUserId)
                throw Denied("CallerContextMismatch");
            IOrganizationService service = factory.CreateOrganizationService(context.InitiatingUserId);
            if (service == null) throw Denied("CallerServiceUnavailable");
            if (!(service.Execute(new WhoAmIRequest()) is WhoAmIResponse who) ||
                who.UserId != context.InitiatingUserId || who.OrganizationId != context.OrganizationId)
                throw Denied("CallerServiceMismatch");
            Entity user = service.Retrieve("systemuser", who.UserId,
                new ColumnSet("isdisabled", "accessmode", "applicationid", "azureactivedirectoryobjectid"));
            if (user == null || user.LogicalName != "systemuser" || user.Id != who.UserId ||
                !user.Attributes.TryGetValue("isdisabled", out object disabled) || !(disabled is bool enabledFlag) || enabledFlag ||
                !user.Attributes.TryGetValue("accessmode", out object access) || !(access is OptionSetValue mode) || mode.Value != 0)
                throw Denied("InteractiveCallerUnverified");
            if (user.Attributes.TryGetValue("applicationid", out object application) && application != null &&
                (!(application is Guid applicationId) || applicationId != Guid.Empty))
                throw Denied("ApplicationCallerNotHuman");
            if (!user.Attributes.TryGetValue("azureactivedirectoryobjectid", out object identity) ||
                !(identity is Guid directoryId) || directoryId == Guid.Empty)
                throw Denied("DirectoryIdentityUnverified");
            var request = new RetrieveEntityKeyRequest { EntityLogicalName = PersonTable, LogicalName = IdentityKey, RetrieveAsIfPublished = false };
            if (!(service.Execute(request) is RetrieveEntityKeyResponse keyResponse)) throw Denied("IdentityIndexUnavailable");
            EntityKeyMetadata key = keyResponse.EntityKeyMetadata;
            if (key == null || key.EntityLogicalName != PersonTable || key.LogicalName != IdentityKey ||
                key.EntityKeyIndexStatus != EntityKeyIndexStatus.Active || key.KeyAttributes == null ||
                key.KeyAttributes.Length != 1 || key.KeyAttributes[0] != DirectoryColumn)
                throw Denied("IdentityIndexUnverified");
            var query = new QueryExpression(PersonTable) { ColumnSet = new ColumnSet(DirectoryColumn, "statecode"), TopCount = 2 };
            query.Criteria.AddCondition(DirectoryColumn, ConditionOperator.Equal, directoryId.ToString("D"));
            EntityCollection rows = service.RetrieveMultiple(query);
            if (rows == null || rows.MoreRecords || rows.Entities.Count != 1) throw Denied("PersonMappingMissingOrAmbiguous");
            Entity person = rows.Entities[0];
            if (person == null || person.LogicalName != PersonTable || person.Id == Guid.Empty ||
                !person.Attributes.TryGetValue("statecode", out object state) || !(state is OptionSetValue status) || status.Value != 0 ||
                !person.Attributes.TryGetValue(DirectoryColumn, out object value) || !(value is string text) ||
                !Guid.TryParseExact(text, "D", out Guid mapped) || mapped != directoryId)
                throw Denied("PersonMappingUnverified");
            return new CallerBinding(new CallerPerson(who.OrganizationId, who.UserId, directoryId, person.Id), service);
        }
        private static InvalidPluginExecutionException Denied(string code) => new InvalidPluginExecutionException("CareOps identity check: " + code);
    }
}
