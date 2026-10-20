using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Caller-scoped recorded-register read; never evaluates or grants permission to practise.</summary>
    public sealed class ReadCredentialingRecord : IPlugin
    {
        /// <summary>Versioned bound Custom API name; registration is a separate approved operation.</summary>
        public const string ApiName = "dft_ReadCredentialingRecordV1";
        /// <summary>Decisions-owned projection table, not the confidential decision-evidence table.</summary>
        public const string Table = "dft_credentialingrecord";
        /// <summary>Mandatory consumer interpretation; successful retrieval is not clearance.</summary>
        public const string Disposition = "recorded-data-only-not-practice-clearance";
        private static readonly ReadOnlyDictionary<string, Type> Fields = new ReadOnlyDictionary<string, Type>(
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                ["dft_recordkey"] = typeof(string), ["dft_practitionerref"] = typeof(string),
                ["dft_professionref"] = typeof(string), ["dft_facilityref"] = typeof(string),
                ["dft_scopeversionref"] = typeof(string), ["dft_scopetext"] = typeof(string),
                ["dft_conditionstext"] = typeof(string), ["dft_supervisiontext"] = typeof(string),
                ["dft_validfrom"] = typeof(DateTime), ["dft_validuntil"] = typeof(DateTime),
                ["dft_recordedstate"] = typeof(OptionSetValue),
                ["dft_sourcecheckedat"] = typeof(DateTime), ["dft_sourcecurrentuntil"] = typeof(DateTime),
                ["dft_projectedasof"] = typeof(DateTime), ["dft_latestsequence"] = typeof(int),
                ["dft_projectionsequence"] = typeof(int)
            });

        /// <summary>Execute only this direct, synchronous bound API, under the actual caller's access.</summary>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            var context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            if (context == null) throw Unavailable();
            context.OutputParameters.Clear();
            context.OutputParameters["ReadSucceeded"] = false;
            try
            {
                if (context.MessageName != ApiName || context.Stage != 30 || context.Mode != 0 ||
                    context.Depth != 1 || context.ParentContext != null || context.IsExecutingOffline || context.IsOfflinePlayback)
                    throw Unavailable();
                if (context.InputParameters.Count != 1 || !context.InputParameters.TryGetValue("Target", out object supplied) ||
                    !(supplied is EntityReference target) || target.LogicalName != Table || target.Id == Guid.Empty || target.KeyAttributes.Count != 0)
                    throw Unavailable();
                var factory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
                if (factory == null) throw Unavailable();
                CallerPerson caller = CallerResolver.Resolve(context, factory);
                IOrganizationService service = factory.CreateOrganizationService(caller.PlatformUser);
                if (service == null) throw Unavailable();
                Entity source = service.Retrieve(Table, target.Id, new ColumnSet(Fields.Keys.ToArray()));
                if (source == null || source.LogicalName != Table || source.Id != target.Id ||
                    !ulong.TryParse(source.RowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out ulong version) || version == 0)
                    throw Unavailable();
                var result = new Entity(Table, source.Id) { RowVersion = source.RowVersion };
                var unavailable = new List<string>();
                foreach (KeyValuePair<string, Type> field in Fields)
                {
                    if (!source.Attributes.TryGetValue(field.Key, out object value) || value == null)
                    {
                        unavailable.Add(field.Key);
                        continue;
                    }
                    if (value.GetType() != field.Value || (value is string text && text.Length > 32768))
                        throw Unavailable();
                    result[field.Key] = value is OptionSetValue state ? new OptionSetValue(state.Value) : value;
                }
                // Never pass through SDK FormattedValues, RelatedEntities, extra attributes or keys.
                context.OutputParameters["Record"] = result;
                context.OutputParameters["UnavailableFields"] = unavailable.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                context.OutputParameters["ReadDisposition"] = Disposition;
                context.OutputParameters["RetrievedAtUtc"] = DateTime.UtcNow;
                context.OutputParameters["ReadSucceeded"] = true;
            }
            catch (InvalidPluginExecutionException) { throw Unavailable(); }
            catch (FaultException<OrganizationServiceFault>) { throw Unavailable(); }
            catch (TimeoutException) { throw Unavailable(); }
        }

        private static InvalidPluginExecutionException Unavailable() =>
            new InvalidPluginExecutionException("Credentialing record read unavailable. No decision or scope has been changed.");
    }
}
