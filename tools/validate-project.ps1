[CmdletBinding()]
param(
    [switch]$SkipDocs,
    [switch]$SkipBrowser,
    [switch]$SkipUnity,
    [ValidateRange(30, 3600)]
    [int]$UnityTimeoutSeconds = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$unityLog = Join-Path $repoRoot 'Logs\project-validation-unity.log'

function Assert-LastExitCode {
    param([Parameter(Mandatory = $true)][string]$Step)

    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Resolve-UnityEditor {
    $versionLine = Select-String -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt') `
        -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if ($null -eq $versionLine) {
        throw 'ProjectSettings/ProjectVersion.txt does not contain m_EditorVersion.'
    }

    $editorVersion = $versionLine.Matches[0].Groups[1].Value.Trim()
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH)) {
        $candidates += $env:UNITY_EDITOR_PATH
    }
    $candidates += "C:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unity $editorVersion was not found. Install it with Unity Hub or set UNITY_EDITOR_PATH."
}

Push-Location $repoRoot
try {
    Write-Host "Validating MiniMapGame at $repoRoot"

    if (-not $SkipDocs) {
        Write-Host '[1/5] Building documentation with strict warnings...'
        & python -m mkdocs build --strict
        Assert-LastExitCode -Step 'MkDocs strict build'
    }

    if (-not $SkipBrowser) {
        Write-Host '[2/5] Checking browser-preview JavaScript syntax...'
        $javascriptFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'browser-preview') -Filter '*.js'
        foreach ($file in $javascriptFiles) {
            & node --check $file.FullName
            Assert-LastExitCode -Step "JavaScript syntax check for $($file.Name)"
        }
    }

    Write-Host '[3/5] Checking canonical state and specification references...'
    $requiredDocuments = @(
        'docs\ai\AGENT_RULES.md',
        'docs\project-context.md',
        'docs\runtime-state.md',
        'docs\spec-index.json'
    )
    foreach ($relativePath in $requiredDocuments) {
        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath))) {
            throw "Required canonical document is missing: $relativePath"
        }
    }

    $projectContext = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'docs\project-context.md')
    $runtimeState = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'docs\runtime-state.md')
    $projectLane = [regex]::Match($projectContext, '(?m)^- Active lane:\s*(.+)$')
    $runtimeLane = [regex]::Match($runtimeState, '(?m)^- lane:\s*(.+)$')
    $projectSlice = [regex]::Match($projectContext, '(?m)^- Active slice:\s*(.+)$')
    $runtimeSlice = [regex]::Match($runtimeState, '(?m)^- slice:\s*(.+)$')
    if (-not $projectLane.Success -or -not $runtimeLane.Success -or
        $projectLane.Groups[1].Value.Trim() -ne $runtimeLane.Groups[1].Value.Trim()) {
        throw 'Active lane differs between project-context.md and runtime-state.md.'
    }
    if (-not $projectSlice.Success -or -not $runtimeSlice.Success -or
        $projectSlice.Groups[1].Value.Trim() -ne $runtimeSlice.Groups[1].Value.Trim()) {
        throw 'Active slice differs between project-context.md and runtime-state.md.'
    }

    $specIndex = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'docs\spec-index.json') | ConvertFrom-Json
    foreach ($entry in $specIndex) {
        $sourcePath = ($entry.file -split '#', 2)[0]
        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $sourcePath))) {
            throw "Specification $($entry.id) references a missing file: $sourcePath"
        }
    }
    $runtimeSpecCount = [regex]::Match($runtimeState, '(?m)^- spec_entries:\s*(\d+)$')
    if (-not $runtimeSpecCount.Success -or [int]$runtimeSpecCount.Groups[1].Value -ne $specIndex.Count) {
        throw "runtime-state.md spec_entries does not match spec-index.json ($($specIndex.Count))."
    }

    Write-Host '[4/5] Checking Unity asset metadata coverage...'
    $missingMeta = @(
        Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets') -Recurse -Force |
            Where-Object {
                -not $_.Name.EndsWith('.meta') -and
                -not (Test-Path -LiteralPath ($_.FullName + '.meta'))
            }
    )
    if ($missingMeta.Count -gt 0) {
        $missingMeta.FullName | ForEach-Object { Write-Host "Missing .meta: $_" }
        throw "Unity asset metadata check found $($missingMeta.Count) missing .meta file(s)."
    }

    $metaFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets') -Recurse -Force -Filter '*.meta')
    $orphanMeta = @(
        $metaFiles | Where-Object {
            $assetPath = $_.FullName.Substring(0, $_.FullName.Length - '.meta'.Length)
            -not (Test-Path -LiteralPath $assetPath)
        }
    )
    if ($orphanMeta.Count -gt 0) {
        $orphanMeta.FullName | ForEach-Object { Write-Host "Orphan .meta: $_" }
        throw "Unity asset metadata check found $($orphanMeta.Count) orphan .meta file(s)."
    }

    $metadataGuids = foreach ($metaFile in $metaFiles) {
        $guidMatch = Select-String -LiteralPath $metaFile.FullName -Pattern '^guid:\s*([0-9a-fA-F]{32})$' |
            Select-Object -First 1
        if ($null -eq $guidMatch) {
            throw "Unity metadata file has no valid GUID: $($metaFile.FullName)"
        }
        [pscustomobject]@{
            Guid = $guidMatch.Matches[0].Groups[1].Value.ToLowerInvariant()
            Path = $metaFile.FullName
        }
    }
    $duplicateGuids = @($metadataGuids | Group-Object Guid | Where-Object Count -gt 1)
    if ($duplicateGuids.Count -gt 0) {
        foreach ($duplicate in $duplicateGuids) {
            Write-Host "Duplicate Unity GUID: $($duplicate.Name)"
            $duplicate.Group.Path | ForEach-Object { Write-Host "  $_" }
        }
        throw "Unity asset metadata check found $($duplicateGuids.Count) duplicate GUID(s)."
    }

    if (-not $SkipUnity) {
        Write-Host '[5/5] Importing and compiling with the project Unity version...'
        $unityEditor = Resolve-UnityEditor
        $logDirectory = Split-Path -Parent $unityLog
        if (-not (Test-Path -LiteralPath $logDirectory)) {
            New-Item -ItemType Directory -Path $logDirectory | Out-Null
        }
        Remove-Item -LiteralPath $unityLog -ErrorAction SilentlyContinue

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $unityEditor
        $startInfo.Arguments = "-batchmode -nographics -quit -projectPath `"$repoRoot`" -logFile `"$unityLog`""
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true

        $unityProcess = [System.Diagnostics.Process]::Start($startInfo)
        if (-not $unityProcess.WaitForExit($UnityTimeoutSeconds * 1000)) {
            $unityProcess.Kill()
            throw "Unity validation exceeded $UnityTimeoutSeconds seconds. See $unityLog."
        }
        if ($unityProcess.ExitCode -ne 0) {
            if (Test-Path -LiteralPath $unityLog) {
                Get-Content -LiteralPath $unityLog -Tail 80 | Write-Host
            }
            throw "Unity import/compile failed with exit code $($unityProcess.ExitCode). See $unityLog."
        }
    }

    Write-Host 'Validation passed.'
}
finally {
    Pop-Location
}
