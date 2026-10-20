using System;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class DecisionReplayTransportTests
    {
        [Fact]
        public void NativePrimaryKeyColumnMayAccompanyTheExactBusinessProjection()
        {
            DecisionAppend intent = DecisionAppendTests.Intent(); Entity row = intent.ToEntity();
            row[RegisterSchemaPlan.DecisionTable + "id"] = intent.DecisionId;
            Assert.True(intent.Matches(row));
            row[RegisterSchemaPlan.DecisionTable + "id"] = Guid.Empty;
            Assert.False(intent.Matches(row));
            row[RegisterSchemaPlan.DecisionTable + "id"] = "not-a-guid";
            Assert.False(intent.Matches(row));
        }

        [Fact]
        public void NullOrOmittedRestrictionsNeverProveNoRestrictionsOnReplay()
        {
            DecisionAppend intent = DecisionAppendTests.Intent(); Entity row = intent.ToEntity();
            row["dft_conditionstext"] = null!; Assert.False(intent.Matches(row));
            row.Attributes.Remove("dft_conditionstext"); Assert.False(intent.Matches(row));
        }

        [Fact]
        public void ChangedTransportPrecisionIsNotSilentlyAnExactReplay()
        {
            DecisionAppend intent = DecisionAppendTests.Intent(); Entity row = intent.ToEntity();
            row["dft_recordedat"] = DecisionAppendTests.Instant.AddTicks(1);
            Assert.False(intent.Matches(row));
        }
    }
}
