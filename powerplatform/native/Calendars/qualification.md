# Native calendar qualification receipt

23 September 2026. Source-only N02.08 slice; full N02 remains open.
Predecessor: careops-approve#57 / 326cd7c93dc5f2728135630de8f63b89fe66843b.

## Executed evidence

GitHub Windows run 35815883417, commit e9773375f2c8cd847fe2f60c5d3dc9604c119664,
compiled the net462 library and executed all 22 net48 tests. The downloaded
qualification artifact 10731801851 contains the actual TRX and Cobertura results:
22 executed/passed, zero failed/skipped; 41/41 lines and 52/52 branches covered.
Artifact SHA-256: 61b23ad17dc06c5f03bec39f0a9711d18d69bbc9f983f1994d023037c575fe0e.
This is actual .NET execution, not a Python reimplementation or Dataverse run.

## Review fixes and locked repeat

The initial collector integration failed to supply coverage even though 22 tests
passed. The nonempty-evidence gate correctly failed the build. Switched to the
framework-compatible coverlet.msbuild 10.0.1 driver; no code, test or coverage
threshold was removed. The upstream issue documents collector/console framework
limitations: https://github.com/coverlet-coverage/coverlet/issues/1984.
Driver documentation: https://www.nuget.org/packages/coverlet.msbuild/10.0.1.
Removed the unused collector settings; retained the whole-library include filter.

The generated core and test dependency lock files were recovered from that actual
artifact and reviewed. They are committed here; final CI uses locked restore and
fails on lock drift, skipped/empty tests or incomplete coverage. SDK 10.0.401 and
workflow actions are pinned. Test-host telemetry is opted out. The final PR must
pass again after these changes before SHA-bound merge.

No calendar source, current delegation, Dataverse hosting, timezone conversion,
application registration, tenant access, operational deadline or scope was
approved or changed. Native sandbox/host binding and policy-owner acceptance
remain gates. This receipt is not independent clinical or security approval.
