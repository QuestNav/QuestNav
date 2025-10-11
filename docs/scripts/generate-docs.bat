@echo off
REM ============================================================================
REM QuestNav Documentation Generator Script
REM ============================================================================
REM This script automatically generates documentation for all QuestNav components:
REM - Protocol Buffer documentation (HTML)
REM - Java API documentation (Javadoc)
REM - C# API documentation (DocFX)
REM
REM Prerequisites:
REM - protoc (Protocol Buffer compiler)
REM - protoc-gen-doc (Go package: github.com/pseudomuto/protoc-gen-doc/cmd/protoc-gen-doc)
REM - Java 17+ and Gradle (for Java docs)
REM - .NET SDK and DocFX (for C# docs)
REM
REM Usage: generate-docs.bat [--clean] [--proto-only] [--java-only] [--csharp-only] [--check-format] [--fix-format] [--skip-format-check]
REM ============================================================================

setlocal enabledelayedexpansion

REM Script configuration
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%..\.."
set "DOCS_DIR=%PROJECT_ROOT%\docs\static\api"
set "PROTO_DIR=%PROJECT_ROOT%\protos"
set "JAVA_DIR=%PROJECT_ROOT%\questnav-lib"
set "UNITY_DIR=%PROJECT_ROOT%\unity"
set "DOCFX_DIR=%UNITY_DIR%\DocFX"

REM Output directories
set "PROTO_OUT_DIR=%DOCS_DIR%\proto"
set "JAVA_OUT_DIR=%DOCS_DIR%\java"
set "CSHARP_OUT_DIR=%DOCS_DIR%\csharp"

REM Parse command line arguments
set "CLEAN_FIRST=false"
set "PROTO_ONLY=false"
set "JAVA_ONLY=false"
set "CSHARP_ONLY=false"
set "CHECK_FORMAT=false"
set "FIX_FORMAT=false"
set "SKIP_FORMAT_CHECK=false"

:parse_args
if "%~1"=="" goto :args_done
if /i "%~1"=="--clean" set "CLEAN_FIRST=true"
if /i "%~1"=="--proto-only" set "PROTO_ONLY=true"
if /i "%~1"=="--java-only" set "JAVA_ONLY=true"
if /i "%~1"=="--csharp-only" set "CSHARP_ONLY=true"
if /i "%~1"=="--check-format" set "CHECK_FORMAT=true"
if /i "%~1"=="--fix-format" set "FIX_FORMAT=true"
if /i "%~1"=="--skip-format-check" set "SKIP_FORMAT_CHECK=true"
if /i "%~1"=="--help" goto :show_help
if /i "%~1"=="-h" goto :show_help
shift
goto :parse_args
:args_done

REM Show header
echo.
echo ============================================================================
echo QuestNav Documentation Generator
echo ============================================================================
echo.

REM Clean existing documentation if requested
if "%CLEAN_FIRST%"=="true" (
    echo [INFO] Cleaning existing documentation...
    if exist "%PROTO_OUT_DIR%" rmdir /s /q "%PROTO_OUT_DIR%"
    if exist "%JAVA_OUT_DIR%" rmdir /s /q "%JAVA_OUT_DIR%"
    if exist "%CSHARP_OUT_DIR%" rmdir /s /q "%CSHARP_OUT_DIR%"
    echo [INFO] Cleanup completed.
    echo.
)

REM Create output directories
echo [INFO] Creating output directories...
if not exist "%DOCS_DIR%" mkdir "%DOCS_DIR%"
if not exist "%PROTO_OUT_DIR%" mkdir "%PROTO_OUT_DIR%"
if not exist "%JAVA_OUT_DIR%" mkdir "%JAVA_OUT_DIR%"
if not exist "%CSHARP_OUT_DIR%" mkdir "%CSHARP_OUT_DIR%"

REM Check prerequisites
echo [INFO] Checking prerequisites...

REM Only check formatting tools if we're doing format operations
if "%CHECK_FORMAT%"=="true" goto :check_format_tools
if "%FIX_FORMAT%"=="true" goto :check_format_tools
if "%SKIP_FORMAT_CHECK%"=="false" if "%CHECK_FORMAT%"=="false" if "%FIX_FORMAT%"=="false" goto :check_format_tools
goto :check_doc_tools

:check_format_tools
call :check_command csharpier "CSharpier code formatter"
if !errorlevel! neq 0 exit /b 1

:check_doc_tools
REM Only check documentation tools if we're generating docs
if "%CHECK_FORMAT%"=="true" goto :prereq_done
if "%FIX_FORMAT%"=="true" goto :prereq_done

call :check_command protoc "Protocol Buffer compiler (protoc)"
if !errorlevel! neq 0 exit /b 1

call :check_command protoc-gen-doc "Protocol Buffer documentation generator (protoc-gen-doc)"
if !errorlevel! neq 0 exit /b 1

call :check_command java "Java Runtime Environment"
if !errorlevel! neq 0 exit /b 1

call :check_command docfx "DocFX documentation generator"
if !errorlevel! neq 0 exit /b 1

:prereq_done
echo [INFO] All required prerequisites found.
echo.

REM Handle format-only operations
if "%CHECK_FORMAT%"=="true" goto :format_only
if "%FIX_FORMAT%"=="true" goto :format_only
goto :check_formatting_before_docs

:format_only
echo ============================================================================
if "%CHECK_FORMAT%"=="true" (
    echo Checking Code Formatting
) else (
    echo Fixing Code Formatting
)
echo ============================================================================
echo.

set "FORMAT_SUCCESS=true"

REM Check/Fix Java formatting
if "%PROTO_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    if "%CHECK_FORMAT%"=="true" (
        call :check_java_formatting
        if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
    ) else (
        call :fix_java_formatting
        if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
    )
)

REM Check/Fix C# formatting
if "%PROTO_ONLY%"=="false" if "%JAVA_ONLY%"=="false" (
    if "%CHECK_FORMAT%"=="true" (
        call :check_csharp_formatting
        if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
    ) else (
        call :fix_csharp_formatting
        if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
    )
)

echo.
if "%FORMAT_SUCCESS%"=="true" (
    if "%CHECK_FORMAT%"=="true" (
        echo [SUCCESS] All code formatting checks passed!
    ) else (
        echo [SUCCESS] All code formatting applied successfully!
    )
    goto :end
) else (
    if "%CHECK_FORMAT%"=="true" (
        echo [ERROR] Code formatting violations found. Run with --fix-format to automatically fix them.
    ) else (
        echo [ERROR] Some formatting fixes failed. Please check the output above.
    )
    exit /b 1
)

:check_formatting_before_docs
REM Run formatting checks before documentation generation (unless skipped)
if "%SKIP_FORMAT_CHECK%"=="true" goto :generate_docs

echo ============================================================================
echo Pre-Documentation Formatting Checks
echo ============================================================================
echo.

set "FORMAT_SUCCESS=true"

REM Check Java formatting
if "%PROTO_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    call :check_java_formatting
    if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
)

REM Check C# formatting
if "%PROTO_ONLY%"=="false" if "%JAVA_ONLY%"=="false" (
    call :check_csharp_formatting
    if !errorlevel! neq 0 set "FORMAT_SUCCESS=false"
)

if "%FORMAT_SUCCESS%"=="false" (
    echo.
    echo [ERROR] Code formatting violations found. Please fix them before generating documentation.
    echo [INFO] Run 'generate-docs.bat --fix-format' to automatically fix formatting issues.
    exit /b 1
)

echo.
echo [SUCCESS] All formatting checks passed. Proceeding with documentation generation...
echo.

:generate_docs
REM Generate Protocol Buffer Documentation
if "%JAVA_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    echo ============================================================================
    echo Generating Protocol Buffer Documentation
    echo ============================================================================
    echo.
    
    echo [INFO] Generating protobuf documentation...
    cd /d "%PROJECT_ROOT%"
    
    protoc --doc_out="%PROTO_OUT_DIR%" --doc_opt=html,index.html -I=protos protos/*.proto
    if !errorlevel! neq 0 (
        echo [ERROR] Failed to generate protocol buffer documentation
        exit /b 1
    )
    
    echo [SUCCESS] Protocol buffer documentation generated successfully
    echo [INFO] Output: %PROTO_OUT_DIR%\index.html
    echo.
)

REM Generate Java Documentation
if "%PROTO_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    echo ============================================================================
    echo Generating Java API Documentation
    echo ============================================================================
    echo.
    
    echo [INFO] Generating Java documentation with Gradle...
    cd /d "%JAVA_DIR%"
    
    call gradlew.bat javadoc
    if !errorlevel! neq 0 (
        echo [ERROR] Failed to generate Java documentation
        exit /b 1
    )
    
    echo [INFO] Copying Java documentation to output directory...
    cd /d "%PROJECT_ROOT%"
    xcopy /e /i /y "%JAVA_DIR%\build\docs\javadoc\*" "%JAVA_OUT_DIR%\"
    if !errorlevel! neq 0 (
        echo [ERROR] Failed to copy Java documentation
        exit /b 1
    )
    
    echo [SUCCESS] Java documentation generated successfully
    echo [INFO] Output: %JAVA_OUT_DIR%\index.html
    echo.
)

REM Generate C# Documentation
if "%PROTO_ONLY%"=="false" if "%JAVA_ONLY%"=="false" (
    echo ============================================================================
    echo Generating C# API Documentation
    echo ============================================================================
    echo.
    
    echo [INFO] Generating C# documentation with DocFX...
    cd /d "%DOCFX_DIR%"
    
    docfx docfx.json
    if !errorlevel! neq 0 (
        echo [ERROR] Failed to generate C# documentation
        exit /b 1
    )
    
    echo [SUCCESS] C# documentation generated successfully
    echo [INFO] Output: %CSHARP_OUT_DIR%\index.html
    echo.
)

REM Final summary
echo ============================================================================
echo Documentation Generation Complete
echo ============================================================================
echo.
echo Generated documentation is available at:

if "%JAVA_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    echo   Protocol Buffers: %PROTO_OUT_DIR%\index.html
)

if "%PROTO_ONLY%"=="false" if "%CSHARP_ONLY%"=="false" (
    echo   Java API:        %JAVA_OUT_DIR%\index.html
)

if "%PROTO_ONLY%"=="false" if "%JAVA_ONLY%"=="false" (
    echo   C# API:          %CSHARP_OUT_DIR%\index.html
)

echo.
echo [SUCCESS] All documentation generated successfully!
goto :end

REM Function to check if a command exists
:check_command
where %1 >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] %2 not found in PATH
    echo [ERROR] Please install %1 and ensure it's available in your PATH
    exit /b 1
)
echo [OK] %2 found
exit /b 0

REM Function to check Java formatting
:check_java_formatting
echo [INFO] Checking Java code formatting with Spotless...
cd /d "%JAVA_DIR%"
call gradlew.bat spotlessJavaCheck >nul 2>&1
if !errorlevel! neq 0 (
    echo [ERROR] Java code formatting violations found
    call gradlew.bat spotlessJavaCheck
    cd /d "%PROJECT_ROOT%"
    exit /b 1
)
echo [OK] Java code formatting is correct
cd /d "%PROJECT_ROOT%"
exit /b 0

REM Function to fix Java formatting
:fix_java_formatting
echo [INFO] Fixing Java code formatting with Spotless...
cd /d "%JAVA_DIR%"
call gradlew.bat spotlessApply
if !errorlevel! neq 0 (
    echo [ERROR] Failed to apply Java formatting
    cd /d "%PROJECT_ROOT%"
    exit /b 1
)
echo [SUCCESS] Java code formatting applied successfully
cd /d "%PROJECT_ROOT%"
exit /b 0

REM Function to check C# formatting
:check_csharp_formatting
echo [INFO] Checking C# code formatting with CSharpier...
cd /d "%UNITY_DIR%"
csharpier check Assets/QuestNav/ >nul 2>&1
if !errorlevel! neq 0 (
    echo [ERROR] C# code formatting violations found
    csharpier check Assets/QuestNav/
    cd /d "%PROJECT_ROOT%"
    exit /b 1
)
echo [OK] C# code formatting is correct
cd /d "%PROJECT_ROOT%"
exit /b 0

REM Function to fix C# formatting
:fix_csharp_formatting
echo [INFO] Fixing C# code formatting with CSharpier...
cd /d "%UNITY_DIR%"
csharpier format Assets/QuestNav/
if !errorlevel! neq 0 (
    echo [ERROR] Failed to apply C# formatting
    cd /d "%PROJECT_ROOT%"
    exit /b 1
)
echo [SUCCESS] C# code formatting applied successfully
cd /d "%PROJECT_ROOT%"
exit /b 0

REM Show help information
:show_help
echo.
echo QuestNav Documentation Generator ^& Code Formatter
echo.
echo Usage: generate-docs.bat [OPTIONS]
echo.
echo Documentation Options:
echo   --clean        Clean existing documentation before generating new docs
echo   --proto-only   Generate only Protocol Buffer documentation
echo   --java-only    Generate only Java API documentation
echo   --csharp-only  Generate only C# API documentation
echo.
echo Formatting Options:
echo   --check-format      Check code formatting without making changes
echo   --fix-format        Automatically fix code formatting issues
echo   --skip-format-check Skip formatting checks during documentation generation
echo.
echo General Options:
echo   --help, -h     Show this help message
echo.
echo Prerequisites:
echo   - protoc (Protocol Buffer compiler)
echo   - protoc-gen-doc (install with: go install github.com/pseudomuto/protoc-gen-doc/cmd/protoc-gen-doc@latest)
echo   - Java 17+ and Gradle (for Java documentation)
echo   - .NET SDK and DocFX (for C# documentation)
echo   - CSharpier (install with: dotnet tool install -g csharpier)
echo.
echo Examples:
echo   generate-docs.bat                    Generate all documentation (with format checks)
echo   generate-docs.bat --clean           Clean and generate all documentation
echo   generate-docs.bat --check-format    Only check code formatting
echo   generate-docs.bat --fix-format      Only fix code formatting
echo   generate-docs.bat --skip-format-check  Generate docs without format checks
echo   generate-docs.bat --java-only       Generate only Java documentation
echo   generate-docs.bat --clean --proto-only  Clean and generate only Protocol Buffer docs
echo.
goto :end

:end
cd /d "%PROJECT_ROOT%"
endlocal
