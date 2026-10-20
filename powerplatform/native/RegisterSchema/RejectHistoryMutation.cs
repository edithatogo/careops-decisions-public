using System;
using Microsoft.Xrm.Sdk;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Terminal guard for Update/Delete of final decision and immutable event rows.</summary>
    public sealed class RejectHistoryMutation : IPlugin
    {
        /// <summary>
        /// Reject every invocation without reading caller parameters, private history or another service.
        /// Register ONLY for the four exact synchronous bindings in HistoryMutationStepDefinition.
        /// New decisions are appended; consumer acknowledgements belong in separate mutable records.
        /// </summary>
        public void Execute(IServiceProvider serviceProvider) => throw new InvalidPluginExecutionException(
            "Recorded credentialing history cannot be edited or deleted through this operation. Use the controlled correction or records-management process.");
    }
}
