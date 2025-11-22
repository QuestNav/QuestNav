# QuestNav Python Examples

Python implementation of QuestNav for FRC robots, demonstrating how to receive and process Quest headset tracking data.

## Overview

This directory contains:

1. **`questnav.py`** - Complete Python implementation of the Java `questnav-lib`
2. **`questnav_viewer.py`** - GUI application demonstrating all library features
3. **`*_pb2.py`** - Generated Protocol Buffer classes for QuestNav messages

The implementation uses official [RobotPy](https://github.com/robotpy/mostrobotpy) libraries to ensure compatibility with FRC robot code.

## Features

### QuestNav Library (`questnav.py`)

Complete Python port of the Java questnav-lib with all methods:

**Data Retrieval:**
- `get_all_unread_pose_frames()` - Get pose tracking frames
- `get_battery_percent()` - Battery level (0-100%)
- `get_frame_count()` - Frame counter
- `get_tracking_lost_counter()` - Tracking loss events
- `get_latency()` - Connection latency in ms

**Status Monitoring:**
- `is_connected()` - Quest connection status
- `is_tracking()` - VIO tracking status

**Commands:**
- `set_pose(pose3d)` - Reset Quest pose
- `command_periodic()` - Process command responses

### QuestNav Viewer (`questnav_viewer.py`)

GUI application demonstrating all features:
- Real-time pose visualization (position & rotation)
- Connection and tracking status indicators
- Battery level monitoring
- Frame rate and latency display
- Diagnostic statistics (frame drops, tracking lost events)
- Pose reset commands (origin or custom)
- Event log with pause/resume/export

## Installation

### Requirements

- Python 3.8 or newer
- Meta Quest headset with QuestNav Unity app

### Install Dependencies

```bash
cd examples
pip install -r requirements.txt
```

This installs:
- `pyntcore` - NetworkTables NT4 protocol
- `robotpy-wpimath` - Geometry classes (Pose3d, etc.)
- `robotpy-wpiutil` - WPILib utilities
- `protobuf` - Protocol Buffer support

## Usage

### Run the Viewer

```bash
python questnav_viewer.py
```

Or use the helper scripts:
- **Windows**: `run_viewer.bat`
- **Linux/Mac**: `./run_viewer.sh`

### Connect Quest Headset

1. Start the QuestNav Unity app on your Quest
2. Configure the Quest app with your computer's IP address
3. Quest will connect automatically
4. Viewer will show "Connected" and display pose data

## Using in Robot Code

### Example Robot Integration

```python
from questnav import QuestNav, PoseFrame
from wpimath.geometry import Pose2d, Rotation2d

class MyRobot:
    def robotInit(self):
        # Create QuestNav instance
        self.questnav = QuestNav()
    
    def autonomousInit(self):
        # Reset Quest to known starting position
        start_pose = Pose2d(1.5, 5.5, Rotation2d.fromDegrees(0))
        self.questnav.set_pose(Pose3d(start_pose))
    
    def robotPeriodic(self):
        # Process command responses
        self.questnav.command_periodic()
        
        # Get new pose frames
        frames = self.questnav.get_all_unread_pose_frames()
        
        for frame in frames:
            if self.questnav.is_connected() and self.questnav.is_tracking():
                # Add to pose estimator
                self.pose_estimator.addVisionMeasurement(
                    frame.quest_pose_3d.toPose2d(),
                    frame.data_timestamp,
                    (0.1, 0.1, 0.05)  # Standard deviations
                )
```

## GUI Features

### Status Bar
- **Connection**: Shows if Quest is connected and sending data
- **Tracking**: VIO tracking system status
- **Battery**: Current battery percentage
- **Latency**: Network latency in milliseconds

### Pose Display
- **Position**: X, Y, Z coordinates in meters (WPILib field frame)
- **Rotation**: Roll, Pitch, Yaw in degrees

### Diagnostics
- **Frame Count**: Total frames from Quest
- **Frame Rate**: Current update rate (~100 Hz expected)
- **Total Received**: Frames received by viewer
- **Frame Drops**: Detected dropped frames
- **Tracking Lost Events**: Count of tracking failures
- **Uptime**: Viewer runtime

### Commands
- **Reset Pose to Origin**: Send (0, 0, 0) reset command
- **Reset Pose to Custom**: Dialog to specify X, Y, Z, Yaw
- **Clear Log**: Clear event log
- **Pause/Resume Log**: Stop log updates
- **Export Log**: Save log to timestamped .txt file

## Coordinate System

QuestNav uses the WPILib field coordinate system:
- **X**: Forward (towards opposing alliance)
- **Y**: Left (when facing forward)  
- **Z**: Up
- **Rotation**: Counter-clockwise positive (right-handed)
- **Units**: Meters for position, radians internally (degrees in GUI)

## NetworkTables Topics

### Quest Publishes (Quest → Robot)
- `/QuestNav/frameData` - Pose data @ 100 Hz (protobuf)
- `/QuestNav/deviceData` - Device status @ 3 Hz (protobuf)
- `/QuestNav/response` - Command responses (protobuf)

### Robot Publishes (Robot → Quest)
- `/QuestNav/request` - Commands like pose reset (protobuf)

## Architecture

```
┌─────────────┐
│ Quest       │ (NT4 Client)
│ Headset     │ Publishes: frameData, deviceData, response
└──────┬──────┘ Subscribes: request
       │
       │ NT4 Protocol
       │ (Ethernet/WiFi)
       │
┌──────┴──────┐
│ Python      │ (NT4 Server - acts like RoboRIO)
│ Viewer      │ 
├─────────────┤
│ questnav.py │ Library - subscribes to Quest data
└─────────────┘ Viewer - displays data in GUI
```

## File Structure

```
examples/
├── questnav.py          # QuestNav library (import this in robot code)
├── questnav_viewer.py   # GUI demonstration application
├── requirements.txt     # Python package dependencies
├── README.md            # This file
├── run_viewer.bat       # Windows launcher
├── run_viewer.sh        # Linux/Mac launcher
└── *_pb2.py             # Generated protobuf classes (4 files)
```

## Protobuf Generation

The `*_pb2.py` files are auto-generated from the `.proto` files. To regenerate:

```bash
pip install protobuf grpcio-tools
cd ../protos
python -m grpc_tools.protoc -I. --python_out=../examples \
    geometry3d.proto geometry2d.proto data.proto commands.proto
```

Then fix relative imports in generated files to absolute imports.

## Performance

- **Frame Rate**: ~100 Hz (10ms updates from Quest)
- **Latency**: 5-50ms depending on connection (USB vs WiFi)
- **CPU Usage**: <5% on modern hardware
- **Memory**: <50MB typical
- **Network**: ~10-20 KB/s sustained

## Troubleshooting

### Quest Shows Connected But No Data

- Check that Quest app is actually running and tracking
- Move Quest headset to ensure tracking is active
- Restart Quest app if needed

### Command "Too Old" Errors

- Normal for first command after connection
- Quest has 50ms command TTL for safety
- Commands sent during good connection will work

### High Latency

- **USB**: Use high-quality USB-C cable, try different port
- **WiFi**: Use 5GHz band, reduce distance to router
- Check firewall isn't delaying packets

## RobotPy Resources

- **GitHub**: https://github.com/robotpy/mostrobotpy
- **Documentation**: https://robotpy.readthedocs.io/
- **API Reference**: https://robotpy.readthedocs.io/projects/robotpy/en/latest/

## QuestNav Resources

- **Main Site**: https://questnav.gg/
- **GitHub**: https://github.com/QuestNav/QuestNav
- **Discord**: https://discord.gg/hD3FtR7YAZ

## License

MIT License - Same as QuestNav project

RobotPy is licensed under BSD 3-Clause License
