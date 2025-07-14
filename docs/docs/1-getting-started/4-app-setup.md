---
title: App Setup 
---
# App Setup

After configuring your Quest headset, the next step is installing and setting up the QuestNav application. This guide walks you through the installation process and initial configuration.

## Installing QuestNav

QuestNav is distributed as an APK file that you'll need to install manually onto your Quest headset. There are several methods to do this:

:::info
You'll need to have Developer Mode enabled on your Quest headset before proceeding with these installation methods. If you haven't done that yet, please refer to the [Device Setup](/docs/getting-started/device-setup) section.
:::

### Method 1: Using ADB (Android Debug Bridge)
ADB provides a command-line method to install the app:

1. Connect your Quest to your computer with a USB cable
2. Enable USB debugging on your Quest when prompted
:::tip
Select "always allow" to allow easy use of ADB with your laptop, even if the Quest is mounted to the robot
:::
3. Download the latest APK file from the [QuestNav GitHub Releases page](https://github.com/QuestNav/QuestNav/releases)
4. Open a command prompt or terminal and navigate to where the APK is saved
5. Run the following command:
   ```
   adb install QuestNav_vX.X.X.apk
   ```
   (Replace X.X.X with the version number of the file you downloaded)

### Method 2: Using Meta Quest Developer Hub (MQDH)
Meta Developer Hub provides a graphical interface for app installation:

1. Download and install [Meta Developer Hub](https://developer.oculus.com/documentation/unity/ts-odh/) on your computer
2. Connect your Quest to your computer via USB
3. Open Meta Developer Hub and select your device
4. Navigate to the "Applications" tab
5. Click "Install" and select the QuestNav APK file
    - Alternatively, drag and drop the APK into the right side of the MQDH window

:::tip
MQDH provides the most user-friendly installation experience and additional debugging tools that can be helpful if you encounter issues.
:::

### Method 3: Using SideQuest
For users familiar with SideQuest:

1. Connect your Quest to your computer with SideQuest running
2. Click the icon of a box with an arrow at the top of the window
3. Select the QuestNav APK file and click "Open"
4. Follow the on-screen prompts to complete installation

## Launching QuestNav

After installation, you can launch QuestNav in several ways:

1. **From Unknown Sources**:
    - In your Quest menu, navigate to "Apps"
    - Select the dropdown menu and choose "Unknown Sources"
    - Find and select "QuestNav" from the list

2. **Using ADB**:
   ```
   adb shell am start -n gg.questnav.questnav/.MainActivity
   ```

:::note
The first time you launch QuestNav, you may need to grant additional permissions for the app to access system features required for tracking and networking.
:::

## Initial Configuration

The first time you launch QuestNav, you'll need to configure a few settings:

### Setting Team Number

:::warning
The example app has a team number set to 9999. You'll need to change this to your team's number for proper communication with your robot.
:::

1. Enter your FRC team number in the provided field and click set
2. This ensures correct network communication with your robot

## Automatic Startup

After installing the app, launch it and accept any permission requests. From then on, QuestNav will automatically launch when the headset is
powered up or rebooted.

## Troubleshooting Installation

If you encounter issues during installation or launch:

### APK Won't Install
- Verify developer mode is enabled
- Check USB connection
- Try a different installation method

:::note
Some USB cables only support charging and not data transfer. If you're having connection issues, try a different USB cable marked as "data cable" or "charging and data cable."
:::

### App Crashes on Launch
- Verify the APK was built with the "Development Build" flag if building with Unity
- Check for adequate storage space on the Quest
- Try reinstalling the app

:::warning
If the APK was not built with the "Development Build" flag, it will crash at launch! This is a critical setting when building the app from source.
:::

### Team Number Issues
- If you're using the example app, remember it's set to team 9999 by default
- Custom builds allow changing the default team number, but this is unnecessary for most teams

## Video Guide
[Placeholder for App Setup Video Guide]

## Next Steps
With QuestNav installed and configured, proceed to the [Mounting](./mounting) section to learn how to properly mount the Quest on your robot.