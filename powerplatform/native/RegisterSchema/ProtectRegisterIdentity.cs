using System;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Post-operation identity invariant for the Decisions register, not a permission to update it.</summary>
    public sealed class ProtectRegisterIdentity : IPlugin
    {
        /// <summary>Required pre-operation image alias, supplied by the native pipeline.</summary>
        public const string BeforeAlias = "CareOpsIdentityBefore";
        /// <summary>Required post-operation image alias, supplied by the native pipeline.</summary>
        public const string AfterAlias = "CareOpsIdentityAfter";
        private static readonly string[] Fields = { "dft_recordkey", "dft_practitionerref", "dft_professionref", "dft_facilityref" };
        private const string Failure = "Credentialing record identity could not be preserved. The update was stopped; use the controlled correction process.";

        /// <summary>Fresh minimal image column list; changing it cannot change the invariant.</summary>
        public static string[] ImageColumns => (string[])Fields.Clone();

        /// <summary>
        /// Runs on every synchronous Update/PostOperation of the standard register table.
        /// Compares native before/after images rather than caller-supplied previous values.
        /// No reads, writes, impersonation, retries, identity repair or external messages.
        /// </summary>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            try
            {
                if (!(serviceProvider.GetService(typeof(IPluginExecutionContext)) is IPluginExecutionContext context) ||
                    context.MessageName != "Update" || context.Stage != 40 || context.Mode != 0 || !context.IsInTransaction ||
                    context.PrimaryEntityName != RegisterSchemaPlan.RecordTable || context.PrimaryEntityId == Guid.Empty ||
                    context.OrganizationId == Guid.Empty || context.Depth < 1 || context.IsExecutingOffline || context.IsOfflinePlayback)
                    throw Rejected();
                if (context.InputParameters == null || !context.InputParameters.TryGetValue("Target", out object value) ||
                    !(value is Entity target) || target.LogicalName != RegisterSchemaPlan.RecordTable || target.Id != context.PrimaryEntityId)
                    throw Rejected();
                Entity before = Image(context.PreEntityImages, BeforeAlias, target.Id);
                Entity after = Image(context.PostEntityImages, AfterAlias, target.Id);
                foreach (string name in Fields)
                {
                    int maximum = name == "dft_recordkey" ? 64 : 256;
                    if (!string.Equals(Identifier(before, name, maximum), Identifier(after, name, maximum), StringComparison.Ordinal))
                        throw Rejected();
                }
            }
            catch (Exception)
            {
                // No source identifiers, SDK faults or inner exception content cross the user boundary.
                // Throwing is essential: do not return success and do not perform compensating writes.
                throw Rejected();
            }
        }

        private static Entity Image(EntityImageCollection images, string alias, Guid id)
        {
            if (images == null || !images.TryGetValue(alias, out Entity image) || image == null ||
                image.LogicalName != RegisterSchemaPlan.RecordTable || image.Id != id)
                throw Rejected();
            return image;
        }

        private static string Identifier(Entity image, string name, int maximum)
        {
            if (!image.Attributes.TryGetValue(name, out object raw) || !(raw is string text) ||
                string.IsNullOrWhiteSpace(text) || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl))
                throw Rejected();
            new UTF8Encoding(false, true).GetByteCount(text);
            return text;
        }

        private static InvalidPluginExecutionException Rejected() => new InvalidPluginExecutionException(Failure);
    }
}
