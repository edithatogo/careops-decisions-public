# Specification

## Goal

Extend the generic governance workspace to support a credentialing committee and
delegated-decision pathway without embedding organisation-specific authority or
allowing workflow or AI state to become the decision.

## Requirements

1. Receive a versioned, hashed governance packet from `careops-approve`.
2. Resolve current committee, terms of reference, membership, quorum,
   relevant-peer, conflict, fairness, and delegated-authority rules from the
   deployment profile.
3. Block consideration or decision when mandatory controls are missing.
4. Record committee consideration, recommendation, reasons, and minutes.
5. Record the delegated decision separately with human confirmation and current
   authority reference.
6. Represent conditions, supervision, commencement, expiry, temporary approval,
   register activation, review, appeal, and notifications explicitly.
7. Return immutable recommendation and decision references to the source case.
8. Emit privacy-minimised process events through a versioned contract.
9. Keep raw sensitive source documents outside the capability pack by default.
10. Keep AI assistance optional, provenance-rich, draft-only, and subject to
    mandatory human review.

## Acceptance criteria

- feature flag disabled by default;
- packet schema and hash validation;
- recommendation and decision separation enforced;
- human committee and delegate evidence required;
- missing authority, quorum, peer input, conflict management, fairness, or
  temporary-pathway expiry fails closed;
- AI cannot vote, recommend, decide, publish, communicate, or activate a record;
- sensitive content is excluded from broad surfaces and telemetry;
- positive, negative, duplicate, replay, failure, and reconciliation scenarios
  are covered by synthetic tests; and
- repository, tenant deployment, silent evaluation, and operating effectiveness
  remain distinct status claims.
