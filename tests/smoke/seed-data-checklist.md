# Seed Data Checklist

Use this checklist to confirm the sample seed data is ready before a smoke run.

## File Check

- [ ] `powerplatform/src/seed/sample-governance-data.json` exists
- [ ] `tests/smoke/smoke-scenarios.md` exists
- [ ] `tests/smoke/runbook.md` exists
- [ ] `docs/testing/smoke-runbook.md` exists
- [ ] `tests/powerapps/suites/feature-switchboard.yaml` exists

## Seed Record Check

- [ ] At least three governance bodies are seeded
- [ ] At least four committees or working groups are seeded
- [ ] Current, superseded, and archived terms-of-reference records are seeded
- [ ] Members are seeded with chair, deputy chair, secretariat, member, alternate, and observer roles
- [ ] Eight feature definitions are seeded for the meeting switchboard
- [ ] Eight meeting-series feature defaults are seeded
- [ ] Three meeting-level feature overrides are seeded
- [ ] One meeting series is seeded
- [ ] One meeting is seeded for May 2026
- [ ] At least two agenda items are seeded
- [ ] Attendee records include accepted attendees, an apology, proxy attendance, an observer, and a guest
- [ ] At least one manual transcript is seeded
- [ ] At least one decision is seeded
- [ ] At least one action is seeded
- [ ] At least one AI Builder prompt and completed run are seeded with pending human review

## Scenario Coverage Check

- [ ] `SMK-01` Governance body setup
- [ ] `SMK-02` Membership and attendees
- [ ] `SMK-03` Meeting creation
- [ ] `SMK-04` Agenda build
- [ ] `SMK-05` Papers
- [ ] `SMK-06` Minutes capture
- [ ] `SMK-07` Decisions
- [ ] `SMK-08` Actions
- [ ] `SMK-09` Planner sync
- [ ] `SMK-10` Teams posts
- [ ] `SMK-11` Transcript-to-minutes manual input
- [ ] `SMK-12` Transcript-to-minutes AI Builder
- [ ] `SMK-13` Integration logs
- [ ] `SMK-14` AI human review gate
- [ ] `SMK-15` Governance dashboards
- [ ] `SMK-16` Reporting data-quality checks
- [ ] `SMK-17` Per-meeting feature switchboard

## Run Notes

- [ ] Environment name recorded
- [ ] Solution version recorded
- [ ] Tester recorded
- [ ] Date and time recorded
- [ ] Evidence links captured where needed
- [ ] Connector skips or suppression reasons documented where needed
