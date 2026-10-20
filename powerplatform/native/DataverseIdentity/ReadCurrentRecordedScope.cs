using System;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.DataverseIdentity
{
    /// <summary>Direct caller-bound scope read, composed from identity and complete-history controls. Not practice clearance.</summary>
    public sealed class ReadCurrentRecordedScope : IPlugin
    {
        /// <summary>Single reserved bound API name used by the existing reader. Registration is not performed here.</summary>
        public const string ApiName = VersionedScopeReader.ApiName;

        /// <summary>Accept only an exact record Target; caller identity and evaluation time come from the host.</summary>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            var context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            if (context == null) throw Unavailable();
            Reset(context);
            try
            {
                if (context.MessageName != ApiName || context.Stage != 30 || context.Mode != 0 || context.Depth != 1 ||
                    context.ParentContext != null || context.IsExecutingOffline || context.IsOfflinePlayback)
                    throw Unavailable();
                if (context.InputParameters.Count != 1 || !context.InputParameters.TryGetValue("Target", out object supplied) ||
                    !(supplied is EntityReference target) || target.LogicalName != RegisterSchemaPlan.RecordTable ||
                    target.Id == Guid.Empty || target.KeyAttributes.Count != 0)
                    throw Unavailable();
                Guid record = target.Id; // Freeze the validated binding before any SDK calls.
                var factory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
                if (factory == null) throw Unavailable();
                CallerPerson caller = CallerResolver.Resolve(context, factory);
                IOrganizationService service = factory.CreateOrganizationService(caller.PlatformUser);
                if (service == null) throw Unavailable();
                VersionedScopeRead read = VersionedScopeReader.Read(context, service, record, DateTime.UtcNow);
                RecordedScopeView view = read.View;
                context.OutputParameters["RootVersion"] = read.RootVersion;
                context.OutputParameters["AsOfUtc"] = view.AsOfUtc;
                context.OutputParameters["CurrentnessVerified"] = view.CurrentnessVerified;
                context.OutputParameters["RecordedStatus"] = view.Status.ToString();
                context.OutputParameters["LatestSequence"] = view.LatestSequence;
                context.OutputParameters["SelectedSequence"] = view.SelectedSequence;
                context.OutputParameters["HasEffectiveDecision"] = view.SelectedSequence != 0;
                context.OutputParameters["SelectedDecision"] = view.ToEntity();
                context.OutputParameters["ReadDisposition"] = view.Disposition;
                context.OutputParameters["ReadSucceeded"] = true;
            }
            catch (Exception)
            {
                // Always abort: never swallow, retry, elevate, or expose record-existence/fault details.
                Reset(context);
                throw Unavailable();
            }
        }

        private static void Reset(IPluginExecutionContext context)
        {
            context.OutputParameters.Clear();
            context.OutputParameters["ReadSucceeded"] = false;
        }
        private static InvalidPluginExecutionException Unavailable() => new InvalidPluginExecutionException(
            "Recorded scope read unavailable. No decision or scope has been changed.");
    }
}
