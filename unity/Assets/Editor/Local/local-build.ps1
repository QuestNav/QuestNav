param(
  [string]$UnityVersion = "6000.0.58f1",
  [switch]$CleanLibrary
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve paths
$RepoRoot   = (Get-Location).Path
$ProjectDir = Join-Path $RepoRoot "unity"
$OutputDir  = Join-Path $ProjectDir "build\Android"
$LogPath    = Join-Path $RepoRoot "local-unity-build.log"
$ApkPath    = Join-Path $OutputDir "QuestNav-local.apk"

# Locate Unity (override by setting $env:UNITY_EXE)
$UnityExe = $env:UNITY_EXE
if (-not $UnityExe) {
  $UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
}
if (-not (Test-Path $UnityExe)) {
  throw "Unity Editor not found at: $UnityExe. Install $UnityVersion with Android Build Support or set UNITY_EXE."
}

# Optional: simulate CI cleanliness
if ($CleanLibrary) {
  Write-Host "Cleaning Library to mimic CI..."
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $ProjectDir "Library")
}

# Restore NuGetForUnity packages (like CI)
try { dotnet --version | Out-Null } catch { throw "dotnet SDK is required for NuGetForUnity.Cli" }
dotnet tool update --global NuGetForUnity.Cli | Out-Null
$nugetCli = Join-Path $env:USERPROFILE ".dotnet\tools\nugetforunity"
& $nugetCli restore $ProjectDir

# Ensure output folder
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Headless Unity build via CI Editor method (mirrors GitHub action behavior)
Write-Host "Starting headless Unity build (CI.PerformAndroid)..."
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

# (Skip pre-check for Android module to avoid false positives; Unity log will report accurately)

$startTime = Get-Date

$env:QUESTNAV_DEVELOPMENT_BUILD = "1" # matches '-developmentBuild' in workflow

# Derive version like CI if not supplied: short hash + commit count
try {
  Push-Location $RepoRoot
  $commitHash = (git rev-parse --short=7 HEAD).Trim()
  $commitCount = [int](git rev-list --count HEAD)
  $version = "$commitHash-dev"
  $versionCode = 1000000 + $commitCount
  Pop-Location
} catch {
  $version = "local-dev"
  $versionCode = 1000001
}
$env:QUESTNAV_VERSION = $version
$env:QUESTNAV_VERSION_CODE = $versionCode

$unityArgs = @(
  "-batchmode",
  "-nographics",
  "-quit",
  "-projectPath", $ProjectDir,
  "-executeMethod", "CI.Build.PerformAndroid",
  "-logFile", $LogPath
)

Write-Host "Unity: $UnityExe"
Write-Host "Args : $($unityArgs -join ' ')"

$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow
$exit = $proc.ExitCode
$duration = (Get-Date) - $startTime

Write-Host "Unity exited with code $exit in $([int]$duration.TotalMinutes)m $([int]$duration.Seconds)s"
if (Test-Path $LogPath) {
  Write-Host "---- Tail of log ----"
  Get-Content $LogPath -Tail 200
} else {
  Write-Warning "Log file not created at $LogPath — Unity may have exited before initializing."
}

if ($exit -ne 0) {
  Write-Error "Unity build failed. See log: $LogPath"
  exit 1
}
if (-not (Test-Path $ApkPath)) {
  Write-Warning "APK not found at $ApkPath after successful exit code."
  Write-Warning "Check that Android is the active build target and scenes are included in Build Settings."
  Write-Error "APK not found. See log: $LogPath"
  exit 1
}

# Optional: preserve XML docs like CI (if produced)
$xmlIn  = Join-Path $ProjectDir "Library\ScriptAssemblies\QuestNav.xml"
$xmlOut = Join-Path $ProjectDir "DocFX\preserved-xml\QuestNav.xml"
if (Test-Path $xmlIn) {
  New-Item -ItemType Directory -Force -Path (Split-Path $xmlOut) | Out-Null
  Copy-Item $xmlIn $xmlOut -Force
  Write-Host "Preserved XML docs at $xmlOut"
}

Write-Host "SUCCESS: APK at $ApkPath"


