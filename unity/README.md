# QuestNav Unity Package Configuration

This document explains the Unity package configuration and architecture for **QuestNav** - a VR pose tracking application that streams Meta Quest headset motion data to FRC (FIRST Robotics Competition) robots via NetworkTables 4 (NT4) protocol.

## Project Overview

- **Unity Version**: 6000.2.7f2 (Unity 6) - **ONLY SUPPORTED VERSION**
- **Target Platform**: Meta Quest 3 and Quest 3S (Android) - **ONLY SUPPORTED HEADSETS**
- **Primary Function**: High-frequency VR pose streaming to FRC robots (default 120Hz MainUpdate loop)
- **Display Frequency**: 72-120Hz configurable (Quest hardware limitation, default 120Hz)
- **Architecture**: Dependency injection with interface-based design (no singletons)
- **Communication Protocol**: Protocol Buffers (protobuf) over NetworkTables 4
- **Web Interface**: Vue.js SPA served by EmbedIO HTTP server on port 5801

## What QuestNav Does

QuestNav leverages the Quest 3/3S headset's Visual-Inertial Odometry (VIO) system - the same technology that powers VR gaming - to provide precise robot localization. It:

1. **Captures** VR headset pose (position + rotation) using Quest's built-in tracking
2. **Converts** Unity coordinates to WPILib FRC field coordinates
3. **Publishes** pose data to NetworkTables at 120Hz (MainUpdate loop, configurable via constants)
4. **Receives** commands from robot (pose resets, etc.)
5. **Monitors** device health (battery, tracking status)
6. **Provides** web-based configuration interface at `http://<quest-ip>:5801/`

This enables FRC robots to use the Quest's precise VR tracking for autonomous navigation and localization.

**Supported Hardware**: Quest 3 and Quest 3S only. Quest 2 and Quest Pro are not supported.

## Meta XR SDK Configuration

### Current Package Configuration

This project uses **individual Meta XR SDK packages (v78.0.0)** instead of the all-in-one `com.meta.xr.sdk.all` package.

```json
"com.meta.xr.sdk.core": "78.0.0",
"com.meta.xr.sdk.interaction": "78.0.0",
"com.meta.xr.sdk.interaction.ovr": "78.0.0",
"com.meta.xr.sdk.platform": "78.0.0",
"com.meta.xr.mrutilitykit": "78.0.0"
```

### Why Individual Packages?

The all-in-one package (`com.meta.xr.sdk.all`) includes `com.meta.xr.simulator`, which has compilation errors in Unity 6000.2.7f2:

```
Library/PackageCache/com.meta.xr.simulator/Editor/Utils/ProcessUtils.cs(70,53): 
  error CS0103: The name 'path' does not exist in the current context
Library/PackageCache/com.meta.xr.simulator/Editor/Utils/ProcessUtils.cs(70,59): 
  error CS0103: The name 'args' does not exist in the current context
```

**Solution Benefits:**
- ✅ Eliminates Unity 6 compilation errors
- ✅ All Quest runtime functionality preserved (VR tracking, rendering, passthrough)
- ✅ APK builds work correctly on Quest devices
- ✅ Smaller package footprint (no unused editor-only tools)

**Trade-off:**
- ❌ No Meta XR Simulator for in-editor testing

This is **not a problem** for QuestNav because:
- VR pose tracking requires actual Quest 3/3S hardware
- NetworkTables communication must be tested with real robot
- Performance profiling must be done on Quest hardware
- Display frequency: 72-120Hz configurable (default 120Hz)
- MainUpdate loop: 120Hz (default, set via `QuestNavConstants.Timing.MAIN_UPDATE_HZ`)

## Key Unity Package Dependencies

### VR & Quest Support
- `com.meta.xr.sdk.core` (v78.0.0) - Oculus VR runtime (OVRCameraRig, OVRPlugin, OVRManager)
- `com.unity.xr.openxr` (v1.15.1) - OpenXR backend
- `com.unity.xr.meta-openxr` (v2.3.0) - Meta OpenXR extensions

### UI & Rendering
- `com.unity.render-pipelines.universal` (v17.2.0) - Universal Render Pipeline for Quest performance
- `com.unity.textmeshpro` (v5.0.0) - In-headset UI text rendering
- `com.unity.ugui` (v2.0.0) - Unity UI system (team number input, status display)

### Development Tools
- `com.unity.mobile.android-logcat` (v1.4.6) - Quest device logging via ADB
- `com.unity.inputsystem` (v1.14.2) - VR input handling
- `com.boxqkrtm.ide.cursor` - Cursor IDE integration (git source)

### Native Libraries (Not in Unity Packages)

These libraries are included as precompiled DLLs in `Assets/Plugins/`:

- **NetworkTables (ntcore)** - C++ native library for FRC robot communication
  - Platform: Android (arm64-v8a), Windows (x64), Linux (x64)
  - Location: `Assets/Plugins/Android/`, `Assets/Plugins/x86_64/`
  - Provides: NT4 protocol, publisher/subscriber pattern, connection management

- **Protocol Buffers (protobuf)** - Binary serialization
  - Package: `Google.Protobuf.Tools.3.33.0` (NuGet, in `Packages/`)
  - Provides: Efficient binary serialization for pose data and commands
  - Schema: `protos/*.proto` → Generated: `Assets/QuestNav/Protos/Generated/`

- **EmbedIO** - Lightweight HTTP server (WebServer assembly)
  - Location: `Assets/NuGet/EmbedIO.dll`
  - Provides: REST API, static file serving, CORS support
  - Endpoints: `/api/status`, `/api/config`, `/api/logs`, `/api/reset-pose`

- **Newtonsoft.Json** - JSON serialization (WebServer assembly)
  - Location: `Assets/NuGet/Newtonsoft.Json.dll`
  - Provides: Configuration persistence, API responses

## QuestNav Architecture

### Assembly Structure

QuestNav uses Unity Assembly Definitions for clean separation and build optimization:

```
QuestNav (Main Assembly)
├─ References: Unity.TextMeshPro, Unity.InputSystem, Oculus.VR, QuestNav.WebServer
├─ Core: QuestNav.cs (main orchestrator), QuestNavConstants.cs, WebServerConstants.cs
├─ Network: NetworkTableConnection.cs (NT4 communication)
├─ Commands: CommandProcessor.cs, PoseResetCommand.cs
├─ UI: UIManager.cs (in-headset UI), TagAlongUI.cs (UI positioning)
├─ Utils: Conversions.cs (Unity ↔ FRC coordinates), QueuedLogger.cs
├─ Protos: Generated protobuf classes
└─ Native: NTCore wrappers for native NetworkTables library

QuestNav.WebServer (Separate Assembly)
├─ References: Newtonsoft.Json, EmbedIO, Swan.Lite (precompiled DLLs)
├─ NO reference to main QuestNav assembly (prevents circular dependencies)
├─ Config: ReflectionBinding, ConfigStore, ConfigAttribute ([Config])
├─ Server: ConfigServer (EmbedIO HTTP server)
├─ Providers: StatusProvider (thread-safe status data), LogCollector (Unity logs)
└─ Models: ConfigModel (JSON serialization)
```

**Critical Design Pattern**: WebServer assembly **cannot** reference main QuestNav assembly. Uses **reverse dependency injection** via callbacks (`ExecutePoseResetToOrigin`) to invoke main assembly functionality.

### Component Architecture

**Central Orchestrator**: `QuestNav.cs` (MonoBehaviour)
- Instantiates all subsystems via dependency injection in `Awake()`
- Runs two update loops: `MainUpdate()` (100Hz), `SlowUpdate()` (3Hz)
- Manages lifecycle of all components

**Subsystems** (all use dependency injection):

1. **NetworkTableConnection** (`INetworkTableConnection`)
   - Publishes pose data at configurable frequency (default 100Hz)
   - Publishes device data at 3Hz (battery, tracking status)
   - Subscribes to robot commands (pose resets)
   - Handles NT4 protocol, connection management, topic publishing

2. **CommandProcessor** (`ICommandProcessor`)
   - Processes incoming commands from robot
   - Validates command freshness (TTL: 50ms default)
   - Executes `PoseResetCommand` to recenter VR tracking
   - Sends success/error responses back to robot

3. **UIManager** (`IUIManager`)
   - Manages in-headset UI (team number input, connection status, pose display)
   - Updates at 3Hz (non-critical, reduces CPU overhead)
   - Syncs configuration with WebServerConstants

4. **TagAlongUI** (`ITagAlongUI`)
   - Keeps UI panel in user's field of view
   - Updates every frame (MainUpdate @ 100Hz)
   - Smooth following with configurable thresholds

5. **WebServerManager** (`IWebServerManager`)
   - Orchestrates EmbedIO HTTP server on port 5801
   - Manages StatusProvider, LogCollector, ConfigServer
   - Handles Android APK file extraction (UI assets)
   - Provides callback pattern for pose reset requests from web interface

### Update Loop Architecture

QuestNav uses **dual-frequency update loops** for optimal performance:

```csharp
// High-frequency loop (120Hz default, set via QuestNavConstants.Timing.MAIN_UPDATE_HZ)
private void MainUpdate()
{
    UpdateFrameData();                          // Capture VR pose from OVRCameraRig
    networkTableConnection.PublishFrameData();  // Send to robot via NT4
    commandProcessor.ProcessCommands();         // Handle robot commands
    tagAlongUI.Periodic();                      // Update UI position
}

// Low-frequency loop (3Hz)
private void SlowUpdate()
{
    networkTableConnection.LoggerPeriodic();    // NT4 internal logging
    uiManager.UIPeriodic();                     // Update UI text/status
    UpdateDeviceData();                         // Battery, tracking status
    networkTableConnection.PublishDeviceData(); // Send device data to robot
    UpdateStatusProvider();                     // Update web interface data
    webServerManager?.Periodic();               // Handle web requests
    QueuedLogger.Flush();                       // Batch log output
}
```

**Configuration**:
- **MainUpdate frequency**: Set in `QuestNavConstants.Timing.MAIN_UPDATE_HZ` (default: 120Hz)
- **Display frequency**: Set in `QuestNavConstants.Display.DISPLAY_FREQUENCY` (default: 120Hz)
- Both are configurable via `WebServerConstants` for runtime adjustment

**Performance Optimization**:
- **Never** allocate objects in MainUpdate (120Hz loop)
- Heavy operations belong in SlowUpdate (3Hz) only
- Use `QueuedLogger` for batched logging (reduces console spam)
- Early exit conditions at top of update methods

### Communication Protocol

**Protobuf over NetworkTables 4**:

1. **Quest → Robot (High Frequency, 120Hz default)**:
   ```
   Topic: /QuestNav/frameData
   Type: ProtobufQuestNavFrameData
   Contains: frame_count, timestamp, pose3d (position + rotation)
   Note: Frequency set by QuestNavConstants.Timing.MAIN_UPDATE_HZ (default 120Hz)
   ```

2. **Quest → Robot (Low Frequency, 3Hz)**:
   ```
   Topic: /QuestNav/deviceData
   Type: ProtobufQuestNavDeviceData
   Contains: tracking_lost_counter, currently_tracking, battery_percent
   ```

3. **Robot → Quest (Commands)**:
   ```
   Topic: /QuestNav/request
   Type: ProtobufQuestNavCommand
   Contains: type, command_id, payload (e.g., pose_reset_payload)
   ```

4. **Quest → Robot (Responses)**:
   ```
   Topic: /QuestNav/response
   Type: ProtobufQuestNavCommandResponse
   Contains: command_id, success, error_message
   ```

**Coordinate System Conversion**:
- Unity: Y-up, left-handed, origin at VR tracking initialization
- FRC: Z-up, right-handed, origin at blue alliance wall
- Conversion: `Conversions.UnityToFrc3d()` / `Conversions.FrcToUnity3d()`

### Thread Safety

WebServer runs HTTP handlers on background threads. **Critical pattern**: Flag-based deferred execution.

```csharp
// WebServerManager.cs
private volatile bool poseResetRequested = false;

// Background thread (HTTP handler) sets flag
private void RequestPoseReset()
{
    poseResetRequested = true;
}

// Main thread (SlowUpdate @ 3Hz) checks flag and invokes Unity APIs
public void Periodic()
{
    if (poseResetRequested)
    {
        poseResetRequested = false;
        poseResetCallback?.Invoke(); // Calls QuestNav.ExecutePoseResetToOrigin()
    }
}
```

**Rules**:
- ✅ Use `volatile` for simple boolean flags
- ✅ Use `lock` for complex shared data (StatusProvider, LogCollector)
- ✅ Defer Unity API calls to main thread via Periodic() polling
- ❌ **Never** call Unity APIs (Transform, GameObject) from background threads

## Runtime Configuration System

QuestNav uses **[Config] attribute** for runtime-configurable settings exposed to web interface:

```csharp
// WebServerConstants.cs
[Config(
    DisplayName = "Team Number",
    Description = "FRC team number for NetworkTables connection (1-25599)",
    Category = "QuestNav",
    Min = 1,
    Max = 25599,
    ControlType = "input",
    Order = 1
)]
public static int webConfigTeamNumber = 9999;

[Config(
    DisplayName = "Display Frequency (Hz)",
    Description = "Quest headset display refresh rate (72-120Hz)",
    Category = "QuestNav",
    Min = 72f,
    Max = 120f,
    Step = 1f,
    ControlType = "slider",
    RequiresRestart = true,
    Order = 4
)]
public static float displayFrequency = 120.0f;
```

**Note**: MainUpdate frequency is automatically tied to the display frequency set by `OVRPlugin.systemDisplayFrequency`. Quest 3/3S hardware supports 72Hz, 90Hz, and 120Hz.

**Configuration Persistence**:
- Stored in JSON at `Application.persistentDataPath/config.json`
- Loaded on app startup via `ConfigStore.LoadConfig()`
- Applied to static fields via `ReflectionBinding.ApplyValues()`
- Modified via web interface at `http://<quest-ip>:5801/`

## Web Interface Features

Accessible at `http://<quest-ip>:5801/` from any device on the same network:

- **Real-time Status**: Live pose visualization, FPS, battery, tracking status
- **Configuration Editor**: Modify all `[Config]` settings at runtime
- **Log Viewer**: View Unity logs in real-time, export for debugging
- **Pose Reset**: Trigger pose reset to origin (0,0,0)
- **System Info**: Unity version, Quest model, device specs
- **App Restart**: Restart QuestNav application remotely

Built with Vue.js (Vite), served by EmbedIO, assets extracted from APK on Android startup.

## Web UI Architecture

### Technology Stack

**Frontend** (`questnav-web-ui/`):
- **Framework**: Vue 3 with Composition API (`<script setup>`)
- **State Management**: Pinia stores for reactive configuration state
- **Build Tool**: Vite 5.0 for fast development and optimized production builds
- **TypeScript**: Full type safety with `.ts` and `.vue` files
- **Styling**: Scoped CSS with modern gradients and animations

**Backend** (Unity C#):
- **HTTP Server**: EmbedIO (lightweight .NET web server)
- **JSON Serialization**: Newtonsoft.Json
- **Static File Serving**: Serves built Vite assets from `StreamingAssets/ui/`
- **REST API**: All endpoints under `/api/*`

### Build Pipeline

```
questnav-web-ui/ (Development)
├─ src/
│  ├─ App.vue           # Main application component
│  ├─ main.ts           # Entry point with Pinia
│  ├─ types.ts          # TypeScript type definitions
│  ├─ components/
│  │  ├─ ConfigForm.vue      # Configuration editor
│  │  ├─ ConfigField.vue     # Individual config fields
│  │  ├─ StatusView.vue      # Real-time headset status
│  │  └─ LogsView.vue        # Log viewer component
│  ├─ api/
│  │  └─ config.ts           # API client for HTTP requests
│  └─ stores/
│     └─ config.ts           # Pinia store for config state
├─ vite.config.ts       # Build configuration
└─ package.json

  ↓ npm run build (Vite)

unity/Assets/StreamingAssets/ui/ (Production Build)
├─ index.html
├─ assets/
│  ├─ main.js         # Bundled Vue app (no hash for Unity compatibility)
│  └─ main.css        # Bundled styles (no hash for Unity compatibility)
├─ logo.svg
└─ logo-dark.svg

  ↓ Unity Build (Android APK)

Quest Device: /data/app/.../files/ui/  (Extracted from APK on startup)
└─ (same structure as StreamingAssets/ui/)
```

**Key Build Configuration** (`vite.config.ts`):
```typescript
build: {
  outDir: '../unity/Assets/StreamingAssets/ui',
  emptyOutDir: true,
  rollupOptions: {
    output: {
      // NO HASH in filenames for Unity compatibility
      // These filenames are referenced in WebServerManager.cs
      entryFileNames: 'assets/[name].js',
      chunkFileNames: 'assets/[name].js',
      assetFileNames: 'assets/[name].[ext]'
    }
  }
}
```

**Why no hashes?** Unity's `WebServerManager.cs` extracts specific files from the Android APK on startup. Using consistent filenames (no content hashes) simplifies the extraction logic.

### API Endpoints

All endpoints are defined in `ConfigServer.cs` and consumed by `questnav-web-ui/src/api/config.ts`:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/schema` | GET | Get configuration schema ([Config] fields) |
| `/api/config` | GET | Get current configuration values |
| `/api/config` | POST | Update a configuration value |
| `/api/status` | GET | Get real-time headset status (pose, battery, tracking) |
| `/api/logs` | GET | Get recent Unity log messages |
| `/api/logs` | DELETE | Clear all Unity logs |
| `/api/info` | GET | Get server/device information |
| `/api/restart` | POST | Restart QuestNav application |
| `/api/reset-pose` | POST | Reset VR tracking to origin |

**Example Response** (`/api/status`):
```json
{
  "position": { "x": 1.23, "y": 0.45, "z": 2.67 },
  "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
  "eulerAngles": { "pitch": 0, "yaw": 90, "roll": 0 },
  "isTracking": true,
  "trackingLostEvents": 0,
  "batteryPercent": 85,
  "batteryLevel": 0.85,
  "batteryStatus": "Discharging",
  "batteryCharging": false,
  "networkConnected": true,
  "ipAddress": "192.168.1.100",
  "teamNumber": 9999,
  "robotIpAddress": "10.99.99.2",
  "fps": 119.8,
  "frameCount": 12345,
  "connectedClients": 1,
  "timestamp": 1732463520000
}
```

### Component Overview

**`App.vue`** - Main application shell:
- Header with logo, connection status, action buttons (Refresh, Info, Restart)
- Content area with `ConfigForm` component
- Footer with documentation links
- Modal for server information display
- Connection status polling (checks `/api/schema` every 3 seconds)

**`ConfigForm.vue`** - Configuration editor:
- Loads schema from `/api/schema` on mount
- Groups config fields by category
- Renders appropriate input controls (slider, checkbox, text input)
- Auto-saves changes to `/api/config` with debouncing
- Shows "Requires Restart" indicators for settings that need app restart

**`ConfigField.vue`** - Individual config field:
- Renders control based on `controlType` (slider, input, checkbox)
- Shows min/max/step for numeric fields
- Displays current value with live updates
- Handles value validation and type conversion

**`StatusView.vue`** - Real-time status display (if implemented):
- Shows live pose data (position, rotation)
- Battery level with visual indicator
- Tracking status with icon
- NetworkTables connection status
- Performance metrics (FPS, frame count)

**`LogsView.vue`** - Log viewer (if implemented):
- Fetches logs from `/api/logs`
- Displays with severity levels (Info, Warning, Error)
- Filters by log level
- Clear all logs button
- Auto-scroll to latest

### Development Workflow

**Local Development** (with Quest device):
```bash
cd questnav-web-ui

# Install dependencies
npm install

# Start dev server with proxy to Quest device
# Set VITE_API_TARGET environment variable to your Quest IP
VITE_API_TARGET=http://192.168.1.100:5801 npm run dev

# Access at http://localhost:5173
# API requests are proxied to Quest device
```

**Build for Production**:
```bash
cd questnav-web-ui

# Build for production (outputs to unity/Assets/StreamingAssets/ui/)
npm run build

# Then build Unity project to package into APK
```

**Vite Dev Server Proxy** (`vite.config.ts`):
```typescript
server: {
  port: 5173,
  proxy: {
    '/api': {
      target: process.env.VITE_API_TARGET || 'http://192.168.1.100:5801',
      changeOrigin: true,
      secure: false
    }
  }
}
```

This allows local development with live reload while API requests go to the actual Quest device.

### Android APK File Extraction

On Android (Quest), assets are compressed in the APK and cannot be served directly. `WebServerManager.cs` extracts UI files on startup:

```csharp
// WebServerManager.cs - ExtractAndroidUIFiles()
private IEnumerator ExtractAndroidUIFiles(string targetPath)
{
    // Force delete old UI files to ensure fresh extraction
    if (Directory.Exists(targetPath))
        Directory.Delete(targetPath, true);
    
    // Extract index.html
    yield return ExtractAndroidFile("ui/index.html", indexPath);
    
    // Extract Vite output files (consistent naming without hashes)
    yield return ExtractAndroidFile("ui/assets/main.css", Path.Combine(assetsDir, "main.css"));
    yield return ExtractAndroidFile("ui/assets/main.js", Path.Combine(assetsDir, "main.js"));
    
    // Extract logo files
    yield return ExtractAndroidFile("ui/logo.svg", Path.Combine(targetPath, "logo.svg"));
    yield return ExtractAndroidFile("ui/logo-dark.svg", Path.Combine(targetPath, "logo-dark.svg"));
}
```

**Extraction Flow**:
1. Unity build packages `StreamingAssets/ui/` into APK
2. On Quest app startup, `WebServerManager.ExtractAndroidUIFiles()` runs
3. Files extracted from APK to `Application.persistentDataPath/ui/`
4. EmbedIO serves files from persistent storage path
5. Fresh extraction on each app start ensures UI is always up-to-date

### CORS Configuration

**Development Mode** (optional):
```csharp
// WebServerConstants.cs
[Config(
    DisplayName = "Enable CORS Dev Mode",
    Description = "Allow web UI development from your computer",
    Category = "General",
    ControlType = "checkbox",
    Order = 41
)]
public static bool enableCORSDevMode = false;
```

When enabled, CORS headers allow requests from `localhost:5173` for local development.

### Web UI Type Safety

**TypeScript Definitions** (`questnav-web-ui/src/types.ts`):
```typescript
export interface ConfigFieldSchema {
  path: string
  displayName: string
  description: string
  category: string
  type: 'int' | 'float' | 'double' | 'bool' | 'string' | 'color'
  controlType: 'slider' | 'input' | 'checkbox' | 'select' | 'color'
  min?: number
  max?: number
  step?: number
  defaultValue: any
  currentValue: any
  requiresRestart: boolean
  order: number
}

export interface HeadsetStatus {
  position: { x: number, y: number, z: number }
  rotation: { x: number, y: number, z: number, w: number }
  eulerAngles: { pitch: number, yaw: number, roll: number }
  isTracking: boolean
  trackingLostEvents: number
  batteryPercent: number
  batteryLevel: number
  batteryStatus: string
  networkConnected: boolean
  ipAddress: string
  teamNumber: number
  robotIpAddress: string
  fps: number
  frameCount: number
  timestamp: number
}
```

These match the C# types in `ConfigServer.cs`, ensuring type safety across the stack.

### Styling

Modern dark theme with QuestNav branding:
- **Primary Color**: `#33A1FD` (QuestNav blue)
- **Background**: Dark gradients (`#1a1f21` to `#0d1214`)
- **Typography**: System fonts with fallbacks
- **Animations**: Smooth transitions, pulsing connection indicator
- **Responsive**: Mobile-friendly with breakpoints at 768px and 1024px

**CSS Variables** (`style.css`):
```css
:root {
  --primary-color: #33A1FD;
  --primary-light: #5BB3FF;
  --bg-color: #0d1214;
  --bg-secondary: #1a1f21;
  --text-primary: #e8eaed;
  --success-color: #4caf50;
  --danger-color: #dc3545;
  --warning-color: #ffc107;
}
```

## Future Considerations

### Meta XR SDK Updates
- Monitor Meta XR SDK releases for Unity 6 compatibility fixes
- When `com.meta.xr.simulator` works with Unity 6000.2.7f2, can revert to `com.meta.xr.sdk.all`
- Track Meta's [Unity package changelog](https://packages.unity.com)

### Unity Version Updates
- **Current Version**: Unity 6000.2.7f2 is the **ONLY supported version**
- Do NOT upgrade Unity without thorough testing:
  - ✅ Meta XR SDK compatibility
  - ✅ NetworkTables native library compatibility (ntcore)
  - ✅ EmbedIO HTTP server functionality
  - ✅ APK builds and Quest deployment
  - ✅ NetworkTables communication with robot
  - ✅ Performance profiling (100Hz pose updates, 120Hz display)

## Package Management

### DO NOT

- ❌ Manually edit `packages-lock.json` (Unity manages this automatically)
- ❌ Add `com.meta.xr.simulator` until Unity 6 compatibility confirmed
- ❌ Modify `.meta` files (Unity generates these automatically)
- ❌ Upgrade Unity version without comprehensive testing
- ❌ Change assembly references (circular dependencies)

### DO

- ✅ Update packages via Unity Package Manager UI
- ✅ Test APK builds on actual Quest hardware after updates
- ✅ Verify NetworkTables connection after Unity/package updates
- ✅ Check web interface functionality at `http://<quest-ip>:5801/`
- ✅ Profile performance on Quest (100Hz pose updates, 120Hz display)
- ✅ Test with actual robot hardware (NetworkTables communication)

## Development Workflow

1. **Code Changes**: Modify C# files in `Assets/QuestNav/`
2. **Build APK**: `File → Build Settings → Android → Build`
3. **Deploy to Quest**: `adb install -r build/Android/QuestNav.apk`
4. **Test on Hardware**: Launch app on Quest, connect to robot
5. **Monitor Logs**: `adb logcat -s Unity QuestNav` or web interface
6. **Web Configuration**: Access `http://<quest-ip>:5801/` for runtime config

### ADB Commands

```bash
# Install APK
adb install -r build/Android/QuestNav.apk

# View logs
adb logcat -s Unity QuestNav

# Find Quest IP
adb shell ip addr show wlan0

# Clear app data (reset configuration)
adb shell pm clear gg.questnav.QuestNav

# Force stop app
adb shell am force-stop gg.questnav.QuestNav
```

## Additional Resources

- **[QuestNav Documentation](https://questnav.gg/)** - Complete user guide
- **[Discord Community](https://discord.gg/hD3FtR7YAZ)** - Support and discussion
- [Meta XR SDK Documentation](https://developer.oculus.com/documentation/unity/)
- [Unity 6 Release Notes](https://unity.com/releases/editor/whats-new)
- [FRC NetworkTables Specification](https://docs.wpilib.org/en/stable/docs/software/networktables/)
- [Protocol Buffers Guide](https://protobuf.dev/)

## Performance Targets

**Supported Headsets** (Quest 3 and Quest 3S only):
- **Display Frequency**: 72Hz, 90Hz, or 120Hz (configurable via `WebServerConstants.displayFrequency`, default 120Hz)
- **MainUpdate Loop**: 120Hz (set via `QuestNavConstants.Timing.MAIN_UPDATE_HZ`)
- **SlowUpdate Loop**: 3Hz (set via `QuestNavConstants.Timing.SLOW_UPDATE_HZ`)
- **Pose Streaming**: Matches MainUpdate frequency (120Hz default)
- **RAM**: 8GB (Quest 3/3S)
- **Processor**: Qualcomm Snapdragon XR2 Gen 2

**Update Frequencies**:
- **MainUpdate**: 120Hz - pose tracking, NetworkTables publishing, command processing
- **SlowUpdate**: 3Hz - device monitoring (battery, tracking status), UI updates, web server operations
- **NetworkTables Latency**: < 10ms typical

**Quest Hardware Specs**:
- **Quest 3**: Display resolution 2064×2208 per eye, 120Hz max refresh rate
- **Quest 3S**: Display resolution 1832×1920 per eye, 120Hz max refresh rate
- Both support 72Hz, 90Hz, and 120Hz display modes

**Note**: Quest 2 and Quest Pro are **NOT** supported. Only Quest 3 and Quest 3S are compatible with QuestNav.
