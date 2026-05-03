---
title: Troubleshooting
---
# Troubleshooting

This guide covers common issues you might encounter when setting up and using QuestNav, along with their solutions. If you experience problems not covered here, please reach out on the [QuestNav Discord server](https://discord.gg/CsRjKfn8Xa).

## Connection Issues

### Quest Not Connecting to Robot Network

**Symptoms:**
- "No connection" error in QuestNav app
- Network Tables not receiving pose data

**Solutions:**
1. **Confirm Wi-Fi is disabled on the headset**
    - With Wi-Fi enabled, the Quest may route NetworkTables traffic over the Wi-Fi interface instead of the Ethernet adapter. The behaviour is inconsistent and can change between sessions, so the only reliable fix is to disable Wi-Fi entirely.
    - Check via `Settings → Wi-Fi` on the headset, or run `adb shell svc wifi disable`
    - The [QuestNav Setup Page](https://setup.questnav.gg/) disables Wi-Fi automatically; if you set up the headset by hand, verify this is off

2. **Check Ethernet Adapter Compatibility**
    - Verify your adapter is on the [supported list](./adapters)
    - Look for LED activity on the adapter — no lights usually indicates a power or connection issue

3. **Verify Physical Connections**
    - Ensure the USB-C to Ethernet adapter is being powered
    - Verify the adapter is fully seated in the Quest's USB-C port
    - Ensure the Ethernet cable is securely connected at both ends
    - Try a different Ethernet cable
    - Test the adapter with a computer to confirm functionality

4. **Check Network Configuration**
    - Verify the team number on the headset matches your team's number — the Quest connects to `10.TE.AM.2`
    - Confirm the roboRIO has booted and NetworkTables is running on it
    - Try resetting the robot's network switch

:::tip
Most connection issues are related to the adapter, its power, or the cable. Always try a known working adapter, power source, and cable first when troubleshooting connection problems.
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
    - Look for damaged cables or loose connectors
    - Use zip ties or cable clips (not electrical tape) to keep connectors seated under vibration

2. **Connector Retention**
    - The Quest's USB-C port can wiggle loose under vibration; use a locking USB-C cable (e.g. the one included with the [Zinc-V](https://shop.reduxrobotics.com/products/zinc-v)) or add a strain-relief loop so motion isn't transferred directly to the connector

3. **Cable Routing**
    - Secure cables to the robot frame so they can't move during matches
    - Add service loops at connection points so vibration doesn't tug on connectors

4. **Power Issues**
    - Watch the battery percent on the headset over a few minutes. If it isn't steadily climbing (or staying at 100% while the adapter is plugged in), the Quest isn't getting consistent power.
    - Force a fixed 5V supply by using a USB-A to USB-C cable from the power source to the adapter. This skips PD negotiation entirely and rules out PD-related instability.
    - If using a non-PD source, confirm it can supply 5V at 4A (matching the Quest's input spec on the [Wiring](./wiring#power-requirements) page).


## Tracking Problems

### Pose Drift

**Symptoms:**
- Position slowly drifts over time
- Heading becomes increasingly inaccurate

**Solutions:**
1. **Reset to a Known Field Pose from Robot Code**
    - Call `questNav.setPose(Pose3d)` at a known location (auto start position, AprilTag relocalization, driver-station override, etc.) — see [Robot Code Setup → Setting Robot Pose](./robot-code#setting-robot-pose)
    - Implement periodic resets whenever you have a known field reference

2. **Environment Factors**
    - Ensure adequate lighting in the environment
    - Add visual features if operating in sparse environments
    - Avoid highly reflective or uniform surfaces

3. **Update QuestNav**
    - Install the latest QuestNav app on the headset and the matching `questnavlib` vendordep on the robot

:::info
Some drift is normal over time as small tracking errors accumulate. Consider implementing automatic resets at known positions (such as game pieces or field elements) during a match. An upcoming release of QuestNav will include AprilTag detection that automates this.
:::

### Sudden Position Jumps

**Symptoms:**
- Position changes abruptly during operation
- Robot responds erratically to pose data

**Common causes:**
- **Loose mount**: any physical movement of the headset relative to the robot is interpreted as pose change.
- **Tracking re-initialization / feature loss**: if the cameras lose enough features (low light, glare, uniform surfaces), the Quest can re-acquire tracking at an offset.
- **"Double-tap to passthrough" gesture**: vibration on the headset can be misinterpreted as a double-tap, briefly switching the Quest to passthrough mode and producing a discontinuity in the reported pose. The only reliable workaround today is to mount the headset *rigidly* — flexible mounts or rubber dampening amplify the vibration enough to trigger the gesture. Meta is adding the ability to disable this gesture in a future Quest OS update (currently rolling out via the Public Test Channel), after which this should no longer be an issue.

**Solutions:**
1. **Mechanical**
    - Verify the mount is rigid with no flex, slop, or wobble (shake test: nothing moves)
    - Use a 3D-printed mount designed for the Quest 3 / 3S rather than improvised brackets
    - Do **not** add dampening / rubber isolators — those make the double-tap-to-passthrough issue worse, not better

2. **Camera View**
    - Ensure the Quest cameras are clean and unobstructed (no smudges, fingerprints, or dust)
    - Make sure the headset has a clear view of distinctive environmental features
    - Avoid mounting where motors, intakes, or game pieces frequently block the cameras

3. **Pose Validation in Robot Code**
    - Reject readings that are clearly outside the field — see [Field-Bounds Filtering](./robot-code#field-bounds-filtering)
    - Optionally add maximum-change constraints between consecutive frames to drop obvious jumps

:::danger
If pose data shows frequent large jumps, check the physical mount first. A loose or flexible mount cannot be compensated for in software.
:::

## Performance Issues

### High Latency

**Symptoms:**
- Noticeable delay in pose updates

**Baseline:**
End-to-end latency over wired Ethernet directly to the roboRIO is typically **5–10 ms**. Connecting over Wi-Fi or running through debug tooling can significantly increase this.

**Solutions:**
1. **Verify the connection path**
    - Confirm the Quest is reaching the robot over wired Ethernet, not Wi-Fi
    - Confirm the `Debug IP Override` is empty when the Quest is mounted on a real robot (otherwise the Quest will keep trying to reach a dev machine instead of `10.TE.AM.2`)

2. **Network Optimization**
    - Reduce other traffic on the robot's Ethernet bus
    - Check for bandwidth-heavy applications (e.g. high-resolution camera streams) sharing the same path

3. **Robot Code**
    - Drain all unread frames every loop with `getAllUnreadPoseFrames()` instead of reading one at a time
    - Profile your subsystem's `periodic()` to ensure pose ingestion isn't blocked by other work

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

### Headset Not Charging from Passthrough Adapter

**Symptoms:**
- Quest is connected to a passthrough power source but the battery still drains
- Adapter shows no power-LED activity

**Solutions:**
1. **Verify the power source is delivering power**
    - Confirm the USB battery bank or 5V converter is on and outputting voltage
    - Confirm the cable is fully seated at both ends

2. **Inspect the connectors**
    - Check for bent pins in the USB-C connectors on the cable, the adapter, and the headset
    - Test with a known good cable

3. **Test with a known working power source**
    - Swap in a different battery bank or charger to rule out a faulty supply
    - If charging works on the bench but not on the robot, the issue is in the robot wiring or 5V converter

:::danger
If your Quest is losing charge during operation despite being connected to power, fix the power source immediately. Running out of battery mid-match will shut the headset down and you will lose pose tracking.
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
- QuestNav closes unexpectedly back to the Quest home environment
- The app icon is no longer running

**Solutions:**
1. **Capture the error**
    - Pull recent logs from `http://<Quest IP>:5801/api/logs?count=200` and look for exceptions or stack traces near the crash
    - Include these logs when reporting the issue

2. **Try a clean reinstall**
    - Uninstall QuestNav, reboot the headset, then install the latest APK
    - Confirm you're installing the official release rather than a self-built APK

3. **Check the Quest OS version**
    - A recent Quest OS update may have introduced an incompatibility — note the headset's OS version when reporting

4. **Contact us**
    - On the [QuestNav Discord](https://discord.gg/CsRjKfn8Xa), include the logs from step 1, your Quest model and OS version, and the QuestNav APK version

:::note
If you compiled QuestNav from source (not the official release APK), the Unity project must have the **Development Build** flag enabled or the app will crash immediately at launch. Teams using the official APK from the GitHub Releases page do not need to worry about this.
:::

### Black Screen or App Freezing

**Symptoms:**
- The headset display goes black during operation
- QuestNav appears unresponsive but hasn't fully closed

**Solutions:**
1. **Check whether the app is actually running**
    - Open `http://<Quest IP>:5801/api/status` from another device. If it returns valid JSON, the app is still running and the issue is a display or proximity-sensor state, not a crash.
    - If the endpoint times out, treat the issue as an [App Crash](#app-crashes).

2. **Verify QuestNav KeepAwake is installed and running**
    - The [QuestNav KeepAwake](https://github.com/QuestNav/QuestNavKeepAwake) companion app prevents the Quest from sleeping during operation. Without it, the Quest 3 / 3S sleep aggressively when the IMU is stationary or the proximity sensor doesn't detect a wearer — Android's standard `screen_off_timeout` and `stay_on_while_plugged_in` settings cannot override this on Quest hardware.
    - The [QuestNav Setup Page](https://setup.questnav.gg/) installs and starts KeepAwake automatically. If you set up the headset by hand, follow the install steps in the [KeepAwake repository](https://github.com/QuestNav/QuestNavKeepAwake) and reboot. The service auto-starts on boot.

3. **Wake the display**
    - The Quest's display can blank when the proximity sensor doesn't detect a wearer. Briefly cover and uncover the proximity sensor near the bridge of the headset to wake the display.

4. **Restart the app remotely**
    - `curl -X POST http://<Quest IP>:5801/api/restart` will restart QuestNav without removing the headset from the robot.

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

### Installing ADB on the RoboRIO (Optional)

Installing ADB directly on your roboRIO lets you send commands to the Quest from the robot's network, without needing a USB connection to a laptop:

1. Download the [ADB for RoboRIO fork](https://github.com/juchong/ADB-For-RoboRIO)
2. Follow the installation instructions in the repository
3. Connect to the roboRIO over your robot's network to issue ADB commands

:::tip
Useful for triggering app restarts, pulling logs, or kicking a stuck headset from the pits without taking the robot offline.
:::

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

When functioning properly:

- `http://<Quest IP>:5801/api/status` returns `isTracking: true` and `networkConnected: true`
- The `QuestNav` table appears in NetworkTables with `QuestNav/Connected = true` and `QuestNav/Tracking = true`
- Pose data updates smoothly at the headset's tracking framerate (around 72–120 Hz depending on the Quest model)

:::tip Compare against a known-good baseline
If you suspect your integration code is the problem, deploy the [QuestNav Robot Sim Example](https://github.com/juchong/QuestNav-Robot-Sim-Example) (or run it in the WPILib simulator) against the same headset. If it connects and tracks correctly, the issue is in your robot project rather than the Quest or network.
:::

## Getting More Help

If you've tried the solutions above and are still experiencing issues:

1. Take a video of the problem if possible
2. Gather log files from both QuestNav and robot
3. Post details on the [QuestNav Discord](https://discord.gg/CsRjKfn8Xa)
4. Include your Quest model, robot controller, and QuestNav version

:::tip
When seeking help, provide as much detail as possible about your setup, including adapter model, power supply method, mounting configuration, and the specific issue you're experiencing.
:::