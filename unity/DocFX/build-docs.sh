#!/bin/bash

# QuestNav DocFX Build Script
# This script generates C# API documentation and integrates it with the Docusaurus site

set -e  # Exit on any error

echo "Building QuestNav C# API Documentation"
echo "=========================================="

# Check if DocFX is installed
if ! command -v docfx &> /dev/null; then
    echo "DocFX not found. Installing..."
    dotnet tool install -g docfx
fi

# Check if we're in the right directory
if [ ! -f "docfx.json" ]; then
    echo "Error: docfx.json not found. Please run this script from unity/DocFX directory"
    exit 1
fi

echo "Generating API metadata from C# project..."
docfx metadata --logLevel Info

echo "Building documentation site..."
docfx build --logLevel Info

# Verify output
if [ -d "../../docs/static/api/csharp" ]; then
    API_FILES=$(find ../../docs/static/api/csharp/api -name "*.html" | wc -l)
    echo "Documentation built successfully!"
    echo "   Generated $API_FILES API pages"
    echo "   Output: docs/static/api/csharp/"
    echo ""
    
    # Clean up XML files to prevent stale builds
    echo "Cleaning up XML documentation..."
    if [ -f "preserved-xml/QuestNav.xml" ]; then
        rm preserved-xml/QuestNav.xml
        echo "   Removed preserved XML file"
    fi
    if [ -f "../Library/ScriptAssemblies/QuestNav.xml" ]; then
        rm ../Library/ScriptAssemblies/QuestNav.xml
        echo "   Removed working XML file"
    fi
    echo "   XML cleanup completed"
    echo ""
    
    echo "To preview the documentation:"
    echo "   1. Run 'npm start' in the docs/ directory"
    echo "   2. Navigate to http://localhost:3000/api/csharp/"
    echo ""
    echo "The documentation includes:"
    echo "   • QuestNav.Core - Main application logic"
    echo "   • QuestNav.Commands - Command processing"
    echo "   • QuestNav.Network - NetworkTables communication"
    echo "   • QuestNav.UI - User interface management"
    echo "   • QuestNav.Utils - Utility functions"
    echo "   • QuestNav.Native.NTCore - Native NetworkTables"
    echo "   • QuestNav.Protos.Generated - Protocol buffers"
    echo ""
    echo "Note: Future builds will require fresh Unity compilation to generate new XML documentation."
else
    echo "Error: Documentation output not found at docs/static/api/csharp/"
    exit 1
fi
