# Native deadline arithmetic: N02 source slice

23 September 2026. Continues careops#160 / N02.08.
Predecessor: careops-approve#57, merged at 326cd7c93dc5f2728135630de8f63b89fe66843b,
which follows careops#164. The cross-repository predecessor is an execution record,
not a mandatory runtime dependency.

## Implemented boundary

`Deadlines.cs` is a .NET Framework 4.6.2 library for the native Dataverse delivery
path. It contains no SQL/SQLite, Python runtime, cloud SDK, network call, identity
provider, application registration, user interface or authoritative write.
It is not yet a registered plug-in or Custom API. The separate no-admin M365
pilot does not require or install this assembly.

BusinessDaysAfter counts days strictly AFTER the supplied civil start date.
Zero returns that date even if nonworking. Nonworking weekdays, closure dates,
calendar coverage and duration are explicit caller inputs; no Queensland/local
holiday or legal counting convention is guessed. Coverage is half-open and bounded
to 3,660 days. Unknown future calendar coverage fails visibly instead of extending
an approval or making up a deadline. Duplicates, bad weekdays, times, implicit
UTC/local conversion and oversized configurations are rejected.

CalendarMonthsAfter uses calendar months with explicit end-of-month clamping,
not 30-day multiplication. It does not decide whether a particular rule includes
the start/end date, or whether the approved duration is three months, three years
or another interval. These remain versioned, editable, human-approved rule data.
The engineering bounds are not clinical policy limits.

## Native hosting requirements still open

An approved server-side adapter must obtain the applicable rule/calendar release,
resolve timestamps to the approved facility/business timezone, and record input,
rule version, calendar version, counting convention and result. A civil date is
not an instant; do not pass UTC midnight or silently truncate an event timestamp.
Date validation does not verify a source or establish legal authority.

Link calendars to current local source instruments and independent configuration
approval. Route missing/expired calendars to the configuration owner and retain
the ordinary authorised human path. Do not send a deficiency notice to a
practitioner because the calendar configuration is incomplete.

No changed delegation, permission, scope, expiry, automatic extension, applicant
communication or register activation is made here. Current CHHHS signed authority
and all other N02 gates remain unverified. One arithmetic component is not a
complete N02 track, a native tenant test or a live credentialing system.

## Qualification and package boundary

CI compiles the net462 library, runs the synthetic tests under .NET Framework
4.8, treats compiler/analyser warnings as errors and requires nonempty 100% line
and branch coverage of the complete library. Tests include weekends/closures,
leap days, end-of-month, zero counts, malformed data, missing configuration,
exhausted calendar coverage and representational overflow. No native runtime was
available locally; inspect actual final-head CI, not a local test claim.

Direct build/test packages are pinned; generated transitive lock files are
recorded for review. Final qualification must use reviewed lock files. Native
Dataverse packaging/registration, authentication, rule retrieval, sandbox parity,
security and operational acceptance remain separate tasks. No extra recurring
review or data-entry step is imposed on credentialing staff by this component.

## Primary sources checked 23 September 2026

- Microsoft Dataverse assembly constraints: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/build-and-package
- Microsoft DateTime.AddMonths: https://learn.microsoft.com/en-us/dotnet/api/system.datetime.addmonths
- Microsoft reference assemblies: https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net462/1.0.3
- Test tools: https://www.nuget.org/packages/xunit.v3/3.2.2 and https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1
