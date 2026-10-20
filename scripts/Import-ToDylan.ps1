[CmdletBinding()] param([switch]$WhatIf)
$env = "https://environment.example.invalid/"
$root = Split-Path -Parent $PSScriptRoot
$token = az account get-access-token --resource $env --query accessToken -o tsv
$h = @{Authorization = "Bearer $token"; "Content-Type" = "application/json"}
$b = "$env/api/data/v9.2"
$s = Get-Content (Join-Path $root "powerplatform/src/seed/sample-governance-data.json") -Raw | ConvertFrom-Json
$c = @{}; $ok = 0; $bad = 0; $ups = 0

# Pre-populate cache with existing OrgPerson records
$existing = Invoke-RestMethod -Uri "$b/dft_orgpersons?`$select=dft_workemail&`$filter=startswith(dft_workemail,'alex.morgan') or startswith(dft_workemail,'priya.shah') or startswith(dft_workemail,'jordan.lee') or startswith(dft_workemail,'mia.chen')" -Headers $h -ErrorAction SilentlyContinue
foreach ($rec in $existing.value) { $id = $rec.PSObject.Properties | Where-Object { $_.Name -ne "@odata.etag" -and $_.Name -ne "dft_workemail" } | Select-Object -First 1; if ($id) { $c[$rec.dft_workemail] = $id.Value; $script:ups++ } }
Write-Host "Pre-loaded $ups existing record IDs" -ForegroundColor Cyan
function N { param($t,$f,$l=@{})
    $body = @{}; foreach ($kv in $f.GetEnumerator()) { $body[$kv.Key] = $kv.Value }
    foreach ($kv in $l.GetEnumerator()) {
        if ($c.ContainsKey($kv.Value)) { $body["$($kv.Key)@odata.bind"] = "/$t($($c[$kv.Value]))" }
        elseif ($kv.Value -match '@') { $body["$($kv.Key)@odata.bind"] = "/dft_orgpersons(dft_workemail='$($kv.Value)')" }
        else { $body["$($kv.Key)@odata.bind"] = "/dft_governancebodies(dft_bodycode='$($kv.Value)')" }
    }
    if ($WhatIf) { return }
    try {
        $resp = Invoke-RestMethod -Uri "$b/$t" -Method Post -Headers $h -Body ($body|ConvertTo-Json -Depth 3) -ErrorAction Stop
        Write-Host "  OK $t ($($f.dft_name))" -ForegroundColor Green; $script:ok++
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq 204) { Write-Host "  OK $t ($($f.dft_name))" -ForegroundColor Green; $script:ok++ }
        elseif ($code -eq 409) { Write-Host "  EXISTS $t ($($f.dft_name))" -ForegroundColor DarkGray; $script:ok++ }
        else { Write-Host "  FAIL $code $t ($($f.dft_name))" -ForegroundColor Yellow; $script:bad++ }
    }
}
function PM { param($k) $p = $s.members|Where-Object{$_.key -eq $k}; if($p){$p.mail}else{$null} }

Write-Host "Phase 1: People" -ForegroundColor Cyan
foreach($m in $s.members){N "dft_orgpersons" @{dft_name=$m.displayName;dft_workemail=$m.mail;dft_jobtitle=$m.jobTitle;dft_department=$m.department}}

Write-Host "Phase 2: Features" -ForegroundColor Cyan
foreach($d in $s.featureDefinitions){N "dft_featuredefinitions" @{dft_name=$d.name;dft_featurecode=$d.featureCode;dft_description=$d.description;dft_defaultenabled=[bool]$d.defaultEnabled;dft_sortorder=$d.sortOrder}}

Write-Host "Phase 3: Bodies" -ForegroundColor Cyan
foreach($g in $s.governanceBodies){N "dft_governancebodies" @{dft_name=$g.name;dft_bodycode=$g.key;dft_bodytype=$g.type;dft_status=$g.status}}

Write-Host "Phase 4: Committees" -ForegroundColor Cyan
foreach($x in $s.committees){N "dft_governancebodies" @{dft_name=$x.name;dft_bodycode=$x.key;dft_bodytype=$x.type;dft_status=$x.status} @{dft_parentgovernancebodyid=$x.governanceBodyKey}}

Write-Host "Phase 5: Membership" -ForegroundColor Cyan
foreach($m in $s.members){N "dft_committeememberships" @{dft_name="$($m.displayName) - $($m.role)";dft_membershiprole=$m.role;dft_startdate=$m.startDate;dft_membershipstatus=$m.membershipStatus;dft_votingrights=[bool]$m.votingRights;dft_isalternate=[bool]$m.isAlternate} @{dft_governancebodyid=$m.committeeKey;dft_orgpersonid=$m.mail}}

Write-Host "Phase 6: ToR" -ForegroundColor Cyan
foreach($t in $s.termsOfReference){N "dft_termsofreferences" @{dft_name=$t.title;dft_versionnumber=$t.version;dft_effectivefrom=$t.effectiveFrom;dft_reviewdueon=$t.reviewDate;dft_status=$t.status;dft_approvalstatus=$t.approvalStatus} @{dft_governancebodyid=$t.committeeKey}}

Write-Host "Phase 7: Series" -ForegroundColor Cyan
foreach($n in $s.meetingSeries){$f=@{dft_name=$n.name;dft_seriescode=$n.key;dft_meetingcadence=$n.cadence;dft_seriesstatus=$n.seriesStatus};$l=@{dft_governancebodyid=$n.committeeKey};if($n.seriesOwnerPersonKey){$l.dft_seriesownerid=PM $n.seriesOwnerPersonKey};N "dft_meetingseries" $f $l}

Write-Host "Phase 8: Meetings" -ForegroundColor Cyan
foreach($m in $s.meetings){$f=@{dft_name=$m.title;dft_meetingstatus=$m.status;dft_scheduledstartutc=$m.scheduledStartUtc;dft_scheduledendutc=$m.scheduledEndUtc;dft_externalcalendarid=$m.externalCalendarId;dft_classification=$m.classification};$l=@{dft_meetingseriesid=$m.seriesKey};if($m.chairPersonKey){$l.dft_chairpersonid=PM $m.chairPersonKey};N "dft_meetings" $f $l}

Write-Host "Phase 9: Agenda" -ForegroundColor Cyan
foreach($a in $s.agendaItems){$f=@{dft_name=$a.title;dft_agendatitle=$a.title;dft_sequence=$a.sequence;dft_purpose=$a.purpose;dft_decisionsought=[bool]$a.decisionSought;dft_status=$a.status};$l=@{dft_meetingid=$a.meetingKey};if($a.presenterPersonKey){$l.dft_presenterid=PM $a.presenterPersonKey};N "dft_agendaitems" $f $l}

Write-Host "Phase 10: Minutes" -ForegroundColor Cyan
foreach($n in $s.minutes){$f=@{dft_name=$n.title;dft_versionnumber=$n.version;dft_status=$n.status;dft_classification=$n.classification};$l=@{dft_meetingid=$n.meetingKey};if($n.approvedByPersonKey){$l.dft_approvedbyid=PM $n.approvedByPersonKey};N "dft_minuteses" $f $l}

Write-Host "Phase 11: Papers" -ForegroundColor Cyan
foreach($p in $s.papers){$f=@{dft_name=$p.title;dft_documentid=$p.documentId;dft_documentsource=$p.documentSource;dft_classification=$p.classification;dft_paperstatus=$p.paperStatus};$l=@{dft_meetingid=$p.meetingKey};if($p.agendaItemKey){$l.dft_agendaitemid=$p.agendaItemKey};N "dft_papers" $f $l}

Write-Host "Phase 12: Decisions" -ForegroundColor Cyan
foreach($d in $s.decisions){$f=@{dft_name=$d.title;dft_decisionnumber=$d.decisionNumber;dft_decisiontext=$d.decisionText;dft_status=$d.status};$l=@{dft_meetingid=$d.meetingKey};if($d.agendaItemKey){$l.dft_agendaitemid=$d.agendaItemKey};N "dft_decisions" $f $l}

Write-Host "Phase 13: Actions" -ForegroundColor Cyan
foreach($a in $s.actions){$f=@{dft_name=$a.title;dft_actionnumber=$a.actionNumber;dft_actionsummary=$a.title;dft_status=$a.status};$l=@{dft_meetingid=$a.meetingKey};if($a.decisionKey){$l.dft_decisionid=$a.decisionKey};N "dft_actionitems" $f $l}

Write-Host "`nResult: $ok created, $bad errors" -ForegroundColor Cyan
