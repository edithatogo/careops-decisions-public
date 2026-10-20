using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CareOps.Decisions.RegisterSchema;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace CareOps.Decisions.RegisterSchemaTests
{
    public sealed class RegisterIdentityGuardTests
    {
        private static readonly Guid Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid Other = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private const string Failure = "Credentialing record identity could not be preserved. The update was stopped; use the controlled correction process.";
        private static Entity Row()
        {
            var row = new Entity(RegisterSchemaPlan.RecordTable, Id);
            row["dft_recordkey"] = "synthetic-key"; row["dft_practitionerref"] = "synthetic-person";
            row["dft_professionref"] = "synthetic-profession"; row["dft_facilityref"] = "synthetic-facility";
            return row;
        }
        private sealed class Context : RealProxy
        {
            internal readonly Dictionary<string, object?> Values;
            internal Context() : base(typeof(IPluginExecutionContext))
            {
                Values = new Dictionary<string, object?>
                {
                    ["MessageName"] = "Update", ["Stage"] = 40, ["Mode"] = 0, ["IsInTransaction"] = true,
                    ["PrimaryEntityName"] = RegisterSchemaPlan.RecordTable, ["PrimaryEntityId"] = Id,
                    ["OrganizationId"] = Other, ["Depth"] = 1, ["IsExecutingOffline"] = false, ["IsOfflinePlayback"] = false,
                    ["InputParameters"] = new ParameterCollection { ["Target"] = new Entity(RegisterSchemaPlan.RecordTable, Id) },
                    ["PreEntityImages"] = new EntityImageCollection { [ProtectRegisterIdentity.BeforeAlias] = Row() },
                    ["PostEntityImages"] = new EntityImageCollection { [ProtectRegisterIdentity.AfterAlias] = Row() }
                };
            }
            internal IPluginExecutionContext Value => (IPluginExecutionContext)GetTransparentProxy();
            internal Entity Before => ((EntityImageCollection)Values["PreEntityImages"]!)[ProtectRegisterIdentity.BeforeAlias];
            internal Entity After => ((EntityImageCollection)Values["PostEntityImages"]!)[ProtectRegisterIdentity.AfterAlias];
            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                return new ReturnMessage(Values[call.MethodName.Substring(4)], null, 0, call.LogicalCallContext, call);
            }
        }
        private sealed class Services : IServiceProvider
        {
            internal object? Value;
            internal readonly List<Type> Requested = new List<Type>();
            internal bool Throw;
            public object GetService(Type type)
            {
                Requested.Add(type);
                if (Throw) throw new InvalidOperationException("synthetic confidential native fault");
                return Value!;
            }
        }
        private static void Run(Context context)
        {
            var services = new Services { Value = context.Value };
            new ProtectRegisterIdentity().Execute(services);
            Assert.Equal(new[] { typeof(IPluginExecutionContext) }, services.Requested);
        }
        private static void Denied(Context context)
        {
            var services = new Services { Value = context.Value };
            var error = Assert.Throws<InvalidPluginExecutionException>(() => new ProtectRegisterIdentity().Execute(services));
            Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
            Assert.Equal(new[] { typeof(IPluginExecutionContext) }, services.Requested);
        }

        [Fact]
        public void UnchangedIdentityAllowsTheExistingOperationWithoutAnotherReadOrUserStep()
        {
            var c = new Context();
            c.Before["dft_latestsequence"] = 1; c.After["dft_latestsequence"] = 2;
            c.After["dft_recordedstate"] = new OptionSetValue(RegisterSchemaPlan.Unverified + 3);
            Run(c);
            Assert.Equal(2, c.After["dft_latestsequence"]);
            Assert.Equal(4, ProtectRegisterIdentity.ImageColumns.Length);
            Assert.Empty(((Entity)((ParameterCollection)c.Values["InputParameters"]!)["Target"]).Attributes);
        }
        [Theory]
        [InlineData("dft_recordkey")] [InlineData("dft_practitionerref")]
        [InlineData("dft_professionref")] [InlineData("dft_facilityref")]
        public void RejectsRelabellingEvenWhenTheOriginalTargetDidNotContainTheColumn(string field)
        {
            var c = new Context(); c.After[field] = "different-synthetic-reference";
            Denied(c); Assert.Equal("different-synthetic-reference", c.After[field]);
        }
        [Fact]
        public void ACallerSuppliedPreviousValueOrAdministrativeLabelCannotOverrideImages()
        {
            var c = new Context();
            var parameters = (ParameterCollection)c.Values["InputParameters"]!;
            parameters["Before"] = c.After; parameters["administrator"] = true;
            c.After["dft_practitionerref"] = "different"; c.Values["Depth"] = 7;
            Denied(c);
        }
        [Fact]
        public void NestedLegitimateUpdatesAreNotSkippedOrRejectedBasedOnDepthAlone()
        {
            var c = new Context(); c.Values["Depth"] = 7; Run(c);
            c.After["dft_facilityref"] = "different"; Denied(c);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
        public void OnlyTheExpectedNativeTransactionalPipelineCanRunThisGuard(int change)
        {
            var c = new Context();
            switch (change)
            {
                case 0: c.Values["MessageName"] = "Create"; break;
                case 1: c.Values["Stage"] = 20; break;
                case 2: c.Values["Mode"] = 1; break;
                case 3: c.Values["IsInTransaction"] = false; break;
                case 4: c.Values["PrimaryEntityName"] = "other"; break;
                case 5: c.Values["PrimaryEntityId"] = Guid.Empty; break;
                case 6: c.Values["OrganizationId"] = Guid.Empty; break;
                case 7: c.Values["Depth"] = 0; break;
                case 8: c.Values["IsExecutingOffline"] = true; break;
                case 9: c.Values["IsOfflinePlayback"] = true; break;
                case 10: c.Values["MessageName"] = "UpdateMultiple"; break;
            }
            Denied(c);
        }
        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
        public void MalformedOrUnboundTargetsCannotPass(int change)
        {
            var c = new Context(); var p = (ParameterCollection)c.Values["InputParameters"]!;
            switch (change)
            {
                case 0: c.Values["InputParameters"] = null; break;
                case 1: p.Clear(); break;
                case 2: p["Target"] = null!; break;
                case 3: p["Target"] = new EntityReference(RegisterSchemaPlan.RecordTable, Id); break;
                case 4: p["Target"] = new Entity("other", Id); break;
                case 5: p["Target"] = new Entity(RegisterSchemaPlan.RecordTable, Other); break;
            }
            Denied(c);
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void EveryMissingMalformedOrWrongRecordImageIsRejected(bool after)
        {
            string collection = after ? "PostEntityImages" : "PreEntityImages";
            string alias = after ? ProtectRegisterIdentity.AfterAlias : ProtectRegisterIdentity.BeforeAlias;
            for (int change = 0; change < 5; change++)
            {
                var c = new Context(); var images = (EntityImageCollection)c.Values[collection]!;
                if (change == 0) c.Values[collection] = null;
                if (change == 1) images.Clear();
                if (change == 2) images[alias] = null!;
                if (change == 3) images[alias].LogicalName = "other";
                if (change == 4) images[alias].Id = Other;
                Denied(c);
            }
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void UnknownInvalidOrTruncatedIdentifiersAreNeverTreatedAsMatching(bool after)
        {
            object?[] invalid = { null, 1, "", " ", " padded", "padded ", "a\nb", new string('x', 257), "\uD800" };
            foreach (string field in ProtectRegisterIdentity.ImageColumns)
            {
                foreach (object? value in invalid)
                {
                    var c = new Context(); (after ? c.After : c.Before)[field] = value!; Denied(c);
                }
                var missing = new Context(); (after ? missing.After : missing.Before).Attributes.Remove(field); Denied(missing);
            }
        }
        [Fact]
        public void ExactCaseAndUnicodeAreNotSilentlyNormalized()
        {
            var c = new Context(); c.Before["dft_practitionerref"] = "Ref-A"; c.After["dft_practitionerref"] = "ref-a"; Denied(c);
            c.Before["dft_practitionerref"] = "\u00e9"; c.After["dft_practitionerref"] = "e\u0301"; Denied(c);
            c.After["dft_practitionerref"] = "\u00e9"; Run(c);
        }
        [Fact]
        public void BoundsAreExactAndNoSdkImageIsMutated()
        {
            var c = new Context();
            foreach (string field in ProtectRegisterIdentity.ImageColumns)
            {
                string value = new string('x', field == "dft_recordkey" ? 64 : 256);
                c.Before[field] = value; c.After[field] = value;
            }
            Run(c); Assert.Equal(4, c.Before.Attributes.Count); Assert.Equal(4, c.After.Attributes.Count);
            c.Before["dft_recordkey"] = new string('x', 65); c.After["dft_recordkey"] = new string('x', 65); Denied(c);
        }
        [Fact]
        public void RequiredImageColumnsCannotBeModifiedByTheInstaller()
        {
            string[] fields = ProtectRegisterIdentity.ImageColumns; fields[0] = "changed";
            Assert.Equal(new[] { "dft_recordkey", "dft_practitionerref", "dft_professionref", "dft_facilityref" }, ProtectRegisterIdentity.ImageColumns);
        }
        [Fact]
        public void MissingServicesAndNativeFaultsNeverLeakPrivateDetails()
        {
            Assert.Throws<ArgumentNullException>(() => new ProtectRegisterIdentity().Execute(null!));
            foreach (object? context in new object?[] { null, new object() })
            {
                var s = new Services { Value = context };
                var error = Assert.Throws<InvalidPluginExecutionException>(() => new ProtectRegisterIdentity().Execute(s));
                Assert.Equal(Failure, error.Message); Assert.Null(error.InnerException);
            }
            var failing = new Services { Throw = true };
            Assert.Equal(Failure, Assert.Throws<InvalidPluginExecutionException>(() => new ProtectRegisterIdentity().Execute(failing)).Message);
        }
    }
}
