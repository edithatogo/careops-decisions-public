# Product Guidelines

## Trust and Evidence

- Distinguish repository source, local validation, hosted validation, packaged
  artifacts, and live tenant state.
- Use explicit states such as `verified`, `provisional`, `blocked`, and
  `not assessed`; do not turn missing evidence into success.
- Attach dates, environment identifiers, commands, and durable receipts to
  deployment claims where disclosure is appropriate.

## Safety and Governance

- Keep secrets, tokens, credentials, personal data, and production exports out
  of the repository.
- Preserve human approval for minutes, AI-generated content, access changes,
  releases, and live deployment.
- Treat licensing, DLP, retention, security-role, connector, and tenant access
  requirements as real gates.
- Use synthetic fixtures unless handling of real data has been explicitly
  authorised and governed.

## User Experience

- Design for secretariats, chairs, members, administrators, and support teams
  with clear ownership and actionable status.
- Prefer accessible Microsoft 365 and Power Platform interaction patterns.
- Make errors and blocked states explain what evidence or action is required.
- Avoid implying that an integration is active when only its contract or
  configuration scaffold exists.

## Documentation

- Write concise operational documentation with exact commands and expected
  outcomes.
- Keep architecture decisions, source maps, runbooks, and readiness gates
  synchronized with implementation.
- Record limitations and deferred integrations beside the relevant claim.

## Maintenance

- Optimize repository controls for a solo maintainer using deterministic
  automated checks rather than fictional reviewers or organisational teams.
- Prefer incremental, reversible changes and keep generated artifacts out of
  source control unless they are required evidence.
