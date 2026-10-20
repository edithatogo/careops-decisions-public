using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class PublisherTests
    {
        [Fact]
        public void RequestsMatchTheActualTrackedSolutionPublisher()
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            var document = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(Path.Combine(AppContext.BaseDirectory, "source-solution.xml"), settings))
                document.Load(reader);
            const string manifest = "/ImportExportXml/SolutionManifest/";
            Assert.Equal(RegisterSchemaPlan.Solution, document.SelectSingleNode(manifest + "UniqueName")!.InnerText);
            string prefix = document.SelectSingleNode(manifest + "Publisher/CustomizationPrefix")!.InnerText + "_";
            int optionPrefix = int.Parse(document.SelectSingleNode(manifest + "Publisher/CustomizationOptionValuePrefix")!.InnerText, CultureInfo.InvariantCulture);
            Assert.Equal(RegisterSchemaPlan.Unverified, optionPrefix * 10000);
            Assert.All(RegisterSchemaPlan.Build().OfType<CreateEntityRequest>(), r => Assert.StartsWith(prefix, r.Entity.SchemaName));
        }
    }
}
