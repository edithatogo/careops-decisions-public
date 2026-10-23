# Project Workflow

## Sources of Truth

1. Hosted `main` and its completed checks are the repository baseline.
2. Track specifications and plans define intended work, but completion markers
   require matching source, tests, commits, and receipts.
3. Local uncommitted files are work in progress, not deployed truth.
4. Live Power Platform state requires authenticated, environment-specific
   evidence and any required human approval.

## Task Lifecycle

1. Select a pending task from the relevant `plan.md` and mark it `[~]`.
2. Identify acceptance criteria and the lowest-cost deterministic reproducer.
3. Add or update tests before or alongside implementation.
4. Implement the smallest coherent change.
5. Run targeted checks, followed by repository validation and packaging when
   the solution layout changes.
6. Review the diff for secrets, personal data, generated artifacts, unsupported
   deployment claims, and accidental solution-tree deletions.
7. Commit the implementation using Conventional Commits and record the commit
   in the plan.
8. Push a branch or `main` as appropriate and verify completed hosted checks.
9. Mark a task `[x]` only when its stated acceptance criteria are evidenced;
   otherwise leave it `[~]` and record the blocker.

## Validation Commands

Run from the repository root in PowerShell:

```powershell
./scripts/Test-AlmLayout.ps1
./scripts/Test-SourceArtifacts.ps1
./scripts/Invoke-RepoValidation.ps1 -RequireImplementationScaffolds
./scripts/Get-EnvironmentReadiness.ps1
./scripts/New-ReleaseArtifact.ps1 -Unmanaged
```

Use more focused `scripts/Test-*.ps1` contract checks while developing. Do not
run live import, publish, activation, seed, or mutation commands without the
required environment authority and an explicit in-scope deployment task.

## Evidence Levels

- **Source complete:** required files and contracts exist.
- **Locally validated:** named local commands completed successfully.
- **Hosted validated:** the exact pushed commit has completed required checks.
- **Artifact ready:** the exact commit produced and validated its package.
- **Environment verified:** read-only probes confirm the named tenant state.
- **Deployed:** authorised mutation completed with a durable receipt.

Higher levels must not be inferred from lower ones.

## Review and Release

- Preserve unrelated dirty work and use focused branches or worktrees.
- Review large generated solution changes using rename/layout analysis and
  pack/unpack validation rather than line count alone.
- Never commit credentials, tokens, patient information, or unapproved real
  tenant exports.
- A release requires version consistency, green deterministic checks, a
  reproducible artifact, documented limitations, and explicit publication or
  deployment authority.

## Commit Format

```text
<type>(<scope>): <description>
```

Use `feat`, `fix`, `docs`, `test`, `refactor`, or `chore` as appropriate.

## Targeted test cadence

Follow AGENTS.md targeted-test guidance during edits. Preserve full integration,
coverage and release gates at coherent milestones. Cached or zero-test results
are not complete acceptance evidence.
