---
title: Manual Headset Setup
---
# Manual Headset Setup

:::warning
This guide is for development and edge-case scenarios where the [QuestNav Setup Page](https://setup.questnav.gg/) cannot be used (for example, no internet access on the setup machine, scripted/automated deployment, or debugging the underlying configuration). For normal team setup, **use the QuestNav Setup Page** — it is faster, less error-prone, and stays current with the supported configuration. The procedure below is preserved for reference and is not actively supported.
:::

This page documents how to manually configure a Quest headset for QuestNav and install the app without the QuestNav Setup Page. You should already have Developer Mode enabled — see [Headset Setup → Enable Developer Mode](../getting-started/device-setup#enable-developer-mode).

The procedure has two phases: configure the headset's system settings, then install the QuestNav APK.

## Phase 1: Headset Configuration

After enabling Developer Mode, apply these system settings on the Quest itself.

### Disable Wi-Fi

QuestNav uses a direct Ethernet connection to the robot, so Wi-Fi should be disabled:

1. Navigate to **Settings → Wi-Fi**
2. Toggle the switch to **Off**

Or with ADB:

```
adb shell svc wifi disable
```

:::warning
If Wi-Fi remains enabled, the headset will constantly disconnect from the robot network as it tries to look for internet connectivity, causing reliability issues.
:::

### Disable Bluetooth

1. Navigate to **Settings → Bluetooth**
2. Toggle the switch to **Off**

Or with ADB:

```
adb shell svc bluetooth disable
```

:::info
Disabling Bluetooth will break the companion app functionality, but this is necessary for competition reliability.
:::

### Disable Guardian System

The Guardian system is designed for VR safety but interferes with QuestNav:

1. Navigate to **Settings → Advanced → Experimental Settings → Enable Custom Settings**
2. Turn **OFF** **Physical Space Features**, **MTP Notification**, and **Link Auto Connect**
   - On older OS builds, these may be located under **Settings → Developer → Experimental Settings**

### Maximize Screen Timeout

To prevent the headset from sleeping during operation:

1. Navigate to **Settings → General → Power → Display off**
2. Set to the maximum value (usually 4 hours)

### Power Settings

For optimal operation on a robot:

1. **Disable Travel Mode**
2. **Disable Battery Saver Mode**
3. Lower brightness as much as possible

:::tip
These power settings are the opposite of what was previously recommended. Testing has shown that disabling these features provides better performance for robot navigation.
:::

### Verification

To confirm your settings have been applied:

1. The Wi-Fi icon should show as disconnected
2. The Bluetooth icon should not appear
3. No guardian boundaries should appear when moving the headset
4. The screen timeout should be set to the maximum value

:::warning
Meta occasionally releases Quest updates that may reset some of these settings. Always verify the configuration before competitions.
:::

## Phase 2: Installing QuestNav {#installing-questnav}

QuestNav is distributed as an APK file. Use one of the methods below to install it without the QuestNav Setup Page.

### Method 1: ADB (Android Debug Bridge)

1. Connect the Quest to your computer with a USB cable
2. Authorize USB debugging when prompted on the Quest (select "Always allow from this computer")
3. Download the latest APK from the [QuestNav GitHub Releases page](https://github.com/QuestNav/QuestNav/releases)
4. Open a terminal where the APK is saved and run:

   ```
   adb install QuestNav_vX.X.X.apk
   ```

   Replace `X.X.X` with the version number of the file you downloaded.

### Method 2: Meta Quest Developer Hub (MQDH)

1. Download and install [Meta Quest Developer Hub](https://developers.meta.com/horizon/documentation/unity/ts-odh/)
2. Connect the Quest via USB
3. Open MQDH and select your device
4. Navigate to the **Applications** tab
5. Click **Install** and select the QuestNav APK (or drag and drop the APK into the right side of the MQDH window)

:::tip
MQDH provides the most user-friendly manual installation experience and additional debugging tools that can be helpful if you encounter issues.
:::

### Method 3: SideQuest

1. Connect the Quest with SideQuest running
2. Click the **box-with-arrow** icon at the top of the window
3. Select the QuestNav APK and click **Open**
4. Follow the on-screen prompts

## Phase 3: Launching QuestNav

After installation, launch QuestNav one of two ways:

1. **From Unknown Sources**:
   - In your Quest menu, navigate to **Apps**
   - Open the dropdown menu and choose **Unknown Sources**
   - Select **QuestNav**

2. **Via ADB**:

   ```
   adb shell am start -n gg.questnav.questnav/.MainActivity
   ```

:::note
The first time you launch QuestNav, you may need to grant additional permissions for the app to access tracking and networking features.
:::

## Phase 4: Set the Team Number

:::warning
The example app ships with team number **9999**. You must change this to your team's number for QuestNav to find your robot.
:::

1. Enter your FRC team number on the main window and click **Set**

:::info
The team number determines the IP address QuestNav connects to. QuestNav uses the FRC convention `10.TE.AM.2` (e.g. team 1234 → `10.12.34.2`). If the team number is wrong, the Quest will attempt to reach the wrong IP address and will never connect to your robot.
:::

## Phase 5: Auto-Start (Optional)

QuestNav can automatically start when the headset is powered on or rebooted. Toggle **Auto Start On Boot** on the main window.

:::tip
Auto-start saves time during competition setup and ensures QuestNav is running after unexpected restarts.
:::

## Troubleshooting

- **Quest goes into sleep mode unexpectedly**: Check for system updates that may have reset display-off settings.
- **Developer Mode not appearing in the Meta Horizon app**: Verify your Meta account has been verified at [developers.meta.com/manage/verify](https://developers.meta.com/manage/verify/).
- **ADB connection fails**: Try a different USB-C cable (charging-only cables won't work), restart the headset, or try a different USB port on your laptop.
- **APK won't install**: Verify Developer Mode is enabled, the USB connection is good, and the cable supports data transfer.
- **App crashes on launch**: Check for adequate storage on the Quest, reinstall the app, or reboot the headset.

:::note
If you compiled QuestNav from source (not the official release APK), the Unity project must have the **Development Build** flag enabled or the app will crash immediately at launch. Teams using the official APK from the GitHub Releases page do not need to worry about this.
:::
