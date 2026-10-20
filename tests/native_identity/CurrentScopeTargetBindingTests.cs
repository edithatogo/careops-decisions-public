using System;
using CareOps.Decisions.DataverseIdentity;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class CurrentScopeTargetBindingTests
    {
        private sealed class ReusingContextHost : IServiceProvider, IOrganizationServiceFactory
        {
            internal readonly CurrentScopeHost Inner = new CurrentScopeHost();
            public object GetService(Type type) => type == typeof(IOrganizationServiceFactory) ? this : Inner.GetService(type);
            public IOrganizationService CreateOrganizationService(Guid? user)
            {
                IOrganizationService service = Inner.CreateOrganizationService(user);
                // Simulate reuse of a mutable SDK input object after the endpoint validated it.
                ((EntityReference)Inner.Identity.Context.InputParameters["Target"]).Id = Guid.Empty;
                return service;
            }
        }

        [Fact]
        public void ValidatedTargetCannotBeRedirectedByMutableSdkObjectReuse()
        {
            var host = new ReusingContextHost(); new ReadCurrentRecordedScope().Execute(host);
            var output = host.Inner.Identity.Context.OutputParameters;
            Assert.True((bool)output["ReadSucceeded"]);
            var selected = Assert.IsType<Entity>(output["SelectedDecision"]);
            Assert.Equal(CurrentScopeHost.RecordId, ((EntityReference)selected["dft_record"]).Id);
            Assert.Equal(Guid.Empty, ((EntityReference)host.Inner.Identity.Context.InputParameters["Target"]).Id);
        }
    }
}
