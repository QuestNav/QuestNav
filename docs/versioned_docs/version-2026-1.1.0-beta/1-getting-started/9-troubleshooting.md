---
title: Troubleshooting
---
# Troubleshooting

This guide covers common issues you might encounter when setting up and using QuestNav, along with their solutions. If you experience problems not covered here, please reach out on the [QuestNav Discord server](https://discord.gg/hD3FtR7YAZ) or [Chief Delphi thread](https://www.chiefdelphi.com/t/questnav-the-best-robot-pose-tracking-system-in-frc/476083).

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
Some drift is normal over time as small tracking errors accumulate. Consider implementing automatic resets at known positions (such as game pieces or field elements) during a match. **AprilTag support is coming soon!**
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
If the app was compiled without the "Development Build" flag, it will crash immediately at launch. Make sure you're using the official release or a properly compiled development build.
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

### QuestNav Log Files

Log viewer coming soon!

### Network Tables Analysis

For network tables diagnostic information:

1. Use AdvantageScope to record NT data
2. Check for missing updates or invalid values
3. Compare timestamps to identify delays

### Performance Metrics

Coming soon!

:::info
The diagnostic metrics can help identify whether issues are occurring on the Quest side or the robot side of the communication.
:::

## Common Questions

### "Why is my robot's position still wrong after resetting?"

Make sure you're resetting the pose to the correct field coordinates. The reset position should match your **headset's** actual position on the field, including orientation. **NOT THE ROBOT POSE**

### "Why does my Quest display 'USB Device Not Supported'?"

This usually indicates an incompatible Ethernet adapter. Refer to the [Adapters](./adapters) page for compatible options.

### "How do I know if QuestNav is working correctly?"

When functioning properly, QuestNav will:
- Show a good connection status in the app
- Display a consistent IP address
- Publish pose data to Network Tables that updates smoothly
- Show "tracking: true" in the Network Tables viewer

## Video Guide
[Placeholder for Troubleshooting Video Guide]

## Getting More Help

If you've tried the solutions above and are still experiencing issues:

1. Take a video of the problem if possible
2. Gather log files from both QuestNav and robot
3. Post details on the [QuestNav Discord](https://discord.gg/hD3FtR7YAZ) or [Chief Delphi thread](https://www.chiefdelphi.com/t/questnav-the-best-robot-pose-tracking-system-in-frc/476083)
4. Include your Quest model, robot controller, and QuestNav version

:::tip
When seeking help, provide as much detail as possible about your setup, including adapter model, power supply method, mounting configuration, and the specific issue you're experiencing.
:::