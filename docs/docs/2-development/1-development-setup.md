---
title: Development Environment Setup
---
:::warning
**This section is not necessary for most teams. Only use this guide if you plan to make custom changes to the QuestNav backend!**
:::

# Development Environment Setup

This guide will help you set up your development environment for working with the QuestNav headset application.

## Prerequisites

:::info
Before you begin, make sure you have the following:
- A Meta Quest 3 or 3s headset
- A Windows PC with a GPU capable of running Unity
- Basic knowledge of Unity and C# programming
- Git installed on your computer
  :::

## Installation Process

### Step 1: Install Unity

:::warning
You must use Unity 6 (6000.0.29f1) LTS for compatibility with the project.
:::

- Download and install Unity Hub from the [official website](https://unity.com/download)
- Open Unity Hub, sign in, and install Unity 6 (6000.0.29f1) LTS
    - Select the following:
        - "Microsoft Visual Studio Community 2022"
        - "Android Build Support"
        - "OpenJDK"
        - "Android SDK & NDK Tools"
- Click "Install" and wait for the installation to finish

### Step 2: Add Unity Support to Visual Studio (Optional)

:::note
This step is only necessary if you already have Visual Studio 2022 installed and the Unity installer fails to add Unity support.
:::

- Download the Visual Studio 2022 Community installer from [Microsoft](https://visualstudio.microsoft.com/downloads/)
- Run the installer and select `Modify`
- Scroll down to the `Gaming` subsection and select `Game development with Unity`
- Click `Install` to add Unity support

### Step 3: Install Quest Link (Optional)

:::tip
Installing Quest Link makes debugging on the headset much easier, though it's not strictly required.
:::

- Download the Quest Link software [here](https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-rift-s/install-app-for-link/)
- Double-click the installer and proceed through the installation
- Once installed, enable the beta/debug options by navigating to `Settings > Beta > Developer Runtime Features`

### Step 4: Add Meta SDKs to Your Unity Account

:::warning
These SDKs must be added to your Unity account before you can import the project.
:::

- Log into your Unity [account](https://id.unity.com) in your browser
- Add the following free SDKs to your account:
    - [Meta XR All-in-One SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)
    - [Meta XR Interaction SDK Essentials](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-essentials-264559)
    - [Meta MR Utility Kit](https://assetstore.unity.com/packages/tools/integration/meta-mr-utility-kit-272450)

### Step 5: Clone the Repository

1. Fork the QuestNav repository on GitHub
2. Clone your fork to your local machine:
   ```
   git clone https://github.com/YOUR-USERNAME/QuestNav.git
   ```

### Step 6: Import the Project into Unity

- Open Unity Hub
- Click `Add > Add project from disk` and select the unity subfolder in the cloned repository
- Click on the newly imported project
- Wait for Unity to compile assets and open the main interface

### Step 7: Install Required Packages

:::danger
The MessagePack plugin is required for Network Tables communication with the robot. The project will not function without it.
:::

Install the MessagePack plugin for Unity:

- Download the latest release with a `.unitypackage` extension from [the MessagePack-CSharp GitHub repository](https://github.com/MessagePack-CSharp/MessagePack-CSharp/releases)
- In Unity, select `Assets > Import Package > Custom Package`
- Browse to the package you downloaded and click `Import`

### Step 8: Configure Build Settings for Android

- Navigate to `File > Build Profiles`
- Under `Platforms` select `Android`
- Click on `Switch Platform`
- Wait for Unity to recompile assets

### Step 9: Set Up Project for Meta Quest

- Navigate to `Meta > Tools > Project Setup Tool`
- Apply all the recommended fixes on the default tab (Android)
- Cycle through the rest of the tabs and apply all fixes
- Ensure the Android tab shows a green ✅ and reports its status as "XR Ready for Android"

## Building and Testing

### Building the Project

#### Using Unity's Standard Build Tool:

- Navigate to `File > Build Profiles`
- Make sure you're on the Android platform
- **IMPORTANT:** Check ✅ `Development Build` option
- Click `Build and Run`

:::warning
If you do not check the `Development Build` option, the build will fail to run on the headset.
:::

#### Using OVR Build (Faster):

:::tip
The OVR Build tool provides much faster build times but will consume more system resources during the build process.
:::

- Navigate to `Meta > OVR Build > OVR Build APK...`
- **IMPORTANT:** Check ✅ `Development Build?` option
- Set your build path and click `Build`

:::warning
If you do not check the `Development Build` option, the build will fail to run on the headset.
:::

### Installing on Your Quest Headset

- Connect your Quest headset to your PC
- Enable developer mode on your headset if you haven't already
- Use SideQuest, the Meta Developer Hub, or ADB to install the APK
:::tip
Check the getting started guide for a step-by-step tutorial on how to install the APK [here](../1-getting-started/4-app-setup.md#installing-questnav).
:::

## Troubleshooting Common Issues

:::note
If you encounter any issues during setup, check this section before reaching out for help.
:::

- **Unity Crashes During Import**: Try closing and reopening Unity Hub, then reimport the project
- **Missing SDK References**: Ensure you've added all required SDKs to your Unity account
- **Build Fails**: Make sure you've enabled the Development Build option
- **APK Doesn't Install**: Verify developer mode is enabled on your Quest headset
- **App Crashes on Launch**: Check that you've installed all required packages, including MessagePack