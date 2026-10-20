# Implementation plan

## Repository tranche

- [x] Add organisation-neutral credentialing governance architecture.
- [x] Add a capability-pack schema and invariants.
- [x] Add the governance and delegated-decision flow contract.
- [x] Add a committee-governance review agent.
- [x] Add this Conductor track and remove active organisation-specific source labels from the track registry.
- [ ] Extend schema and contract validators for the new files.
- [ ] Add synthetic cases for routine approval, deferral, adverse fairness,
  missing quorum, conflict, missing relevant peer, stale authority, invalid
  delegate, temporary expiry, review, appeal, duplicate, replay, and failed
  outbound reconciliation.
- [ ] Add BPMN and rendered process diagrams.
- [ ] Add role-specific smoke tests for secretariat, chair, member, peer,
  decision maker, auditor, and restricted reader.

## CHHHS silent-mode MVP

- [ ] Map current CHHHS committee terms, quorum, peer, conflict, fairness,
  delegation, reasons, conditions, expiry, review, and appeal requirements.
- [ ] Configure one committee and one routine renewal pathway.
- [ ] Use synthetic cases before any approved de-identified historical cases.
- [ ] Run retrospective and prospective silent comparison without changing the
  current decision or register.
- [ ] Measure readiness failures, deferrals, preparation time, recommendation
  quality, decision latency, correspondence accuracy, and staff effort.
- [ ] Complete privacy, security, records, clinical-governance, legal,
  accessibility, architecture, support, and continuity gates.
- [ ] Seek a separate decision before production integration or register writes.

## Later packs

- [ ] Changed and additional scope.
- [ ] Mutual recognition and multi-organisation notifications.
- [ ] Interim, emergency, disaster, and other temporary pathways.
- [ ] Conditions and supervision monitoring.
- [ ] Unscheduled review, reduction, suspension, revocation, and appeal.
- [ ] Register activation, release-to-practise, expiry, and roster enforcement.
- [ ] Policy-change triage and human rule-maintainer workflow.
