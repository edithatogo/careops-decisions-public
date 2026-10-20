# Human-confirmed journal: source review corrections

25 September 2026. Part of the N03.04-N03.06 composition source PR.

Native graph qualification added only the existing same-module Authority project
edge; no external package versions changed. The actual generated locks came from
artifact10841640113, SHA256
a0612536d01018258a236a0ee1a8eeba3b034d949ff7fefcdca72b1ece05cadc.
They are committed, and the temporary graph-resolving workflow was removed.
The original locked-restore test gate was never disabled; it blocked the initial
unqualified edge. Native compile then identified a possibly-null parameter access.
Replaced it with explicit typed pattern validation, not a suppressed warning.

Review identified a temporal authority gap: being authorised now does not prove
that the same person held that authority when the human confirmation occurred.
The composed core now invokes the actual delegation kernel at BOTH the protected
confirmation instant and the server recording-attempt time. Tests reject later
profile release, later role occupancy, later profile effectiveness and authority
which was valid at confirmation but has since expired. Current authority cannot
retroactively legitimise an earlier unauthorised human action.

This is not historical source reconstruction. The protected native adapter still
must retrieve the correct version and original human-action evidence, preserve
withdrawal history and prevent concurrent source changes. Neither constructors
nor a caller-generated digest establish those facts. The source core is not a
public API or active tenant control. It adds no clinician approval or queue.
Final actual hosted compilation/tests and complete coverage remain prerequisites
for merge; no native tenant or local .NET execution is claimed here.
