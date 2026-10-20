using System;
using System.Globalization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CareOps.Decisions.RegisterSchema
{
    /// <summary>Frozen recorded view and its native root version, not a clearance or reusable authority receipt.</summary>
    public sealed class VersionedScopeRead
    {
        internal VersionedScopeRead(string version, RecordedScopeView view) { RootVersion = version; View = view; }
        /// <summary>Native root row version checked before and after history retrieval.</summary>
        public string RootVersion { get; }
        /// <summary>Recorded history interpreted at the supplied host instant, with currentness separate.</summary>
        public RecordedScopeView View { get; }
    }

    /// <summary>Bounded caller-consistent native read adapter; an authenticated protected host must supply the service.</summary>
    public static class VersionedScopeReader
    {
        /// <summary>Reserved direct read API. This adapter does not register or expose it.</summary>
        public const string ApiName = "dft_ReadCurrentRecordedScopeV1";
        private static readonly string[] RootColumns = { "dft_latestsequence", "dft_latestdecision",
            "dft_sourcecheckedat", "dft_sourcecurrentuntil" };

        /// <summary>
        /// Read root/history/root with no retries or writes. A stable marker is meaningful only with the
        /// protected immutable journal and atomic root updates; it is not independent snapshot isolation.
        /// </summary>
        public static VersionedScopeRead Read(IPluginExecutionContext context, IOrganizationService service, Guid record, DateTime atUtc)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (record == Guid.Empty || atUtc.Kind != DateTimeKind.Utc || context.MessageName != ApiName ||
                context.Stage != 30 || context.Mode != 0 || context.Depth != 1 || context.ParentContext != null ||
                context.IsExecutingOffline || context.IsOfflinePlayback || context.OrganizationId == Guid.Empty ||
                context.InitiatingUserId == Guid.Empty || context.UserId != context.InitiatingUserId) throw Unavailable();
            if (!(service.Execute(new WhoAmIRequest()) is WhoAmIResponse who) ||
                who.OrganizationId != context.OrganizationId || who.UserId != context.InitiatingUserId) throw Unavailable();

            Header first = Capture(service.Retrieve(RegisterSchemaPlan.RecordTable, record, new ColumnSet(RootColumns)), record);
            var query = new QueryExpression(RegisterSchemaPlan.DecisionTable)
            {
                ColumnSet = new ColumnSet(RecordedScopeProjection.Columns), TopCount = RecordedScopeProjection.MaximumHistory + 1
            };
            query.Criteria.AddCondition("dft_record", ConditionOperator.Equal, record);
            query.AddOrder("dft_sequence", OrderType.Ascending);
            query.AddOrder(RegisterSchemaPlan.DecisionTable + "id", OrderType.Ascending);
            // Never filter out future, expired or restricted rows: missing history must not revive an older grant.
            EntityCollection history = service.RetrieveMultiple(query);
            if (history == null) throw Unavailable();
            RecordedScopeView view = RecordedScopeProjection.Project(record, first.Sequence, first.Head,
                history, atUtc, first.CheckedAt, first.CurrentUntil);
            Header last = Capture(service.Retrieve(RegisterSchemaPlan.RecordTable, record, new ColumnSet(RootColumns)), record);
            if (first.Version != last.Version || first.Sequence != last.Sequence || !Nullable.Equals(first.Head, last.Head) ||
                !Nullable.Equals(first.CheckedAt, last.CheckedAt) || !Nullable.Equals(first.CurrentUntil, last.CurrentUntil)) throw Unavailable();
            return new VersionedScopeRead(first.Version, view);
        }

        private sealed class Header
        {
            internal Header(string version, int sequence, Guid? head, DateTime? checkedAt, DateTime? until)
            { Version = version; Sequence = sequence; Head = head; CheckedAt = checkedAt; CurrentUntil = until; }
            internal readonly string Version;
            internal readonly int Sequence;
            internal readonly Guid? Head;
            internal readonly DateTime? CheckedAt;
            internal readonly DateTime? CurrentUntil;
        }

        private static Header Capture(Entity root, Guid record)
        {
            if (root == null || root.LogicalName != RegisterSchemaPlan.RecordTable || root.Id != record ||
                !ulong.TryParse(root.RowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out ulong version) || version == 0 ||
                root.RowVersion != version.ToString(CultureInfo.InvariantCulture)) throw Unavailable();
            if (!root.Attributes.TryGetValue("dft_latestsequence", out object number) || !(number is int sequence) ||
                sequence < 0 || sequence > RecordedScopeProjection.MaximumHistory) throw Unavailable();
            Guid? head = null;
            if (root.Attributes.TryGetValue("dft_latestdecision", out object pointer) && pointer != null)
            {
                if (!(pointer is EntityReference reference) || reference.LogicalName != RegisterSchemaPlan.DecisionTable ||
                    reference.Id == Guid.Empty || reference.KeyAttributes.Count != 0) throw Unavailable();
                head = reference.Id;
            }
            if ((sequence == 0) != !head.HasValue) throw Unavailable();
            return new Header(root.RowVersion, sequence, head, OptionalUtc(root, "dft_sourcecheckedat"), OptionalUtc(root, "dft_sourcecurrentuntil"));
        }

        private static DateTime? OptionalUtc(Entity root, string field)
        {
            if (!root.Attributes.TryGetValue(field, out object value) || value == null) return null;
            if (!(value is DateTime instant) || instant.Kind != DateTimeKind.Utc) throw Unavailable();
            return instant;
        }
        private static InvalidPluginExecutionException Unavailable() => new InvalidPluginExecutionException(
            "Recorded scope read is unavailable or changed during retrieval. No decision or scope has been changed.");
    }
}
