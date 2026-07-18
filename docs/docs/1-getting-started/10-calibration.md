---
title: Calibration
---
# Calibration

Once QuestNav is installed and your robot code is set up, the final step before competition is calibrating the headset on your robot. This guide covers headset calibration — getting the Quest running, mounted, and reporting a usable pose — and introduces field calibration for aligning that pose to the field.

:::note One-time setup
Calibration is a **one-time** step. You only need to redo it if you **change the headset mount** or **replace the headset** — anything that changes where the Quest sits relative to the robot. If the mount and headset are unchanged, your existing offset transform still applies, and there's no extra work to do before each match.
:::

## Headset Calibration

Follow these steps to bring the headset online and confirm it is tracking before you rely on it.

### 1. Don the headset

Put the headset on and confirm it has established tracking in its environment. Wearing it first lets you verify the cameras have a clear view and that the Quest is not showing a boundary or tracking warning.

:::tip
If the Quest prompts you to redraw a boundary or can't establish tracking, move to an area with more visual features and better lighting. Featureless or very dark spaces make tracking unreliable.
:::

### 2. Launch QuestNav

Open the QuestNav app from your Quest's app library (under **Unknown Sources** if it isn't pinned). If you enabled **Auto Start On Boot** during [App Setup](./app-setup), QuestNav launches automatically when the headset powers on.

### 3. Connect to the Quest's network

Connect the device you'll use for calibration (a laptop or phone) to the **same network as the Quest** — typically your robot radio's network. QuestNav displays the headset's current IP address in the app; note it for the next step.

:::info
The headset's IP address is also reported through the [`/api/status`](../api-reference/web-api) endpoint as `ipAddress`. When connected to your robot radio, the Quest uses your team's network (`10.TE.AM.x`).
:::

### 4. Open the web interface

In a browser on that same network, navigate to:

```
http://<quest-ip>:5801
```

Replace `<quest-ip>` with the IP address shown in the QuestNav app. This opens the QuestNav **ConfigServer** web interface, where you can view live status (tracking state, battery, pose), adjust configuration, and reset the pose. See the [Web API reference](../api-reference/web-api) for the full list of available endpoints.

### 5. Mount the headset to the robot

Secure the headset to its robot mount. If you haven't built or attached a mount yet, follow the [Mounting](./mounting) guide first. A rigid, stable mount is critical: because QuestNav reports the *headset's* position, any movement of the headset relative to the robot shows up as position error on the field.

### 6. Determine the Quest's offset transform

The Quest reports its own pose, so your robot code needs to know where the headset sits relative to the center of the robot. This offset is the `ROBOT_TO_QUEST` transform (X forward, Y left, Z up, plus rotation).

:::note Placeholder
A detailed, guided procedure for measuring the Quest's offset transform from the center of the robot will be added here.

For now, measure the offset by hand and set it in your robot code as described in [Measuring `ROBOT_TO_QUEST`](./robot-code#measuring-robot_to_quest).
:::

### 7. Follow the robot code setup

With the headset mounted and the offset determined, complete the software integration in [Robot Code Setup](./robot-code). That guide covers adding the vendor dependency, reading poses, and feeding QuestNav measurements into your drive pose estimator.

## Field Calibration

Field calibration aligns the Quest's tracking origin with the field coordinate system, so the pose QuestNav reports matches the robot's true position on the field.

:::warning Big TODO
Automatic field calibration is a **work in progress**. It depends on spatial anchor support, which is not yet available in QuestNav. This section will be expanded once that work lands.

In the meantime, align the robot to the field by resetting the pose to a known location at the start of each match — for example, seeding your pose estimator from the selected autonomous start position, or using an external source such as AprilTags ([Limelight](https://limelightvision.io/) / [PhotonVision](https://photonvision.org/)). See [Setting Robot Pose](./robot-code#setting-robot-pose) for how to push a known pose to QuestNav.
:::

## Next Steps

With the headset calibrated, review the [Pre-Match Checklist](./pre-match-checklist) before your first match.
