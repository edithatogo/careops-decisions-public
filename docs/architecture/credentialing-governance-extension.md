# Credentialing governance extension

Status: blueprint; disabled by default

## Purpose

This capability pack extends the generic Decisions Workspace to support the
committee, recommendation, delegated-decision, review, and appeal parts of a
credentialing and scope-of-clinical-practice process.

It is designed to receive an immutable, versioned case packet from
`careops-approve`. It does not store the authoritative source documents unless a
deployment profile explicitly approves that records architecture. It does not
replace the current policy, committee, delegate, or authoritative register.

## Separation of responsibilities

| Responsibility | Owning component or role |
| --- | --- |
| Intake, completeness, evidence requests and routing | `careops-approve` |
| Committee mandate, quorum, conflicts and meeting record | `careops-decisions` |
| Relevant-peer and service-capability judgement | Accountable human reviewers |
| Committee recommendation and reasons | Credentialing committee or approved equivalent |
| Final decision | Current delegated human decision maker |
| Operational register and release control | Deployment-defined authoritative system and owner |
| Process conformance and delay analysis | `careops-process` privacy-minimised event projection |

A recommendation and a delegated decision are different records. The system
must not convert a committee recommendation into an approval without the
required delegate, authority reference, time, and reasons.

## Domain entities

The pack introduces or references:

- Credentialing Case
- Practitioner Reference
- Profession and Registration Type
- Application Pathway
- Requested Scope
- Facility or Service
- Evidence Manifest
- Verification Summary
- Relevant-Peer Assessment
- Service-Capability Attestation
- Procedural-Fairness Notice and Response
- Committee Consideration
- Recommendation
- Delegated Decision
- Condition
- Supervision Plan
- Temporary Approval
- Register Activation Reference
- Renewal
- Unscheduled Review
- Appeal
- Cross-Organisation Notification

The extension stores stable references and structured decision evidence. It
must not expose raw sensitive documents in Teams posts, broad dashboards, AI
prompts, or process telemetry.

## Governance packet contract

An inbound packet must include:

- request and practitioner references;
- pathway and requested-scope identifiers;
- facility or service references;
- active template and authority-mapping versions;
- evidence-manifest and verification-summary references;
- professional and service-review references where required;
- procedural-fairness status;
- known exceptions;
- source packet hash; and
- originating system and timestamp.

A packet missing mandatory authority, pathway, evidence, or fairness state is
not ready for committee consideration. It remains visible as an exception and
is returned through the controlled correction path.

## Committee workflow

1. Register the inbound packet and verify its hash and version.
2. Resolve the current committee, terms of reference, membership, quorum rules,
   and decision pathway from the deployment profile.
3. Check conflicts, recusals, proxies, relevant-peer requirements, and meeting
   readiness.
4. Present structured evidence references and exceptions.
5. Record deliberation and a human recommendation with reasons.
6. Where the proposed outcome may be adverse, confirm the required procedural-
   fairness step before final recommendation or decision.
7. Send the recommendation to the current delegated decision maker.
8. Record the separate human decision, authority reference, reasons,
   conditions, supervision, commencement, expiry, and communication status.
9. Return immutable recommendation and decision references to the intake case.
10. Emit privacy-minimised events and create any approved actions or review
    dates.

## Temporary and high-risk pathways

Interim, emergency, disaster, urgent, review, suspension, reduction, revocation,
and appeal pathways remain disabled until individually designed and approved.
Each requires a distinct subtype, current authority, minimum evidence, maximum
duration, hard expiry, retrospective review, fairness controls, and escalation.
They must not be represented by a generic urgent checkbox.

## AI assistance

Permitted assistance includes:

- agenda and evidence-pack summaries;
- draft minutes and action extraction;
- draft recommendation or decision wording;
- consistency checks against structured fields;
- identification of missing reasons, dates, owners, or conditions; and
- policy-change triage for a human maintainer.

Mandatory controls:

- approved model, prompt or custom agent, and deployment environment;
- source references and output provenance;
- draft status only;
- mandatory human acceptance, editing, or rejection;
- no AI vote, recommendation, decision, condition, or appeal outcome;
- no publication without reviewer and approval evidence; and
- normal human operation when AI is unavailable or uncertain.

## MVP

The first MVP should support routine renewal cases in synthetic and silent mode:

- packet receipt and readiness check;
- one configured committee and delegate;
- quorum and conflict evidence;
- recommendation and separate decision;
- decision correspondence draft;
- return of immutable references; and
- operational metrics.

It should not initially write to the authoritative credentialing register or
affect a practitioner’s right to practise.

## Acceptance evidence

- schema and contract validation;
- synthetic positive and negative cases;
- duplicate and replay protection;
- missing authority, quorum, peer, fairness, and delegate tests;
- AI-disabled and AI-failure tests;
- role and access tests;
- decision/recommendation separation;
- feature-flag and rollback test;
- reconciliation with `careops-approve` and `careops-process`; and
- explicit distinction between repository readiness, tenant deployment, silent
  evaluation, and operating effectiveness.
