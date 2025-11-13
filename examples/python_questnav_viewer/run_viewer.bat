@echo off
REM Quick start script for QuestNav Viewer (Windows)

echo QuestNav Viewer - Quick Start
echo ==============================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed!
    echo Please install Python 3.8 or newer from python.org
    pause
    exit /b 1
)

echo Python version:
python --version
echo.

REM Check if dependencies are installed
python -c "import ntcore" >nul 2>&1
if errorlevel 1 (
    echo Installing dependencies...
    pip install -r requirements.txt
    echo.
)

echo Starting QuestNav Viewer...
echo.
python questnav_viewer.py %*

