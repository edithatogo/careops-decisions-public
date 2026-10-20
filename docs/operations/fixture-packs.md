# Fixture Packs

## Purpose

Fixture packs provide repeatable seed datasets for local development, smoke tests, and shared test environments. Each pack targets a specific scenario and record scale.

## Pack Types

### Small (single committee, one meeting)

**Intent**: Minimal viable governance structure for smoke testing the app — one body, one committee, one meeting, basic members.

**Record counts**:
| Entity | Count |
| --- | --- |
| Governance Body | 1 |
| Committee | 1 |
| Terms of Reference | 1 (current) |
| Org Person | 3 |
| Committee Membership | 3 |
| Meeting Series | 1 |
| Meeting | 1 |
| Agenda Item | 2 |
| Paper | 1 |
| Minutes | 1 (draft) |
| Decision | 1 |
| Action | 1 |
| Attendee | 3 |

**File location**: `powerplatform/src/seed/fixture-small.json`

### Medium (multi-level hierarchy, multiple meetings)

**Intent**: Realistic committee structure with parent body, child committees, a subcommittee, multiple meetings with decisions and actions, alternate and observer members, superseded ToR.

**Record counts**:
| Entity | Count |
| --- | --- |
| Governance Body | 1 (parent) |
| Committee | 3 |
| Subcommittee | 1 |
| Working Group | 1 |
| Terms of Reference | 8 (current + superseded per body) |
| Org Person | 12 |
| Committee Membership | 20 (mix of roles including alternates and observers) |
| Meeting Series | 4 |
| Meeting | 6 (mix of states) |
| Agenda Item | 18 |
| Paper | 12 |
| Minutes | 10 (draft, approved, published) |
| Decision | 8 |
| Action | 14 |
| Attendee | 48 |
| Transcript | 2 |
| Conflict of Interest | 2 |
| Access Rule | 4 |
| Integration Log | 2 |

**File location**: `powerplatform/src/seed/fixture-medium.json`

### Edge-Case

**Intent**: Boundary conditions — retired bodies, cancelled meetings, superseded decisions, overdue actions, members with expired terms, proxy attendees, meetings with no quorum, empty agenda items, withdrawn papers.

**Record counts**:
| Entity | Count |
| --- | --- |
| Governance Body (retired) | 1 |
| Committee (suspended) | 1 |
| Working Group (draft) | 1 |
| Terms of Reference | 5 (mix of draft, superseded, archived) |
| Org Person | 8 |
| Committee Membership | 8 (including expired and suspended) |
| Meeting Series | 2 (one retired, one active) |
| Meeting | 4 (cancelled, deferred, no-quorum, empty-agenda) |
| Agenda Item | 6 (including withdrawn and deferred) |
| Paper | 4 (withdrawn, superseded) |
| Minutes | 4 (archived, rejected) |
| Decision | 4 (rescinded, superseded) |
| Action | 6 (overdue, blocked, cancelled) |
| Attendee | 16 (including proxy, apology, absent) |
| Access Rule | 2 |

**File location**: `powerplatform/src/seed/fixture-edge.json`

## Usage

Fixture packs are loaded into Dataverse using `Import-SeedData.ps1`:

```powershell
# Small fixture pack
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-small.json

# Medium fixture pack
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-medium.json

# Edge-case fixture pack
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-edge.json
```

## Generation

Fixture packs are **documented** as YAML/JSON placeholders. The actual JSON files should be generated from the record count tables above, following the same shape as `sample-governance-data.json`. Each pack must pass `Test-SeedImportPlan.ps1` validation.

## Synthetic Data Notice

All fixture pack data is explicitly synthetic. Do not include real personal, patient, confidential, or production meeting data in any fixture pack.
