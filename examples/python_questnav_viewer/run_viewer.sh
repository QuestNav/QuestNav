#!/bin/bash
# Quick start script for QuestNav Viewer

echo "QuestNav Viewer - Quick Start"
echo "=============================="
echo ""

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed!"
    echo "Please install Python 3.8 or newer."
    exit 1
fi

echo "Python version:"
python3 --version
echo ""

# Check if dependencies are installed
if ! python3 -c "import ntcore" 2>/dev/null; then
    echo "Installing dependencies..."
    pip3 install -r requirements.txt
    echo ""
fi

echo "Starting QuestNav Viewer..."
echo ""
python3 questnav_viewer.py "$@"

