[CmdletBinding()] param([switch]$WhatIf)
$env="https://environment.example.invalid/"
$root=Split-Path -Parent $PSScriptRoot
$token=az account get-access-token --resource $env --query accessToken -o tsv
$h=@{Authorization="Bearer $token";"Content-Type"="application/json"}
$b="$env/api/data/v9.2"
$s=Get-Content (Join-Path $root "powerplatform/src/seed/sample-governance-data.json") -Raw|ConvertFrom-Json
$ok=0;$bad=0

$opt=@{board=602170000;council=602170001;committee=602170002;subcommittee=602170003;working_group=602170004;project_board=602170005;executive_forum=602170006;advisory_panel=602170007;task_force=602170008;other=602170009;active=602170001;draft=602170000;suspended=602170002;retired=602170003;
voting=602170000;ai_minutes=602170001;transcript_import=602170002;planner_sync=602170003;teams_posts=602170004;sharepoint_papers_minutes=602170005;outlook_invites=602170006;org_hierarchy=602170007;
enabled=602170000;disabled=602170001;chair=602170000;deputy_chair=602170001;secretary=602170002;member=602170003;alternate=602170004;observer=602170005;guest=602170006;presenter=602170007;
planned=602170001;invited=602170002;in_progress=602170003;minutes_drafting=602170004;ready_for_review=602170005;approved=602170006;published=602170007;cancelled=602170008;deferred=602170009;
proposed=602170000;resolved=602170001;rescinded=602170003;superseded=602170004;open=602170000;blocked=602170002;completed=602170003;overdue=602170004;closed=602170005;
low=602170000;normal=602170001;high=602170002;urgent=602170003;information=602170000;discussion=602170001;decision=602170002;action=602170003;ratification=602170004;noting=602170005;
ready=602170001;held=602170002;withdrawn=602170004;chair_review=602170001;archived=602170004;
submitted=602170001;circulated=602170002;sharepoint=602170000;teams=602170001;email=602170002;uploaded=602170003;dataverse_file=602170004;external_link=602170005;
public=602170000;internal=602170001;confidential=602170002;restricted=602170003;legal=602170004;
paused=602170002;current=602170001;pending=602170000;rejected=602170002;
one_off=602170000;weekly=602170001;fortnightly=602170002;monthly=602170003;quarterly=602170004;custom=602170005;
manager=602170000;direct_report=602170001;delegate=602170002;substitute=602170003;dotted_line=602170004;
office365_users=602170000;graph=602170001;manual=602170002;hr_feed=602170003;
succeeded=602170001;failed=602170002;retried=602170003;skipped=602170004;total_members=602170000;voting_members=602170001;appointed_members=602170002;eligible_attendees=602170003;
body_wide=602170000;meeting_only=602170001;agenda_item_only=602170002;document_only=602170003;role_based=602170004;external_shared=602170005;allow=602170000;deny=602170001;review=602170002;exception=602170003}
function O($v){if($v-and$opt.ContainsKey("$($v)")){return$opt["$($v)"]}else{return$null}}
function X($t){$v=$t.Substring(4);return$v}
function C($t,$f){
    if($WhatIf){Write-Host"  $t ($($f.dft_name))";return}
    try{Invoke-RestMethod -Uri "$b/$t" -Method Post -Headers $h -Body($f|ConvertTo-Json -Depth 3)-ErrorAction Stop|Out-Null;Write-Host"  OK $t ($($f.dft_name))"-ForegroundColor Green;$script:ok++}
    catch{$c=$_.Exception.Response.StatusCode.value__;if($c-eq204){Write-Host"  OK $t ($($f.dft_name))"-ForegroundColor Green;$script:ok++}else{Write-Host"  FAIL $c $t ($($f.dft_name))"-ForegroundColor Yellow;$script:bad++}}
}

Write-Host "Phase 1: People" -ForegroundColor Cyan
foreach($m in $s.members){C "dft_orgpersons" @{dft_name=$m.displayName;dft_workemail=$m.mail}}

Write-Host "Phase 2: Features" -ForegroundColor Cyan
foreach($d in $s.featureDefinitions){C "dft_featuredefinitions" @{dft_name=$d.name;dft_featurekeycode=(O $d.featureCode);dft_defaultstatecode=(O $(if($d.defaultEnabled){'enabled'}else{'disabled'}));dft_description=$d.description;dft_sortorder=$d.sortOrder}}

Write-Host "Phase 3: Bodies" -ForegroundColor Cyan
foreach($g in $s.governanceBodies){$f=@{dft_name=$g.name;dft_bodycode=$g.key;dft_bodytypecode=(O $g.type);dft_statuscode=(O $g.status);dft_defaultclassificationcode=(O $g.defaultClassification)};C "dft_governancebodies" $f}

Write-Host "Phase 4: Committees" -ForegroundColor Cyan
foreach($x in $s.committees){$f=@{dft_name=$x.name;dft_bodycode=$x.key;dft_bodytypecode=(O $x.type);dft_statuscode=(O $x.status);"dft_parentgovernancebodyid@odata.bind"="/dft_governancebodies(dft_bodycode='$($x.governanceBodyKey)')"};C "dft_governancebodies" $f}

Write-Host "Phase 5: Membership" -ForegroundColor Cyan
foreach($m in $s.members){$f=@{dft_name="$($m.displayName) - $($m.role)";dft_membershiprolecode=(O $m.role);dft_membershipstatuscode=(O $m.membershipStatus);dft_startdate=$m.startDate;dft_votingrights=[bool]$m.votingRights;dft_isalternate=[bool]$m.isAlternate;"dft_governancebodyid@odata.bind"="/dft_governancebodies(dft_bodycode='$($m.committeeKey)')";"dft_orgpersonid@odata.bind"="/dft_orgpersons(dft_workemail='$($m.mail)')"};C "dft_committeememberships" $f}

Write-Host "Phase 6: ToR" -ForegroundColor Cyan
foreach($t in $s.termsOfReference){$f=@{dft_name=$t.title;dft_versionnumber=$t.version;dft_effectivefrom=$t.effectiveFrom;dft_reviewdueon=$t.reviewDate;dft_statuscode=(O $t.status);dft_approvalstatuscode=(O $t.approvalStatus);"dft_governancebodyid@odata.bind"="/dft_governancebodies(dft_bodycode='$($t.committeeKey)')"};C "dft_termsofreferences" $f}

Write-Host "Phase 7: Series" -ForegroundColor Cyan
foreach($n in $s.meetingSeries){$f=@{dft_name=$n.name;dft_seriescode=$n.key;dft_meetingcadencecode=(O $n.cadence);dft_seriesstatuscode=(O $n.seriesStatus);"dft_governancebodyid@odata.bind"="/dft_governancebodies(dft_bodycode='$($n.committeeKey)')"};C "dft_meetingseries" $f}

Write-Host "Phase 8: Meetings" -ForegroundColor Cyan
foreach($m in $s.meetings){$f=@{dft_name=$m.title;dft_meetingstatuscode=(O $m.status);dft_scheduledstartutc=$m.scheduledStartUtc;dft_scheduledendutc=$m.scheduledEndUtc;dft_externalcalendarid=$m.externalCalendarId;dft_classificationcode=(O $m.classification);"dft_meetingseriesid@odata.bind"="/dft_meetingseries(dft_seriescode='$($m.seriesKey)')"};C "dft_meetings" $f}

Write-Host "Phase 9: FeatureTemplate" -ForegroundColor Cyan
foreach($s2 in $s.meetingSeriesFeatureDefaults){$f=@{dft_name=$s2.name;dft_statecode=(O $(if($s2.enabled){'enabled'}else{'disabled'}));"dft_meetingseriesid@odata.bind"="/dft_meetingseries(dft_seriescode='$($s2.seriesKey)')";"dft_featuredefinitionid@odata.bind"="/dft_featuredefinitions(dft_featurekeycode='$(O $s2.featureDefinitionKey)')"};C "dft_featuretemplates" $f}

Write-Host "Phase 10: Overrides" -ForegroundColor Cyan
foreach($o in $s.meetingFeatureOverrides){$f=@{dft_name=$o.name;dft_statecode=(O $(if($o.enabled){'enabled'}else{'disabled'}));dft_rationale=$o.reason;"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($o.meetingKey)')";"dft_featuredefinitionid@odata.bind"="/dft_featuredefinitions(dft_featurekeycode='$(O $o.featureDefinitionKey)')"};C "dft_meetingfeatureoverrides" $f}

Write-Host "Phase 11: Agenda" -ForegroundColor Cyan
foreach($a in $s.agendaItems){$f=@{dft_name=$a.title;dft_agendatitle=$a.title;dft_sequence=$a.sequence;dft_purposecode=(O $a.purpose);dft_decisionsought=[bool]$a.decisionSought;dft_statuscode=(O $a.status);"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($a.meetingKey)')"};C "dft_agendaitems" $f}

Write-Host "Phase 12: Papers" -ForegroundColor Cyan
foreach($p in $s.papers){$f=@{dft_name=$p.title;dft_documentid=$p.documentId;dft_documentsourcecode=(O $p.documentSource);dft_classificationcode=(O $p.classification);dft_paperstatuscode=(O $p.paperStatus);"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($p.meetingKey)')";"dft_agendaitemid@odata.bind"="/dft_agendaitems(dft_name='$($p.agendaItemKey)')"};C "dft_papers" $f}

Write-Host "Phase 13: Minutes" -ForegroundColor Cyan
foreach($n in $s.minutes){$f=@{dft_name=$n.title;dft_versionnumber=$n.version;dft_statuscode=(O $n.status);dft_classificationcode=(O $n.classification);"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($n.meetingKey)')"};C "dft_minuteses" $f}

Write-Host "Phase 14: Decisions" -ForegroundColor Cyan
foreach($d in $s.decisions){$f=@{dft_name=$d.title;dft_decisionnumber=$d.decisionNumber;dft_decisiontext=$d.decisionText;dft_statuscode=(O $d.status);"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($d.meetingKey)')";"dft_agendaitemid@odata.bind"="/dft_agendaitems(dft_name='$($d.agendaItemKey)')"};C "dft_decisions" $f}

Write-Host "Phase 15: Actions" -ForegroundColor Cyan
foreach($a in $s.actions){$f=@{dft_name=$a.title;dft_actionnumber=$a.actionNumber;dft_actionsummary=$a.title;dft_statuscode=(O $a.status);dft_prioritycode=(O $a.priority);"dft_meetingid@odata.bind"="/dft_meetings(dft_externalcalendarid='$($a.meetingKey)')";"dft_decisionid@odata.bind"="/dft_decisions(dft_decisionnumber='$($a.decisionKey)')"};C "dft_actionitems" $f}

Write-Host "`nResult: $ok created, $bad errors" -ForegroundColor Cyan
