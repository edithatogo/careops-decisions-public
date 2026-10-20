using System;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class RecordedScopeSafetyRegressions
    {
        [Fact]
        public void MixedOutcomeAndEffectiveTimeMatrixAlwaysSelectsSequenceBeforeStatus()
        {
            var statuses = new[] { RecordedScopeStatus.NoEffectiveDecision, RecordedScopeStatus.ScopeRecorded,
                RecordedScopeStatus.Suspended, RecordedScopeStatus.Revoked, RecordedScopeStatus.Refused };
            int[] effects = { -1, 0, 5 }; int[] instants = { -3, -2, -1, 0, 5 };
            for (int firstState = 1; firstState <= 4; firstState++)
            for (int secondState = 1; secondState <= 4; secondState++)
            foreach (int firstEffect in effects)
            foreach (int secondEffect in effects)
            foreach (int instant in instants)
            {
                Entity first = RecordedScopeProjectionTests.Row(1, firstState, firstEffect, -2);
                Entity second = RecordedScopeProjectionTests.Row(2, secondState, secondEffect, -1);
                int expectedSequence = instant >= -1 && instant >= secondEffect ? 2 : instant >= -2 && instant >= firstEffect ? 1 : 0;
                int expectedState = expectedSequence == 2 ? secondState : expectedSequence == 1 ? firstState : 0;
                var result = RecordedScopeProjection.Project(RecordedScopeProjectionTests.Record, 2, second.Id,
                    RecordedScopeProjectionTests.History(first, second), RecordedScopeProjectionTests.At.AddDays(instant), null, null);
                Assert.Equal(expectedSequence, result.SelectedSequence);
                Assert.Equal(statuses[expectedState], result.Status);
                Assert.False(result.CurrentnessVerified);
            }
        }

        [Fact]
        public void UnselectedFutureRevisionMustStillHaveCompleteRestrictions()
        {
            Entity first = RecordedScopeProjectionTests.Row(); Entity future = RecordedScopeProjectionTests.Row(2, 1, 5, -1);
            future.Attributes.Remove("dft_conditionstext");
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(
                RecordedScopeProjectionTests.Record, 2, future.Id, RecordedScopeProjectionTests.History(first, future),
                RecordedScopeProjectionTests.At, null, null));
        }

        [Fact]
        public void IncorrectQueryOrderingIsRejectedRatherThanSilentlyRepaired()
        {
            Entity first = RecordedScopeProjectionTests.Row(); Entity second = RecordedScopeProjectionTests.Row(2, 3);
            Assert.Throws<InvalidPluginExecutionException>(() => RecordedScopeProjection.Project(
                RecordedScopeProjectionTests.Record, 2, second.Id, RecordedScopeProjectionTests.History(second, first),
                RecordedScopeProjectionTests.At, null, null));
        }
    }
}
