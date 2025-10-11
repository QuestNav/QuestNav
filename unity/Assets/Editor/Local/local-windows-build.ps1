param(
  [string]$WorkflowPath = ".github/workflows/build-questnav-apk.yml",
  [string]$ProjectPath = "unity",
  [string]$UnityExe = "C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.58f1\\Editor\\Unity.exe",
  [switch]$Development
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-UnityBatchBuild([string]$unityExe, [string]$projPath, [string]$logPath) {
  if (Test-Path $logPath) { Remove-Item $logPath -Force }
  $null = New-Item -ItemType File -Path $logPath -Force
  $args = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projPath,
    "-executeMethod", "CI.Build.PerformAndroid",
    "-logFile", $logPath
  )
  & $unityExe @args
  $exit = $LASTEXITCODE
  Write-Host "---- Tail of Unity log ----"
  if (Test-Path $logPath) { Get-Content $logPath -Tail 200 } else { Write-Warning "Log file not found at $logPath" }
  return $exit
}

function Get-UnityVersionFromWorkflow([string]$ymlPath) {
  if (-not (Test-Path $ymlPath)) { throw "Workflow YAML not found: $ymlPath" }
  $content = Get-Content -Raw -Path $ymlPath
  $m = [regex]::Match($content, "unityVersion:\s*([0-9\.a-zA-Z\-]+)")
  if ($m.Success) { return $m.Groups[1].Value }
  return $null
}

Write-Host "Reading workflow: $WorkflowPath"
$wfUnity = Get-UnityVersionFromWorkflow -ymlPath $WorkflowPath
if ($wfUnity) {
  $defaultExe = Join-Path "C:\\Program Files\\Unity\\Hub\\Editor" $wfUnity
  $defaultExe = Join-Path $defaultExe "Editor\\Unity.exe"
  if (Test-Path $defaultExe) { $UnityExe = $defaultExe }
}

if (-not (Test-Path $UnityExe)) {
  throw "Unity Editor not found at: $UnityExe"
}

# Clean Unity caches and build artifacts for a from-scratch build
Write-Host "Cleaning Unity project artifacts for a fresh build..."
$pathsToRemove = @(
  (Join-Path -Path $ProjectPath -ChildPath 'Library'),
  (Join-Path -Path $ProjectPath -ChildPath 'Temp'),
  (Join-Path -Path $ProjectPath -ChildPath 'obj'),
  (Join-Path -Path $ProjectPath -ChildPath 'Logs'),
  (Join-Path -Path $ProjectPath -ChildPath 'build')
)
foreach ($p in $pathsToRemove) {
  if (Test-Path $p) {
    try {
      Remove-Item -Recurse -Force -LiteralPath $p -ErrorAction Stop
      Write-Host "Removed: $p"
    } catch {
      Write-Warning "Failed to remove $($p): $($_.Exception.Message)"
    }
  }
}

# Compute version like CI (short hash + commit count)
Push-Location $PSScriptRoot
try {
  $commitHash = (git rev-parse --short=7 HEAD).Trim()
  $commitCount = [int](git rev-list --count HEAD)
  $env:QUESTNAV_VERSION = "$commitHash-dev"
  $env:QUESTNAV_VERSION_CODE = (1000000 + $commitCount)
} finally { Pop-Location }

# Match CI environment settings
$env:OVR_DISABLE_PROJECT_SETUP = "1"

 

# Ensure NuGetForUnity CLI is available and restore
Write-Host "Ensuring NuGetForUnity CLI is installed..."
dotnet tool install --global NuGetForUnity.Cli | Out-Null
$toolPath = Join-Path $env:USERPROFILE ".dotnet\\tools"
$nugetCli = Join-Path $toolPath "nugetforunity.exe"
if (Test-Path $nugetCli) {
  & $nugetCli restore $ProjectPath
} else {
  nugetforunity restore $ProjectPath
}

# Build
$repoRoot = (Get-Location).Path
$logPath = Join-Path $repoRoot "local-unity-build.log"
# Ensure a log file exists so tailing never errors, and start fresh each run
if (Test-Path $logPath) { Remove-Item $logPath -Force }
$null = New-Item -ItemType File -Path $logPath -Force
Write-Host "Running Unity batch build: $UnityExe"
if ($Development.IsPresent) { $env:QUESTNAV_DEVELOPMENT_BUILD = "1" }
$exit = Invoke-UnityBatchBuild -unityExe $UnityExe -projPath $ProjectPath -logPath $logPath

if ($exit -ne 0) { throw "Unity build failed with exit code $exit. See $logPath" }

# Wait for Unity to finish or APK to appear to avoid false negatives on long imports
$apk = Join-Path $ProjectPath "build/Android/QuestNav-local.apk"
$deadline = (Get-Date).AddMinutes(60)
while (-not (Test-Path $apk) -and (Get-Date) -lt $deadline) {
  $u = Get-Process -Name Unity -ErrorAction SilentlyContinue
  if (-not $u) { break }
  Start-Sleep -Seconds 10
}

if (-not (Test-Path $apk)) { Write-Warning "APK missing at expected path after build. See $logPath" }

if (Test-Path $apk) {
  Write-Host "SUCCESS: APK at $apk"
} else {
  throw "Expected APK not found at $apk. See $logPath"
}


