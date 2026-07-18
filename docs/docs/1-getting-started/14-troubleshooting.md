---
title: Troubleshooting
---
# Troubleshooting

This guide covers common issues you might encounter when setting up and using QuestNav, along with their solutions. If you experience problems not covered here, please reach out on the [QuestNav Discord server](https://discord.gg/CsRjKfn8Xa) or [Chief Delphi thread](https://www.chiefdelphi.com/t/questnav-the-best-robot-pose-tracking-system-in-frc/476083).

## Connection Issues

### Quest Not Connecting to Robot Network

**Symptoms:**
- "No connection" error in QuestNav app
- Network Tables not receiving pose data

**Solutions:**
1. **Check Ethernet Adapter Compatibility**
    - Verify your adapter is on the [supported list](./adapters)
    - Look for LED activity on the adapter

2. **Verify Physical Connections**
    - Ensure Ethernet cable is securely connected at both ends
    - Try a different Ethernet cable
    - Test adapter with a computer to confirm functionality

3. **Check Network Configuration**
    - Confirm robot radio is operational
    - Try resetting the robot's network switch

:::tip
Most connection issues are related to the adapter or cable. Always try a known working adapter and cable first when troubleshooting connection problems.
:::

### Quest Not Connecting During Simulation

**Symptoms:**
- Quest web UI (`http://<Quest IP>:5801/api/status`) shows `networkConnected: false`
- `QuestNav/Connected` stays `false` in the simulation GUI
- The Quest headset is on the same network and has the correct Debug IP Override set

**Cause:**
Windows Firewall (or your OS firewall) is blocking incoming connections to the Java process running the simulation,
preventing the Quest from reaching the NetworkTables server.

**Solutions:**
1. **Allow Java through Windows Firewall**
    - Open **Windows Security > Firewall & network protection > Allow an app through firewall**
    - Click **Change settings**, then **Allow another app...**
    - Browse to the WPILib JDK's `java.exe` (typically `C:\Users\Public\wpilib\<year>\jdk\bin\java.exe`)
    - Ensure both **Private** and **Public** checkboxes are selected

2. **Verify the correct JDK**: your simulation may use a different JDK than you expect. Check Task Manager
   while the simulation is running to see which `java.exe` is active.

3. **Check for port conflicts**: if a previous simulation didn't exit cleanly, ports 1735 and 5810 may still be
   in use. End any stale `java.exe` processes in Task Manager and restart.

:::info
See the [Testing & Simulation](./simulation) page for more details on running QuestNav with WPILib desktop simulation.
:::

### Intermittent Connection Drops

**Symptoms:**
- Connection status fluctuates during operation
- Pose data freezes or jumps unexpectedly

**Solutions:**
1. **Cable Quality**
    - Replace with a higher quality, shielded Ethernet cable
    - Secure loose connections with electrical tape

2. **Power Issues**
    - Check if adapter is receiving consistent power
    - If using passthrough power, verify voltage stability
    - Test with Quest's internal battery only


## Tracking Problems

### Pose Drift

**Symptoms:**
- Position slowly drifts over time
- Heading becomes increasingly inaccurate

**Solutions:**
1. **Reset Position**
    - Use the reset position function at a known location
    - Implement periodic resets if field landmarks are available

2. **Environment Factors**
    - Ensure adequate lighting in the environment
    - Add visual features if operating in sparse environments
    - Avoid highly reflective or uniform surfaces

3. **Update QuestNav**
    - Check for and install the latest app version
    - Clear the app cache in Android settings

:::info
Some drift is normal over time as small tracking errors accumulate. Consider implementing automatic resets at known positions (such as game pieces or field elements) during a match. An upcoming release of QuestNav will include AprilTag detection.
:::

### Sudden Position Jumps

**Symptoms:**
- Position changes abruptly during operation
- Robot responds erratically to pose data

**Solutions:**
1. **Motion Constraints**
    - Implement filtering in robot code
    - Add maximum change constraints for pose updates

2. **Mechanical Issues**
    - Check if headset is securely mounted
    - Look for vibration that might affect tracking
    - Add dampening material to mount if needed

3. **Camera Obstructions**
    - Ensure Quest cameras are clean and unobstructed
    - Check for damage to Quest cameras
    - Verify nothing is blocking Quest's view

:::danger
If your pose data shows frequent large jumps, check the physical mount immediately. A loose headset will cause erratic tracking that can't be fixed with software filtering.
:::

## Performance Issues

### High Latency

**Symptoms:**
- Noticeable delay in pose updates

**Solutions:**
1. **Network Optimization**
    - Reduce other network traffic on robot
    - Check for bandwidth-heavy applications

2. **App Settings**
    - Close any background apps on Quest
    - Restart the Quest headset

:::tip
If experiencing latency, check your CPU usage in robot code first. Complex filtering or processing can add significant delays to pose updates.
:::

### Battery Drain

**Symptoms:**
- Quest battery depletes rapidly
- Shutdown during operation

**Solutions:**
1. **Power Management**
    - Use passthrough power if available
    - Lower screen brightness

2. **Heat Issues**
    - Ensure adequate ventilation around Quest
    - Check for excessive heat from nearby components
    - Allow cooling time between matches

:::warning
The Quest will automatically shut down when battery is critically low. Always ensure adequate power supply during competitions, preferably using a passthrough adapter.
:::

## Software Issues

### Version Mismatch Warnings

**Symptoms:**
- Console is flooded with warnings every 5 seconds:
  `WARNING FROM QUESTNAV: QuestNavLib version (X.X.X) on your robot does not match QuestNav app version (Y.Y.Y) on your headset.`

**Solutions:**
1. **Update to matching versions**: ensure the `questnavlib.json` vendordep version matches the QuestNav APK
   installed on the headset. Both should be the same release.

2. **Suppress during development**: if you're intentionally running mismatched versions (e.g. testing a new library
   against an older headset app), you can suppress the warning:
    ```java
    questNav.setVersionCheckEnabled(false);
    ```

:::warning
Re-enable version checking before competition. Mismatched versions can cause subtle compatibility issues.
:::

### App Crashes

**Symptoms:**
- QuestNav application closes unexpectedly
- Black screen or freezing

**Solutions:**
1. **App Maintenance**
    - Reinstall the latest version
    - Reboot the Quest headset

2. **System Updates**
    - Check for Quest system updates
    - Update after competition if possible
    - Test thoroughly after any updates

3. **Contact Us**
   - If you are using an official build, let us know on Discord or Chief Delphi

:::note
If you compiled QuestNav from source (not the official release APK), the Unity project must have the "Development Build" flag enabled or the app will crash immediately at launch. Teams using the official APK from the GitHub releases page do not need to worry about this.
:::

### Installing ADB on RoboRIO (Optional)

Installing ADB directly on your RoboRIO can simplify management and troubleshooting:

1. Download the [ADB for RoboRIO fork](https://github.com/juchong/ADB-For-RoboRIO)
2. Follow the installation instructions in the repository
3. Connect to the RoboRIO over your robot's network to issue ADB commands

:::tip
Installing ADB on your RoboRIO allows you to send commands to the Quest headset directly from your robot's network, eliminating the need for a USB connection to a laptop.
:::

## Diagnostic Tools

QuestNav exposes a web interface at `http://<Quest IP Address>:5801` that provides several tools for diagnosing issues without needing to touch the headset or write any code.

### Web Interface Status

Open `http://<Quest IP Address>:5801/api/status` in a browser to see a real-time JSON snapshot of the headset's state, including:

- `isTracking`: whether the headset is actively tracking its position
- `networkConnected`: whether the headset has a NetworkTables connection to the robot
- `batteryPercent` / `batteryCharging`: battery level and charging status
- `position` / `rotation` / `eulerAngles`: current headset pose
- `fps`: current tracking framerate
- `trackingLostEvents`: number of times tracking was lost since the app started
- `ipAddress` / `robotIpAddress` / `teamNumber`: network configuration

This is the fastest way to verify that the headset is connected, tracking, and sending data.

### Viewing Logs

The Quest stores application logs that can be retrieved via:

```
http://<Quest IP Address>:5801/api/logs
```

This returns recent log entries including warnings and errors. To get the last 200 entries:

```
http://<Quest IP Address>:5801/api/logs?count=200
```

Logs can be cleared with a DELETE request:

```
curl -X DELETE http://<Quest IP Address>:5801/api/logs
```

### Restarting QuestNav Remotely

If the app is in a bad state, you can restart it without physically touching the headset:

```
curl -X POST http://<Quest IP Address>:5801/api/restart
```

### Resetting Pose via Web Interface

You can trigger a pose reset from the web interface:

```
curl -X POST http://<Quest IP Address>:5801/api/reset-pose
```

### Reading and Updating Configuration

To read the current configuration:

```
http://<Quest IP Address>:5801/api/config
```

This shows the team number, debug IP override, auto-start setting, passthrough stream setting, and debug logging state.

To update a setting (e.g. team number):

```
curl -X POST http://<Quest IP Address>:5801/api/config \
     -H "Content-Type: application/json" \
     -d '{"path":"WebServerConstants/webConfigTeamNumber","value":1234}'
```

See the [Web API Reference](/docs/api-reference/web-api) for the complete endpoint documentation.

### NetworkTables Analysis

For deeper diagnostics from the robot side:

1. Use [AdvantageScope](https://docs.wpilib.org/en/stable/docs/software/dashboards/advantagescope.html) to record and replay NetworkTables data
2. Check for missing updates or invalid values in the `QuestNav` table
3. Compare timestamps to identify delays between the headset and robot code

## Common Questions

### "Why is my robot's position still wrong after resetting?"

Make sure you're resetting the pose to the correct field coordinates. The reset position should match your **headset's** actual position on the field, including orientation. **NOT THE ROBOT POSE.** If you know the robot's pose, you must apply the `ROBOT_TO_QUEST` transform before calling `setPose()`:

```java
Pose3d questPose = robotPose.transformBy(ROBOT_TO_QUEST);
questNav.setPose(questPose);
```

### "Why does my Quest display 'USB Device Not Supported'?"

This usually indicates an incompatible Ethernet adapter. Refer to the [Adapters](./adapters) page for compatible options.

### "How do I know if QuestNav is working correctly?"

When functioning properly, QuestNav will:
- Show a good connection status in the app
- Display a consistent IP address
- Publish pose data to Network Tables that updates smoothly
- Show "tracking: true" in the Network Tables viewer

## Video Guide
:::tip Video Guide
A video walkthrough for troubleshooting is coming soon.
:::

## Getting More Help

If you've tried the solutions above and are still experiencing issues:

1. Take a video of the problem if possible
2. Gather log files from both QuestNav and robot
3. Post details on the [QuestNav Discord](https://discord.gg/CsRjKfn8Xa) or [Chief Delphi thread](https://www.chiefdelphi.com/t/questnav-the-best-robot-pose-tracking-system-in-frc/476083)
4. Include your Quest model, robot controller, and QuestNav version

:::tip
When seeking help, provide as much detail as possible about your setup, including adapter model, power supply method, mounting configuration, and the specific issue you're experiencing.
:::