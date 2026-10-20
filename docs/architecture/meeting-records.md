# Meeting Records

## Minutes State Machine

The `dft_Minutes` table tracks minutes through a defined lifecycle. Each meeting may have multiple minute versions; the most recent approved or published version is the authoritative record.

### States

| State | Description |
| --- | --- |
| draft | Secretariat preparing initial text from notes, transcript, or AI draft |
| chair_review | Submitted to chair for approval; not yet final |
| approved | Chair has confirmed; meeting record is official |
| published | Distributed to members and stored in SharePoint or Dataverse |
| archived | Previous published version retained for audit; no longer current |

### Allowed Transitions

```
draft ──→ chair_review ──→ approved ──→ published
  │                        │              │
  └──→ (revision loop)     │              │
         draft ←───────────┘              │
         (chair rejects)                  │
                    approved ────────────→┘
                    (publish after approval)
```

- **Revision loop**: Chair review can send minutes back to draft with review notes.
- **Re-approval**: After revision, minutes must pass through chair_review again.
- **Archive**: Published minutes can be archived when superseded by a correction or new version.

### Fields for State Transitions

| Field | Set When |
| --- | --- |
| `dft_status` | Updated on each transition |
| `dft_approvedbyid` | Set when status moves to approved |
| `dft_approvedon` | Set when status moves to approved |
| `dft_publishedon` | Set when status moves to published |
| `dft_publishedurl` | Set when status moves to published |

## Paper States

| State | Description |
| --- | --- |
| draft | Being prepared; not yet for circulation |
| submitted | Submitted to secretariat for the meeting pack |
| circulated | Included in the meeting pack and distributed |
| superseded | Replaced by a newer version |
| withdrawn | Removed by author or secretariat before meeting |
| archived | Historical; no longer active |

## Decision Register

Decisions are formal resolutions recorded against a meeting and optional agenda item.

### Register Format

Each decision in the register includes:
- Sequential decision number (e.g. `DEC-001`, `MSC-DEC-2026-05-001`)
- Decision title and full decision text
- Authority reference (e.g. "Medication Safety Committee Terms of Reference")
- Status: proposed, resolved, deferred, rescinded, superseded
- Effective date and review due date
- Owner (responsible person)
- Link to originating meeting and agenda item
- Optional link to parent governance body (for escalation)

### States

```
proposed → resolved → (effective)
             │
             ├──→ deferred
             ├──→ rescinded
             └──→ superseded (by later decision)
```

## Action Register

Actions are follow-up tasks arising from meetings and decisions.

### Register Format

Each action in the register includes:
- Sequential action number (e.g. `ACT-001`, `MSC-ACT-2026-05-001`)
- Summary and completion evidence
- Owner, due date, priority
- Status: open, in progress, blocked, completed, overdue, closed, cancelled
- Link to originating meeting, decision, and agenda item
- Planner sync metadata (task ID, sync status, sync hash)

### States

```
open → in_progress → completed → closed
  │                    │
  ├──→ blocked ─────→ (re-open or close)
  └──→ cancelled
```

### Planner Sync Pattern

When `dft_synctoplanner = true`, a flow should:
1. Create or update a Planner task in the configured plan.
2. Record the Planner task ID on `dft_plannertaskid`.
3. Set `dft_plannersyncstatus` to track the sync state.
4. When the action is completed, update the Planner task.

Dataverse remains the authoritative source; Planner is a sync target.

## Meeting Lifecycle

```
planned → invited → in_progress → minutes_drafting → ready_for_review → approved → published
                                                                          │
                                     deferred ←───────────────────────────┘
                                     cancelled
```

| State | Description |
| --- | --- |
| planned | Meeting created; agenda being prepared |
| invited | Outlook invites sent; attendee responses being tracked |
| in_progress | Meeting is happening or has happened |
| minutes_drafting | Secretariat drafting minutes |
| ready_for_review | Minutes submitted to chair |
| approved | Minutes approved by chair |
| published | Minutes and decisions distributed |
| cancelled | Meeting will not occur |
| deferred | Postponed to a later date |

## Pack Generation Requirements

### Agenda Pack

- Contents: meeting title, date, location, agenda items with papers attached, previous minutes, standing items.
- Format: PDF or SharePoint folder with cover page.
- Distribution: email with pack link (or direct Teams post).
- Timing: circulated N business days before meeting (configurable per series).

### Paper Pack

- Contents: all papers linked to agenda items for a meeting.
- Format: PDF compilation or SharePoint document set.
- Classification: inherited from individual papers; pack label is the highest classification.

### Minutes Pack

- Contents: approved minutes, decisions register, actions register, attendance record.
- Format: PDF with cover page, or SharePoint document set.
- Distribution: published via Teams post or email after chair approval.
- Retention: stored in SharePoint document library with classification label.

### Implementation Notes

All pack generation is deferred to the DMS integration phase. The schema already stores the SharePoint location fields (`dft_sharepointsiteid`, `dft_sharepointdriveid`, `dft_sharepointitemid`) on Paper, Minutes, and Transcript tables to support this.
