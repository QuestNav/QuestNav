# ============================================================================
# QuestNav Code Formatter Script (PowerShell)
# ============================================================================
# This script provides dedicated code formatting functionality for QuestNav:
# - Java code formatting using Spotless
# - C# code formatting using CSharpier
#
# Prerequisites:
# - Java 17+ and Gradle (for Java formatting)
# - CSharpier (install with: dotnet tool install -g csharpier)
#
# Usage: .\format-code.ps1 [-Check] [-Fix] [-JavaOnly] [-CSharpOnly] [-Help]
# ============================================================================

[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Fix,
    [switch]$JavaOnly,
    [switch]$CSharpOnly,
    [switch]$Help
)

# Script configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)  # Go up two levels from docs/scripts to project root
$JavaDir = Join-Path $ProjectRoot "questnav-lib"
$UnityDir = Join-Path $ProjectRoot "unity"

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to check if a command exists
function Test-Command {
    param(
        [string]$Command,
        [string]$Description
    )
    
    try {
        $null = Get-Command $Command -ErrorAction Stop
        Write-ColorOutput "[OK] $Description found" "Green"
        return $true
    }
    catch {
        Write-ColorOutput "[ERROR] $Description not found in PATH" "Red"
        Write-ColorOutput "[ERROR] Please install $Command and ensure it's available in your PATH" "Red"
        return $false
    }
}

# Function to check Java formatting
function Test-JavaFormatting {
    param([string]$JavaDir)
    
    Write-ColorOutput "[INFO] Checking Java code formatting with Spotless..." "Yellow"
    Set-Location $JavaDir
    
    try {
        if ($IsWindows -or $env:OS -eq "Windows_NT") {
            $result = & .\gradlew.bat spotlessJavaCheck 2>&1
        } else {
            $result = & ./gradlew spotlessJavaCheck 2>&1
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "[OK] Java code formatting is correct" "Green"
            return $true
        } else {
            Write-ColorOutput "[ERROR] Java code formatting violations found:" "Red"
            Write-Host $result
            return $false
        }
    }
    catch {
        Write-ColorOutput "[ERROR] Failed to check Java formatting: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to fix Java formatting
function Fix-JavaFormatting {
    param([string]$JavaDir)
    
    Write-ColorOutput "[INFO] Fixing Java code formatting with Spotless..." "Yellow"
    Set-Location $JavaDir
    
    try {
        if ($IsWindows -or $env:OS -eq "Windows_NT") {
            $result = & .\gradlew.bat spotlessApply 2>&1
        } else {
            $result = & ./gradlew spotlessApply 2>&1
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "[SUCCESS] Java code formatting applied successfully" "Green"
            return $true
        } else {
            Write-ColorOutput "[ERROR] Failed to apply Java formatting:" "Red"
            Write-Host $result
            return $false
        }
    }
    catch {
        Write-ColorOutput "[ERROR] Failed to fix Java formatting: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to check C# formatting
function Test-CSharpFormatting {
    param([string]$UnityDir)
    
    Write-ColorOutput "[INFO] Checking C# code formatting with CSharpier..." "Yellow"
    Set-Location $UnityDir
    
    try {
        $result = & csharpier check Assets/QuestNav/ 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "[OK] C# code formatting is correct" "Green"
            return $true
        } else {
            Write-ColorOutput "[ERROR] C# code formatting violations found:" "Red"
            Write-Host $result
            return $false
        }
    }
    catch {
        Write-ColorOutput "[ERROR] Failed to check C# formatting: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to fix C# formatting
function Fix-CSharpFormatting {
    param([string]$UnityDir)
    
    Write-ColorOutput "[INFO] Fixing C# code formatting with CSharpier..." "Yellow"
    Set-Location $UnityDir
    
    try {
        $result = & csharpier format Assets/QuestNav/ 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "[SUCCESS] C# code formatting applied successfully" "Green"
            return $true
        } else {
            Write-ColorOutput "[ERROR] Failed to apply C# formatting:" "Red"
            Write-Host $result
            return $false
        }
    }
    catch {
        Write-ColorOutput "[ERROR] Failed to fix C# formatting: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to show help
function Show-Help {
    Write-Host ""
    Write-ColorOutput "QuestNav Code Formatter" "Cyan"
    Write-Host ""
    Write-Host "Usage: .\format-code.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Action Options (choose one):"
    Write-Host "  -Check        Check code formatting without making changes (default)"
    Write-Host "  -Fix          Automatically fix code formatting issues"
    Write-Host ""
    Write-Host "Scope Options:"
    Write-Host "  -JavaOnly     Format only Java code"
    Write-Host "  -CSharpOnly   Format only C# code"
    Write-Host "  (default)     Format both Java and C# code"
    Write-Host ""
    Write-Host "General Options:"
    Write-Host "  -Help         Show this help message"
    Write-Host ""
    Write-Host "Prerequisites:"
    Write-Host "  - Java 17+ and Gradle (for Java formatting)"
    Write-Host "  - CSharpier (install with: dotnet tool install -g csharpier)"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\format-code.ps1                Check all code formatting"
    Write-Host "  .\format-code.ps1 -Fix          Fix all code formatting issues"
    Write-Host "  .\format-code.ps1 -Check -JavaOnly   Check only Java formatting"
    Write-Host "  .\format-code.ps1 -Fix -CSharpOnly   Fix only C# formatting"
    Write-Host ""
}

# Show help if requested
if ($Help) {
    Show-Help
    exit 0
}

# Default to check if neither check nor fix is specified
if (!$Check -and !$Fix) {
    $Check = $true
}

# Show header
Write-Host ""
Write-ColorOutput "============================================================================" "Cyan"
if ($Check) {
    Write-ColorOutput "QuestNav Code Formatter - Checking Code" "Cyan"
} else {
    Write-ColorOutput "QuestNav Code Formatter - Fixing Code" "Cyan"
}
Write-ColorOutput "============================================================================" "Cyan"
Write-Host ""

# Check prerequisites
Write-ColorOutput "[INFO] Checking prerequisites..." "Yellow"
$prerequisitesFailed = $false

if (!$CSharpOnly -and !(Test-Command "java" "Java Runtime Environment")) { $prerequisitesFailed = $true }
if (!$JavaOnly -and !(Test-Command "csharpier" "CSharpier code formatter")) { $prerequisitesFailed = $true }

if ($prerequisitesFailed) {
    Write-ColorOutput "[ERROR] Some prerequisites are missing. Please install them and try again." "Red"
    exit 1
}

Write-ColorOutput "[INFO] All required prerequisites found." "Green"
Write-Host ""

try {
    $formatSuccess = $true
    
    # Check/Fix Java formatting
    if (!$CSharpOnly) {
        if ($Check) {
            if (!(Test-JavaFormatting $JavaDir)) { $formatSuccess = $false }
        } else {
            if (!(Fix-JavaFormatting $JavaDir)) { $formatSuccess = $false }
        }
        Set-Location $ProjectRoot
    }
    
    # Check/Fix C# formatting
    if (!$JavaOnly) {
        if ($Check) {
            if (!(Test-CSharpFormatting $UnityDir)) { $formatSuccess = $false }
        } else {
            if (!(Fix-CSharpFormatting $UnityDir)) { $formatSuccess = $false }
        }
        Set-Location $ProjectRoot
    }
    
    Write-Host ""
    if ($formatSuccess) {
        if ($Check) {
            Write-ColorOutput "[SUCCESS] All code formatting checks passed!" "Green"
        } else {
            Write-ColorOutput "[SUCCESS] All code formatting applied successfully!" "Green"
        }
    } else {
        if ($Check) {
            Write-ColorOutput "[ERROR] Code formatting violations found. Run with -Fix to automatically fix them." "Red"
            exit 1
        } else {
            Write-ColorOutput "[ERROR] Some formatting fixes failed. Please check the output above." "Red"
            exit 1
        }
    }
}
catch {
    Write-ColorOutput "[ERROR] $($_.Exception.Message)" "Red"
    Set-Location $ProjectRoot
    exit 1
}
finally {
    Set-Location $ProjectRoot
}
