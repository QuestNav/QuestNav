# QuestNav DocFX API Documentation

This directory contains the DocFX configuration for generating C# API documentation from the QuestNav Unity project.

## Overview

DocFX automatically generates comprehensive API documentation from XML documentation comments in the C# source code. The generated documentation is integrated into the main Docusaurus site at `/api/csharp/`.

## Files

- **`docfx.json`** - DocFX configuration file
- **`index.md`** - Homepage for the API documentation
- **`build-docs.sh`** - Linux/macOS build script
- **`build-docs.ps1`** - Windows PowerShell build script
- **`api/`** - Generated YAML metadata files (auto-generated)

## Local Development

### Prerequisites

1. Install .NET SDK 8.0 or later
2. Install DocFX: `dotnet tool install -g docfx`

### Building Documentation

**Windows (PowerShell):**
```powershell
cd unity/DocFX
.\build-docs.ps1
```

**Linux/macOS:**
```bash
cd unity/DocFX
chmod +x build-docs.sh
./build-docs.sh
```

**Manual Commands:**
```bash
cd unity/DocFX
docfx metadata  # Generate API metadata
docfx build     # Build HTML documentation
```

### Preview Documentation

After building, the documentation is available at:
- **Local files**: `docs/static/api/csharp/index.html`
- **Docusaurus dev server**: `http://localhost:3000/api/csharp/` (run `npm start` in `docs/`)

## CI/CD Integration

### GitHub Actions Workflow

The DocFX documentation is generated through coordinated workflows with proper dependency management:

#### 1. Main Build Workflow (`test-build.yml`)
- **Triggers on**: Push to `main` or pull requests
- **Calls**: Unity build workflow which automatically triggers DocFX generation

#### 2. Unity Build Workflow (`build-questnav-apk.yml`)
- **Preserves XML Documentation**: After Unity builds, copies `QuestNav.xml` from `Library/ScriptAssemblies/` to `DocFX/preserved-xml/`
- **Uploads XML Artifact**: Stores the XML file as a workflow artifact for later use
- **Prevents Cleanup**: Ensures XML documentation survives Unity's build cleanup process
- **Calls DocFX Workflow**: Automatically triggers DocFX generation after successful Unity build

#### 3. DocFX Build Workflow (`build-docfx-api.yml`)
- **Triggers via**:
  - Called by Unity build workflow (automatic after Unity builds)
  - Manual workflow dispatch (with force-build option)
  - Standalone docs workflow (for DocFX config changes only)

- **Process**:
  - Sets up .NET and DocFX
  - Restores Unity NuGet packages
  - **Checks for preserved XML**: Looks for XML documentation in `preserved-xml/` directory
  - **Fails if no XML**: Reports failure when XML documentation is not available (unless force-build=true)
  - Copies XML to expected location for DocFX processing
  - Generates API metadata from C# project (only if XML available)
  - Builds HTML documentation
  - **Cleans up XML files**: Removes preserved and working XML files to prevent stale builds
  - Commits updated docs back to repository
  - Uploads artifacts for review

- **Output**:
  - Updates `docs/static/api/csharp/` with latest API docs
  - Automatically triggers Docusaurus deployment
  - Provides PR comments with build status

#### 4. Standalone Docs Workflow (`build-docs-standalone.yml`)
- **Triggers on**: Changes to `unity/DocFX/**` files only
- **Purpose**: Updates documentation when only DocFX configuration changes
- **Behavior**: Uses existing preserved XML documentation

### Integration with Docusaurus

The generated documentation integrates seamlessly with the main Docusaurus site:

- **Static files** are placed in `docs/static/api/csharp/`
- **Served automatically** by Docusaurus at `/api/csharp/`
- **Search functionality** included with full-text search
- **Responsive design** matches Docusaurus theme

## XML Documentation Lifecycle

### Automatic XML Management

QuestNav implements automatic XML documentation lifecycle management to ensure documentation is always built from fresh, up-to-date XML:

1. **Generation**: Unity builds generate `QuestNav.xml` in `Library/ScriptAssemblies/`
2. **Preservation**: Unity workflow copies XML to `DocFX/preserved-xml/` to survive build cleanup
3. **Consumption**: DocFX workflow uses preserved XML to generate documentation
4. **Cleanup**: After successful DocFX build, XML files are automatically removed
5. **Regeneration**: Next Unity build creates fresh XML for subsequent documentation builds

### Why XML Cleanup Matters

- **Prevents Stale Documentation**: Ensures docs always reflect current code state
- **Forces Fresh Builds**: Requires new Unity compilation for documentation updates
- **Avoids Inconsistencies**: Eliminates risk of outdated XML generating incorrect docs
- **Clear Dependencies**: Makes Unity → DocFX dependency explicit and enforceable

## Configuration

### DocFX Settings

Key configuration in `docfx.json`:

```json
{
  "metadata": [{
    "src": [{"files": ["QuestNav.csproj"], "src": "../"}],
    "dest": "api",
    "includePrivateMembers": false,
    "namespaceLayout": "flattened"
  }],
  "build": {
    "output": "../../docs/static/api/csharp",
    "template": ["default"]
  }
}
```

### Customization

To customize the documentation:

1. **Styling**: Modify templates in DocFX or override CSS in Docusaurus
2. **Content**: Update XML documentation comments in C# source files
3. **Structure**: Modify `docfx.json` configuration
4. **Integration**: Update Docusaurus navigation to link to API docs

## Namespaces Covered

The documentation includes all public APIs from:

- **`QuestNav.Core`** - Main application logic and constants
- **`QuestNav.Commands`** - Command processing system
- **`QuestNav.Network`** - NetworkTables communication
- **`QuestNav.UI`** - User interface management
- **`QuestNav.Utils`** - Utility functions and extensions
- **`QuestNav.Native.NTCore`** - Native NetworkTables bindings
- **`QuestNav.Protos.Generated`** - Protocol buffer generated classes

## Troubleshooting

### Common Issues

1. **Build Failures**:
   - Ensure .NET SDK 8.0+ is installed
   - Check that Unity project compiles successfully
   - Verify NuGet packages are restored

2. **Missing Documentation**:
   - Add XML documentation comments to C# source files
   - Enable XML documentation generation in Unity project settings
   - Check that `QuestNav.xml` is generated in `Library/ScriptAssemblies/`
   - Verify XML file is preserved in `DocFX/preserved-xml/` after Unity builds
   - **Note**: XML files are automatically cleaned up after successful DocFX builds to prevent stale documentation

3. **Broken Links**:
   - Verify relative paths in `index.md`
   - Check that Docusaurus serves files from `static/` directory
   - Ensure cross-references use correct namespace names

### Manual Debugging

```bash
# Check DocFX version
docfx --version

# Verbose build output
docfx build --logLevel Verbose

# Serve locally for testing
docfx serve docs/static/api/csharp --port 8080
```

## Contributing

When adding new C# APIs:

1. **Add XML documentation** comments to all public members
2. **Follow conventions** for parameter descriptions and examples
3. **Test locally** by running the build scripts
4. **Verify output** in the generated documentation

The documentation will automatically update when changes are merged to the main branch.
