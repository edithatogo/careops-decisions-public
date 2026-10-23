# Agent instructions

## Portable suite delivery

Read README.md and .careops/module.json for the existing source/native queue and
shared standards. Retain exact pins, current-head CI gates and publication
boundaries. Use only fictional inputs in portable source; private bindings,
records and native exports remain outside Git. Preserve existing component IDs
and publisher/logical names. Source packaging never establishes native operation.
Evaluate the shared preferred structured-document route before a new generator.

## Required Python execution

Every active Python helper, build, test and CI job uses Python3.15 at the newest
qualified patch, currently3.15.1. Select the interpreter explicitly and align pins,
locks and instructions. Older runtime evidence remains historical, not a default.
Do not replace a running environment or change native bindings for an upgrade.

## Targeted test execution

Follow the [shared targeted-testing policy](https://github.com/edithatogo/careops/blob/main/docs/targeted-testing.md).
During edits, select exact affected files/nodes or native runner checks and reuse
unchanged evidence. Use an explicitly selected repo-local Python3.15.1 via
REPO_PYTHON where applicable. Do not rerun unchanged whole suites during each edit.
Run broader tests for shared fixtures, schemas, dependencies or uncertain impact;
retain full coverage/security/integration and release gates at coherent milestones.

For pytest, preserve cacheprovider per worktree/environment: --lf together with
--last-failed-no-failures=none never silently runs all tests; empty means no tests
verified. --ff still runs all selected tests; --sw is local debugging only. Do not
set selection flags globally or present partial coverage as full coverage.
Default to no automatic retries. skip/skipif requires a real capability condition
and reason; xfail needs a known issue/reason and strict=True. Neither is for speed.
Never retry native mutations, sends or real external effects through test plugins.
Difftest0.3.0 failed actual3.15.1 qualification (PyO3 maximum3.14); do not install
it with a compatibility override or downgrade Python. Rerunfailures16.7 is opt-in
for bounded, diagnosed fictional/read-only transient cases with first failure
visible; it must not turn flaky outcomes into clean acceptance.

Existing runner example (select the actual changed case, not this example blindly):

```sh
"$REPO_PYTHON" -m unittest discover -s tests/reference -p test_register_store.py -v
```
