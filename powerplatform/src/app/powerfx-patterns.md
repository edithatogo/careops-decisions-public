# Power Fx Patterns

These patterns are the shared formulas for the Teams canvas app scaffold. They assume Dataverse-backed tables and role-aware visibility.

## App initialization

Use one initialization flow to load identity, role, defaults, and the first data window.

```powerfx
Set(varUserEmail, Lower(User().Email));
Set(varUserName, User().FullName);
Set(varToday, Today());
Set(varAppMode, "Home");
Set(varSelectedBody, Blank());
Set(varSelectedCommittee, Blank());
Set(varSelectedMeeting, Blank());
Set(varSelectedAgendaItem, Blank());
Set(varSelectedMinutes, Blank());
Set(varSelectedPaper, Blank());
Set(varSelectedDecision, Blank());
Set(varSelectedAction, Blank());
Set(varCanEdit, false);
Set(varIsAdmin, false);
```

## Role resolution

Resolve the current user's effective role from membership and direct assignments, then store the strongest permission.

```powerfx
Set(
    varCurrentPerson,
    LookUp(dft_OrgPerson, Lower('Primary Email') = varUserEmail)
);

Set(
    varEffectiveRole,
    Coalesce(
        LookUp(dft_CommitteeMembership, Lower(Person.'Primary Email') = varUserEmail && Active = true, Role),
        "member"
    )
);

Set(varIsAdmin, varEffectiveRole = "governance_administrator");
Set(varCanEdit, varIsAdmin || varEffectiveRole = "secretariat" || varEffectiveRole = "chair");
```

## Navigation

Navigate with an explicit screen variable so the shell can stay Teams-friendly and predictable.

```powerfx
Set(varAppMode, "Meeting");
Set(varSelectedMeeting, ThisItem);
Navigate(MeetingWorkspaceScreen, ScreenTransition.Fade);
```

Use role-safe navigation buttons:

```powerfx
DisplayMode = If(varCanEdit, DisplayMode.Edit, DisplayMode.View)
```

## Filtered galleries

Keep galleries scoped to the selected body, meeting, and search term.

```powerfx
Filter(
    dft_Meeting,
    IsBlank(varSelectedBody) || GovernanceBody = varSelectedBody,
    IsBlank(cmbStatus.Selected.Value) || Status = cmbStatus.Selected.Value,
    IsBlank(txtSearch.Text) || StartsWith(Title, txtSearch.Text)
)
```

For agenda items:

```powerfx
SortByColumns(
    Filter(dft_AgendaItem, Meeting = varSelectedMeeting),
    "Sequence",
    Ascending
)
```

## Single-record editing

Use `Defaults(...)` for new records and `Patch(...)` for save actions.

```powerfx
Set(
    varSavedMeeting,
    Patch(
        dft_Meeting,
        Coalesce(varSelectedMeeting, Defaults(dft_Meeting)),
        {
            Title: txtMeetingTitle.Text,
            Status: cmbMeetingStatus.Selected.Value,
            ScheduledDate: dpScheduledDate.SelectedDate,
            Chair: cmbChair.Selected,
            GovernanceBody: varSelectedBody
        }
    )
);
Set(varSelectedMeeting, varSavedMeeting);
```

## Child record creation

Create related rows from the parent record that is already in context.

```powerfx
Patch(
    dft_AgendaItem,
    Defaults(dft_AgendaItem),
    {
        Meeting: varSelectedMeeting,
        Sequence: Coalesce(Max(Filter(dft_AgendaItem, Meeting = varSelectedMeeting), Sequence), 0) + 1,
        Title: txtAgendaTitle.Text,
        Presenter: cmbPresenter.Selected,
        DecisionSought: txtDecisionSought.Text
    }
)
```

## Command enablement

Disable commands when the role or record state does not support the action.

```powerfx
DisplayMode =
If(
    varCanEdit && varSelectedMeeting.Status <> "published",
    DisplayMode.Edit,
    DisplayMode.Disabled
)
```

Use the same pattern for chair approvals:

```powerfx
Visible = varEffectiveRole = "chair" || varIsAdmin
```

## Per-meeting feature switchboard

Resolve the selected meeting's effective feature state once, then use the same helper everywhere the app needs to show or hide a command, button, or panel.

```powerfx
Set(
    varMeetingFeatureSwitchboard,
    /* resolved meeting series defaults with meeting-level overrides and runtime availability */
    Blank()
);
```

Use the helper directly in visibility checks:

```powerfx
Visible = IsFeatureEnabled(varSelectedMeeting, "voting_enabled")
```

```powerfx
DisplayMode =
If(
    IsFeatureEnabled(varSelectedMeeting, "planner_sync_enabled") && varCanEdit,
    DisplayMode.Edit,
    DisplayMode.Disabled
)
```

The feature key stays explicit in the contract. Common keys in this app are `voting_enabled`, `ai_minutes_enabled`, `transcript_import_enabled`, `planner_sync_enabled`, `teams_post_enabled`, `sharepoint_document_store_enabled`, `outlook_invites_enabled`, `org_hierarchy_refresh_enabled`, and `conflict_tracking_enabled`.

When no meeting is selected, treat the capability as disabled until a meeting context is active.

## Quorum calculation

Calculate quorum from active voting members and present attendees.

```powerfx
Set(
    varQuorumRequired,
    RoundUp(
        CountRows(Filter(dft_CommitteeMembership, Committee = varSelectedBody && Active = true && VotingRights = true)) / 2,
        0
    )
);

Set(
    varQuorumPresent,
    CountRows(Filter(dft_Attendee, Meeting = varSelectedMeeting && AttendanceStatus = "present" && VotingEligible = true))
);

Set(varQuorumMet, varQuorumPresent >= varQuorumRequired);
```

## Status transitions

Keep state transitions explicit instead of letting random edits infer final state.
Reference the full state machine in `state-transitions.yaml` for all entities, roles, and preconditions.

### Meeting lifecycle

```powerfx
Patch(
    dft_Meeting,
    varSelectedMeeting,
    { Status: "in progress" }
);
```

```powerfx
Patch(
    dft_Meeting,
    varSelectedMeeting,
    { Status: "completed" }
);
```

### Minutes lifecycle (draft → chair_review → approved → published → archived)

```powerfx
Patch(
    dft_Minutes,
    varSelectedMinutes,
    { Status: "chair review" }
);
```

```powerfx
Patch(
    dft_Minutes,
    varSelectedMinutes,
    { Status: "approved" }
);
```

```powerfx
Patch(
    dft_Minutes,
    varSelectedMinutes,
    { Status: "published" }
);
```

### Decision lifecycle (draft → approved → published → superseded)

```powerfx
Patch(
    dft_Decision,
    varSelectedDecision,
    { Status: "approved", Authority: varUserEmail }
);
```

```powerfx
Patch(
    dft_Decision,
    varSelectedDecision,
    { Status: "published" }
);
```

### Action lifecycle (open → in_progress → completed → closed)

```powerfx
Patch(
    dft_ActionItem,
    ThisItem,
    { Status: "open", DueDate: dpDueDate.SelectedDate }
);
```

```powerfx
Patch(
    dft_ActionItem,
    ThisItem,
    { Status: "in_progress" }
);
```

```powerfx
Patch(
    dft_ActionItem,
    ThisItem,
    { Status: "completed", EvidenceLink: txtEvidenceLink.Text }
);
```

### Paper lifecycle (draft → submitted → superseded)

```powerfx
Patch(
    dft_Paper,
    varSelectedPaper,
    { Status: "submitted", Classification: cmbClassification.Selected.Value }
);
```

### Agenda item lifecycle (draft → confirmed → completed → deferred)

```powerfx
Patch(
    dft_AgendaItem,
    varSelectedAgendaItem,
    { Status: "confirmed" }
);
```

```powerfx
Patch(
    dft_AgendaItem,
    varSelectedAgendaItem,
    { Status: "deferred" }
);
```

### Vote lifecycle (open → closed → recorded)

```powerfx
Patch(
    dft_Vote,
    varSelectedVote,
    { Status: "closed" }
);
```

```powerfx
Patch(
    dft_Vote,
    varSelectedVote,
    { Status: "recorded" }
);
```

### Transcript lifecycle (uploaded → processing → needs_review → reviewed)

```powerfx
Patch(
    dft_Transcript,
    varSelectedTranscript,
    { Status: "uploaded", ProcessingRequested: true }
);
```

```powerfx
Patch(
    dft_Transcript,
    varSelectedTranscript,
    { Status: "reviewed" }
);
```

## Transcript import

Normalize pasted transcript text before it is passed to the draft workflow.

```powerfx
Set(
    varTranscriptText,
    Trim(
        Substitute(
            Substitute(txtTranscript.Text, Char(13), Char(10)),
            Char(9),
            " "
        )
    )
);
```

Use a guard before launching draft generation:

```powerfx
If(
    Len(varTranscriptText) > 0 && !IsBlank(varSelectedMeeting),
    Navigate(MinutesReviewScreen, ScreenTransition.Cover)
)
```

## AI review staging

Treat AI output as draft assistance until a human reviews it.

```powerfx
Set(varDraftStatus, "queued");
Patch(
    dft_AIBuilderRun,
    Defaults(dft_AIBuilderRun),
    {
        Meeting: varSelectedMeeting,
        Transcript: varSelectedTranscript,
        Status: "queued",
        Reviewer: Blank()
    }
);
```

When promoting AI output into minutes, decisions, or actions, always write review state explicitly.

```powerfx
Patch(
    dft_Minutes,
    varSelectedMinutes,
    {
        Status: "draft",
        ReviewStatus: "pending chair review"
    }
);
```

## Conflict of interest declaration

Record and resolve conflicts before voting begins.

```powerfx
Patch(
    dft_ConflictOfInterest,
    Defaults(dft_ConflictOfInterest),
    {
        Meeting: varSelectedMeeting,
        AgendaItem: varSelectedAgendaItem,
        Person: varCurrentPerson,
        ConflictType: cmbConflictType.Selected.Value,
        Resolution: Blank()
    }
);
```

Resolve a declared conflict:

```powerfx
Patch(
    dft_ConflictOfInterest,
    varSelectedConflict,
    {
        Resolution: cmbConflictResolution.Selected.Value,
        ResolvedBy: varUserEmail,
        ResolvedOn: Now()
    }
);
```

Filter active (unresolved) conflicts for a meeting:

```powerfx
Filter(
    dft_ConflictOfInterest,
    Meeting = varSelectedMeeting,
    IsBlank(Resolution)
)
```

## Planner sync flags

Keep local truth in Dataverse and treat Planner as a mirror.

```powerfx
Patch(
    dft_ActionItem,
    ThisItem,
    {
        PlannerSyncState: "queued",
        PlannerTaskId: Blank(),
        LastSyncAttempt: Now()
    }
);
```

## Integration log write

Write a log row whenever a connector action succeeds or fails.

```powerfx
Patch(
    dft_IntegrationLog,
    Defaults(dft_IntegrationLog),
    {
        SourceSystem: "Power Apps",
        TargetSystem: "Planner",
        RecordType: "ActionItem",
        RelatedRecordId: Text(ThisItem.ActionItemId),
        Status: If(varSyncSucceeded, "success", "error"),
        ErrorMessage: If(varSyncSucceeded, Blank(), varSyncError)
    }
);
```

## Gallery selection

Always set the selected parent before navigating to a child workspace.

```powerfx
Set(varSelectedBody, ThisItem);
Navigate(GovernanceEditScreen, ScreenTransition.None);
```

```powerfx
Set(varSelectedAgendaItem, ThisItem);
Navigate(AgendaEditorScreen, ScreenTransition.None);
```

## Recommended record context variables

- `varCurrentPerson`
- `varEffectiveRole`
- `varSelectedBody`
- `varSelectedCommittee`
- `varSelectedMeeting`
- `varSelectedAgendaItem`
- `varSelectedPaper`
- `varSelectedMinutes`
- `varSelectedDecision`
- `varSelectedAction`
- `varQuorumMet`
- `varDraftStatus`
- `varAppMode`
