---
title: About QuestNav
---

# About QuestNav

QuestNav is a VIO-based robot localization system for FRC that leverages Meta Quest VR headsets to stream real-time pose information to your robot via Network Tables 4 (NT4). By mounting a Quest headset on your robot, you gain access to Visual-Inertial Odometry (VIO) technology used in VR headsets for mapping and navigation in competition fields, practice spaces, or any environment.

::::info
QuestNav provides consistent VIO-based pose tracking at 100Hz and does not require AprilTags. It streams pose data over NT4 for robot localization.
::::

## Key Benefits

### Tracking Performance
- **Consistent VIO-based pose tracking**
- **No AprilTags required** - works anywhere without field setup or tag placement
- **100Hz pose updates** for real-time, responsive robot control
- **Multiple SLAM cameras** for redundant, calibrated VIO (Visual Inertial Odometry)
- **Sensor fusion** - camera tracking combined with onboard IMU for accurate position and velocity estimates
- **VR-grade tracking** - the same technology that powers head tracking in VR

### Cost and Availability
- Uses off-the-shelf Meta Quest hardware; total cost depends on headset model and accessories
- Widely available consumer device; easy to source and replace through retail channels
- Power pass-through USB‑C Ethernet adapter recommended to reduce battery drain during matches
- **Quest 3S recommended** for FRC applications due to price and tracking characteristics
- **All-in-one device** - no need for separate cameras, compute units, or complex setups

### Hardware Platform
- Works with Meta Quest 3 and Quest 3S headsets
- Powered by [Qualcomm XR2G2 platform](https://www.qualcomm.com/products/mobile/snapdragon/xr-vr-ar/snapdragon-xr2-gen-2-platform)
- Multiple CPU/GPU cores for parallel processing
- 8GB RAM and 128GB+ storage
- Dedicated video hardware for efficient image processing
- **Self-contained, rechargeable battery** - no external power required

## Supported Devices
- Meta Quest 3
- Meta Quest 3S
- Other headsets are untested

### Integration
- **Vendor dependency** installation for Java-based FRC robot code
- **Standard WPILib coordinate system** for integration with existing code
- **Works with any NT4-compatible language** (Java, Python, C++, etc.)
- **Compatible with SwerveDrivePoseEstimator** and popular swerve libraries (YAGSL, AdvantageKit, CTRE)
- Official language support: Java vendor dependency; see [Robot Code Setup](./robot-code) for details

::::tip
Quest 3 and Quest 3S are supported. Quest 3S is recommended for FRC due to price and tracking characteristics. The depth projector on the Quest 3 doesn't provide significant benefits for robot navigation.
::::

## How It Works

QuestNav uses the Quest headset's Visual-Inertial Odometry (VIO) system — the same technology used for VR head tracking — to track an FRC robot's position in 3D space.

### Data Flow
1. **Visual Capture** - Quest's multiple SLAM cameras capture visual data from the environment
2. **Sensor Fusion** - Visual data is combined with inertial data from the built-in IMU
3. **Pose Calculation** - Onboard processing determines position and orientation in real-time (100Hz)
4. **Network Transmission** - Pose data is transmitted to the robot via wired Ethernet connection
5. **Robot Integration** - Data is published to NetworkTables and consumed by robot code for localization

### Communication Architecture

QuestNav implements bidirectional communication using Protocol Buffers over NT4 (NetworkTables 4):

**Quest → Robot (Publishers):**
- `/QuestNav/frameData` - High-frequency pose updates (100Hz) with position, rotation, and timestamps
- `/QuestNav/deviceData` - Device status updates (3Hz) including battery level and tracking state
- `/QuestNav/response` - Command execution results and error messages

**Robot → Quest (Subscribers):**
- `/QuestNav/request` - Commands from robot including pose reset requests and configuration updates

**Key Features:**
- **Protocol Buffers serialization** for type-safe, versioned message encoding
- **Asynchronous command system** with response tracking via unique command IDs
- **Update rates**: 100Hz pose data, 3Hz device data; commands are asynchronous

::::note
Only pose/device status and command messages are sent over NT4. Camera frames are not transmitted.
::::

## Typical Use Case and Field Localization

QuestNav provides relative localization using VIO. It is not yet automatically localize to an FRC field by detecting field features. Teams should:

- Set an initial field-relative pose from robot code (e.g., at the start of a match)
- Periodically reset pose when needed to correct drift
- Use `SwerveDrivePoseEstimator` (or similar) to fuse QuestNav with other sensors

Note: QuestNav does not require AprilTags. If you want automatic field alignment, you can combine QuestNav with external systems that provide absolute field references.

### Operating Conditions
- Works best in evenly lit, feature-rich environments
- Secure, vibration-damped mounting improves tracking stability
- Expect occasional pose resets over long durations or after rough impacts

## Demo Video

Check out this demo video to see QuestNav in action:

<iframe width="560" height="315" src="https://www.youtube.com/embed/Mo0p1GGeasM?si=pigvJwCiWEIoZxlO" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

For a more comprehensive demonstration, view the [full video on YouTube](https://youtu.be/Mo0p1GGeasM).

## Thanks

QuestNav exists because of many sidebar discussions, technical deep-dives, and what-if conversations with coworkers and members of the FIRST community. Special thanks to the following contributors who made this project possible:

- [@juchong](https://github.com/juchong)
- [@ThadHouse](https://github.com/ThadHouse)
- [@SeanErn](https://github.com/SeanErn)
- [@jasondaming](https://github.com/jasondaming)
- [@allengregoryiv](https://github.com/allengregoryiv)
- [@agasser](https://github.com/agasser)
- [@itsWihy](https://github.com/itsWihy)
- [@schreiaj](http://github.com/schreiaj)
- [@steinra2002](https://github.com/steinra2002)
- [@TheGamer1002](https://github.com/TheGamer1002)
- [@spacey-sooty](https://github.com/spacey-sooty)

## Support

::::info
For questions, troubleshooting help, or to share your experiences with QuestNav, join the community discussion on our [Discord](https://discord.gg/hD3FtR7YAZ).
::::

## Quickstart
- Start with [Device Setup](./device-setup)
- Install the app via [App Setup](./app-setup)
- Choose an [Ethernet Adapter](./adapters)
- Review [Mounting](./mounting) and [Wiring](./wiring)
- Integrate robot code via [Robot Code Setup](./robot-code)

## Next Steps
Ready to get started? Continue to the [Choosing an Ethernet Adapter](./adapters) section to select the appropriate hardware for your setup.