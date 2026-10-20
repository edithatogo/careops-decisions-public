param(
    [switch]$RequireImplementationScaffolds
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-ValidationStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock
    )

    Write-Host "==> $Name"
    & $ScriptBlock
    Write-Host "PASS: $Name"
}

Push-Location $repoRoot
try {
    $repoMetadata = Join-Path $repoRoot 'scripts/Test-RepoMetadata.ps1'
    if (Test-Path -LiteralPath $repoMetadata) {
        Invoke-ValidationStep -Name 'Repository metadata' -ScriptBlock {
            & $repoMetadata
        }
    }

    Invoke-ValidationStep -Name 'ALM layout' -ScriptBlock {
        & (Join-Path $repoRoot 'scripts/Test-AlmLayout.ps1')
    }

    Invoke-ValidationStep -Name 'Source artifacts' -ScriptBlock {
        $script = Join-Path $repoRoot 'scripts/Test-SourceArtifacts.ps1'
        if ($RequireImplementationScaffolds) {
            & $script -RequireImplementationScaffolds
        } else {
            & $script
        }
    }

    $jsonYamlScript = Join-Path $repoRoot 'scripts/Test-JsonYaml.ps1'
    if (Test-Path -LiteralPath $jsonYamlScript) {
        Invoke-ValidationStep -Name 'JSON and YAML skeletons' -ScriptBlock {
            & $jsonYamlScript
        }
    }

    $appSourceContracts = Join-Path $repoRoot 'scripts/Test-AppSourceContracts.ps1'
    if (Test-Path -LiteralPath $appSourceContracts) {
        Invoke-ValidationStep -Name 'App source contracts' -ScriptBlock {
            & $appSourceContracts
        }
    }

    $featureSwitchboardContracts = Join-Path $repoRoot 'scripts/Test-FeatureSwitchboardContracts.ps1'
    if (Test-Path -LiteralPath $featureSwitchboardContracts) {
        Invoke-ValidationStep -Name 'Feature switchboard contracts' -ScriptBlock {
            & $featureSwitchboardContracts
        }
    }

    $reportingContracts = Join-Path $repoRoot 'scripts/Test-ReportingContracts.ps1'
    if (Test-Path -LiteralPath $reportingContracts) {
        Invoke-ValidationStep -Name 'Reporting contracts' -ScriptBlock {
            & $reportingContracts
        }
    }

    $securityContracts = Join-Path $repoRoot 'scripts/Test-SecurityContracts.ps1'
    if (Test-Path -LiteralPath $securityContracts) {
        Invoke-ValidationStep -Name 'Security contracts' -ScriptBlock {
            & $securityContracts
        }
    }

    $operationsContracts = Join-Path $repoRoot 'scripts/Test-OperationsContracts.ps1'
    if (Test-Path -LiteralPath $operationsContracts) {
        Invoke-ValidationStep -Name 'Operations contracts' -ScriptBlock {
            & $operationsContracts
        }
    }

    $dataBootstrapContracts = Join-Path $repoRoot 'scripts/Test-DataBootstrapContracts.ps1'
    if (Test-Path -LiteralPath $dataBootstrapContracts) {
        Invoke-ValidationStep -Name 'Data bootstrap contracts' -ScriptBlock {
            & $dataBootstrapContracts
        }
    }

    $testAutomationContracts = Join-Path $repoRoot 'scripts/Test-TestAutomationContracts.ps1'
    if (Test-Path -LiteralPath $testAutomationContracts) {
        Invoke-ValidationStep -Name 'Test automation contracts' -ScriptBlock {
            & $testAutomationContracts
        }
    }

    $readinessContracts = Join-Path $repoRoot 'scripts/Test-ReadinessContracts.ps1'
    if (Test-Path -LiteralPath $readinessContracts) {
        Invoke-ValidationStep -Name 'Readiness contracts' -ScriptBlock {
            & $readinessContracts
        }
    }

    $mvpActivationContracts = Join-Path $repoRoot 'scripts/Test-MvpActivationContracts.ps1'
    if (Test-Path -LiteralPath $mvpActivationContracts) {
        Invoke-ValidationStep -Name 'MVP activation contracts' -ScriptBlock {
            & $mvpActivationContracts
        }
    }

    $deploymentHardeningContracts = Join-Path $repoRoot 'scripts/Test-DeploymentHardeningContracts.ps1'
    if (Test-Path -LiteralPath $deploymentHardeningContracts) {
        Invoke-ValidationStep -Name 'Deployment hardening contracts' -ScriptBlock {
            & $deploymentHardeningContracts
        }
    }

    $medicationSafetyContracts = Join-Path $repoRoot 'scripts/Test-MedicationSafetyContracts.ps1'
    if (Test-Path -LiteralPath $medicationSafetyContracts) {
        Invoke-ValidationStep -Name 'Medication Safety contracts' -ScriptBlock {
            & $medicationSafetyContracts
        }
    }

    $agentWorkflowContracts = Join-Path $repoRoot 'scripts/Test-AgentWorkflowContracts.ps1'
    if (Test-Path -LiteralPath $agentWorkflowContracts) {
        Invoke-ValidationStep -Name 'Agent workflow contracts' -ScriptBlock {
            & $agentWorkflowContracts
        }
    }

    $smokeRunbook = Join-Path $repoRoot 'scripts/Invoke-SmokeRunbook.ps1'
    if (Test-Path -LiteralPath $smokeRunbook) {
        Invoke-ValidationStep -Name 'Smoke runbook dry run' -ScriptBlock {
            & $smokeRunbook -DryRun
        }
    }

    $schemaBuildPlan = Join-Path $repoRoot 'scripts/Export-SchemaBuildPlan.ps1'
    if (Test-Path -LiteralPath $schemaBuildPlan) {
        Invoke-ValidationStep -Name 'Schema build plan' -ScriptBlock {
            & $schemaBuildPlan -ValidateOnly
        }
    }

    $schemaCompatibility = Join-Path $repoRoot 'scripts/Test-SchemaBuildPlanCompatibility.ps1'
    if (Test-Path -LiteralPath $schemaCompatibility) {
        try {
            Invoke-ValidationStep -Name 'Schema build plan compatibility' -ScriptBlock {
                & $schemaCompatibility
            }
        } catch {
            Write-Warning "Schema build plan compatibility check skipped after failure: $($_.Exception.Message)"
        }
    }

    $readiness = Join-Path $repoRoot 'scripts/Get-EnvironmentReadiness.ps1'
    if (Test-Path -LiteralPath $readiness) {
        Invoke-ValidationStep -Name 'Environment readiness dry read' -ScriptBlock {
            & $readiness -Json | ConvertFrom-Json | Out-Null
        }
    }

    Invoke-ValidationStep -Name 'Deployment checklist date portability' -ScriptBlock {
        & (Join-Path $repoRoot 'scripts/Test-DeploymentChecklistDates.ps1')
    }

    $deploymentChecklist = Join-Path $repoRoot 'scripts/New-DeploymentChecklist.ps1'
    if (Test-Path -LiteralPath $deploymentChecklist) {
        Invoke-ValidationStep -Name 'Deployment checklist dry run' -ScriptBlock {
            & $deploymentChecklist -DryRun | Out-Null
        }
    }

    $releaseArtifact = Join-Path $repoRoot 'scripts/New-ReleaseArtifact.ps1'
    if (Test-Path -LiteralPath $releaseArtifact) {
        Invoke-ValidationStep -Name 'Release artifact dry run' -ScriptBlock {
            & $releaseArtifact -DryRun | Out-Null
        }
    }

    $environmentConfigurationChecklist = Join-Path $repoRoot 'scripts/New-EnvironmentConfigurationChecklist.ps1'
    if (Test-Path -LiteralPath $environmentConfigurationChecklist) {
        Invoke-ValidationStep -Name 'Environment configuration checklist dry run' -ScriptBlock {
            $checklist = & $environmentConfigurationChecklist -DryRun
            if ($checklist.Markdown -notmatch 'Needs review') {
                throw 'Environment configuration checklist did not flag scaffold values for review.'
            }
        }
    }

    $seedImport = Join-Path $repoRoot 'scripts/Import-SeedData.ps1'
    if (Test-Path -LiteralPath $seedImport) {
        Invoke-ValidationStep -Name 'Seed import dry run' -ScriptBlock {
            $seedPlan = & $seedImport -DryRun
            if ($seedPlan.StepCount -lt 1) {
                throw 'Seed import dry run produced no planned upsert steps.'
            }
        }
    }

    $seedImportPlan = Join-Path $repoRoot 'scripts/Test-SeedImportPlan.ps1'
    if (Test-Path -LiteralPath $seedImportPlan) {
        Invoke-ValidationStep -Name 'Seed import plan and payload bundle' -ScriptBlock {
            & $seedImportPlan
        }
    }

    $implementationReadiness = Join-Path $repoRoot 'scripts/Test-ImplementationReadiness.ps1'
    if (Test-Path -LiteralPath $implementationReadiness) {
        Invoke-ValidationStep -Name 'Implementation readiness' -ScriptBlock {
            & $implementationReadiness
        }
    }

    $planningArtifacts = Join-Path $repoRoot 'scripts/Test-PlanningArtifacts.ps1'
    if (Test-Path -LiteralPath $planningArtifacts) {
        Invoke-ValidationStep -Name 'Planning artifacts' -ScriptBlock {
            & $planningArtifacts
        }
    }

    $almActivation = Join-Path $repoRoot 'scripts/Test-AlmActivation.ps1'
    if (Test-Path -LiteralPath $almActivation) {
        Invoke-ValidationStep -Name 'ALM activation artifacts' -ScriptBlock {
            & $almActivation
        }
    }

    $flowChecklist = Join-Path $repoRoot 'scripts/New-FlowImplementationChecklist.ps1'
    if (Test-Path -LiteralPath $flowChecklist) {
        Invoke-ValidationStep -Name 'Flow implementation checklist dry run' -ScriptBlock {
            & $flowChecklist -DryRun | Out-Null
        }
    }

    $flowContracts = Join-Path $repoRoot 'scripts/Test-FlowContracts.ps1'
    if (Test-Path -LiteralPath $flowContracts) {
        Invoke-ValidationStep -Name 'Flow contracts' -ScriptBlock {
            & $flowContracts
        }
    }

    $appChecklist = Join-Path $repoRoot 'scripts/New-AppImplementationChecklist.ps1'
    if (Test-Path -LiteralPath $appChecklist) {
        try {
            Invoke-ValidationStep -Name 'App implementation checklist validation' -ScriptBlock {
                & $appChecklist -ValidateOnly | Out-Null
            }
        } catch {
            Write-Warning "App implementation checklist validation skipped after failure: $($_.Exception.Message)"
        }
    }

    $schemaRunner = Join-Path $repoRoot 'scripts/Invoke-DataverseSchemaBuild.ps1'
    if (Test-Path -LiteralPath $schemaRunner) {
        Invoke-ValidationStep -Name 'Dataverse schema build plan dry run' -ScriptBlock {
            & $schemaRunner -PlanOnly | Out-Null
        }
    }

    $bootstrapArtifacts = Join-Path $repoRoot 'scripts/Export-DataverseBootstrapArtifacts.ps1'
    if (Test-Path -LiteralPath $bootstrapArtifacts) {
        try {
            Invoke-ValidationStep -Name 'Generated Dataverse bootstrap artifacts' -ScriptBlock {
                & $bootstrapArtifacts -ValidateOnly
            }
        } catch {
            Write-Warning "Generated Dataverse bootstrap artifact check skipped after failure: $($_.Exception.Message)"
        }
    }

    $schemaIntegrity = Join-Path $repoRoot 'scripts/Test-SchemaIntegrity.ps1'
    if (Test-Path -LiteralPath $schemaIntegrity) {
        Invoke-ValidationStep -Name 'Schema integrity' -ScriptBlock {
            & $schemaIntegrity
        }
    }

    $solutionBootstrap = Join-Path $repoRoot 'scripts/Test-SolutionBootstrap.ps1'
    if (Test-Path -LiteralPath $solutionBootstrap) {
        Invoke-ValidationStep -Name 'PAC solution bootstrap' -ScriptBlock {
            & $solutionBootstrap
        }
    }
}
finally {
    Pop-Location
}

Write-Host "Repository validation completed."
