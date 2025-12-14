---
title: Device Setup 
---
# Device Setup

Setting up your Quest headset correctly is crucial for optimal QuestNav performance. This guide will walk you through the initial configuration to prepare your Quest for robot navigation.

## Initial Setup

1. **Power on the Quest headset** and put it on.
2. **Complete the initial Oculus setup process** if this is a new headset:
    - Create or log into a Meta account if prompted
    - Set up your guardian boundaries in a clear area
    - Complete any system tutorials that are required

:::tip
If this is your first time using a Quest headset, take a few minutes to familiarize yourself with the basic controls and interface before proceeding with the QuestNav setup.
:::

## Developer Account Setup

Before you can install custom applications on your Quest, you need to set up a developer account. There are two ways to do this:

### Method 1: Manual Meta Account Setup
1. Follow [this guide](https://medium.com/sidequestvr/how-to-turn-on-developer-mode-for-the-quest-3-509244ccd386) to set up "Developer Mode"
2. Sign into your Meta developer account and download the [Meta Quest Developer Hub (MQDH)](https://developers.meta.com/horizon/develop)

### Method 2: SideQuest-Led Setup
1. Download the SideQuest advanced installer from [sidequestvr.com](https://sidequestvr.com/setup-howto)
2. Connect your Quest to your PC using a USB cable
3. Follow the on-screen prompts to enable developer mode

:::info
Developer mode is required to install and use custom applications like QuestNav. Without it, you won't be able to complete the installation process.
:::

:::info
The following steps can be automated by visiting the [QuestNav Setup Page](https://setup.questnav.gg/) and following the steps provided.
:::

## Optimizing for QuestNav

Once developer mode is enabled, you'll need to adjust several system settings:

### Disable Wi-Fi
QuestNav uses a direct Ethernet connection to your robot, so Wi-Fi should be disabled:
1. Navigate to **Settings** → **Wi-Fi**
2. Toggle the switch to **Off**

Alternatively, you can use ADB to disable Wi-Fi with this command:
```
adb shell svc wifi disable
```

:::warning
If Wi-Fi remains enabled, the headset will constantly disconnect from the robot network as it tries to look for internet connectivity, causing reliability issues.
:::

### Disable Bluetooth
Bluetooth connections can cause interference:
1. Go to **Settings** → **Bluetooth**
2. Toggle the switch to **Off**

Alternatively, you can use ADB with this command:
```
adb shell svc bluetooth disable
```

:::tip
Disabling Bluetooth will break the companion app functionality, but this is necessary for competition reliability.
:::

### Disable Guardian System
The Guardian system is designed for VR safety but can interfere with QuestNav:
1. Navigate to **Settings** → **Advanced** → **Experimental Settings** → **Enable Custom Settings**
2. Turn OFF **Physical Space Features**, **MTP Notification**, and **Link Auto Connect**
    - These settings might also be located in **Settings** → **Developer** → **Experimental Settings** on older OS builds

### Maximize Screen Timeout
To prevent the headset from sleeping during operation:
1. Go to **Settings** → **General** → **Power** → **Display off**
2. Set to the maximum value (usually 4 hours)

### Power Settings
For optimal operation on your robot:
1. **Disable Travel Mode** in power settings
2. **Disable Battery Saver Mode** in power settings
3. Turn brightness as low as possible

:::tip
These power settings are the opposite of what was previously recommended. Testing has shown that disabling these features provides better performance for robot navigation.
:::

## Verification
To verify your settings have been properly applied:
1. Wi-Fi icon should show as disconnected
2. Bluetooth icon should not appear
3. No guardian boundaries should appear when moving the headset
4. Screen timeout should be set to the maximum value

:::tip
Create a pre-competition checklist with these verification steps to ensure your Quest is properly configured before each match.
:::

## Troubleshooting
- If the quest goes into sleep mode, check for system updates that may have changed settings
- If developer mode isn't working, verify your Meta account has developer status enabled
- If ADB connection fails, try restarting the headset or switching USB ports on your laptop

:::warning
Meta occasionally releases Quest updates that may reset some of these settings. Always verify your configuration before competitions.
:::

## Video Guide
[Placeholder for Device Setup Video Guide]

## Next Steps
Now that your Quest is configured, proceed to the [App Setup](./app-setup) section to install the QuestNav application.