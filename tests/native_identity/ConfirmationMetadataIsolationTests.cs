using System.Linq;
using CareOps.Decisions.DataverseIdentity;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.DataverseIdentity.Tests
{
    public sealed class ConfirmationMetadataIsolationTests
    {
        [Fact]
        public void MutatingASdkKeyCannotChangeAnotherSourceDefinition()
        {
            var first = ConfirmationSourceSchema.Build().OfType<CreateEntityKeyRequest>().Single();
            first.EntityKey.KeyAttributes[0] = "dft_privatecontent";
            var next = ConfirmationSourceSchema.Build();
            var key = Assert.Single(next.OfType<CreateEntityKeyRequest>());
            Assert.Equal(new[] { "dft_confirmationkey" }, key.EntityKey.KeyAttributes);
            var keyColumn = Assert.Single(next.OfType<CreateAttributeRequest>(), row => row.Attribute.SchemaName == "dft_confirmationkey");
            Assert.False(keyColumn.Attribute.IsSecured);
            Assert.False(Assert.Single(next.OfType<CreateEntityRequest>()).PrimaryAttribute.IsSecured);
            Assert.False(Assert.Single(next.OfType<CreateOneToManyRequest>()).Lookup.IsSecured);
        }
    }
}
