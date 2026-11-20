---
title: About QuestNav
---

# About QuestNav

QuestNav enables streaming Meta Quest headset pose information to an FRC robot using the Network Tables 4 (NT4) protocol. This pose data provides robot control systems with accurate mapping and navigation capabilities in competition fields, practice spaces, or any location.

:::info
QuestNav produces a more stable and reliable tracking pose than any other FRC vision solution (LimeLight, Photon Vision, etc.), making it an ideal choice for teams seeking precise robot localization.
:::

## Key Benefits

- More stable and reliable tracking than other FRC vision solutions
- Lower cost compared to most FRC vision solutions
- Multiple SLAM cameras for redundant, calibrated VIO (Visual Inertial Odometry)
- Camera tracking fused with onboard IMU for accurate position and velocity estimates
- Up to 120Hz robot odometry refresh rate (linked to the Quest headset framerate)
- Powered by [Qualcomm XR2G2 platform](https://www.qualcomm.com/products/mobile/snapdragon/xr-vr-ar/snapdragon-xr2-gen-2-platform) with multiple CPU/GPU cores, 8GB RAM, dedicated video hardware, and 128GB+ storage
- Well-supported ecosystem with off-the-shelf 3D mapping libraries
- Self-contained, rechargeable battery
- FRC AprilTag integration for automatic field positioning

:::tip
The Quest 3S headset is recommended for FRC applications due to its lower cost and excellent tracking performance. The depth projector on the Quest 3 doesn't provide significant benefits for robot navigation.
:::

## How It Works

QuestNav uses the Quest headset's Visual-Inertial Odometry (VIO) system - the same technology that powers VR gaming - to track position in 3D space with remarkable accuracy. The system:

1. Captures visual data through the headset's cameras
1. Combines this with inertial data from the built-in IMU
1. Processes this information to determine position and orientation in real-time
1. Detects AprilTags and solves for the headset position on the field
1. Uses a [Kalman filter](https://en.wikipedia.org/wiki/Kalman_filter) to fuse VIO data with AprilTag data to provide an absolute position on the FRC field
1. Transmits this data to the robot via a wired Ethernet connection
1. Makes the information available through Network Tables for robot code to use

:::note
The same technology that enables VR by precisely tracking head movements is now used to track your robot's position on the competition field!
:::

## Software Architecture

QuestNav implements a simple bidirectional communication structure between the VR headset and robot, enabling:
- Real-time pose data streaming over NetworkTables, with tracking, AprilTag alignment state and device status
- Pose reset requests from robot code
- Configuration, logs and passthrough camera video streaming through the headset's web interface

## Demo Video

Check out this demo video to see QuestNav in action:

<iframe width="560" height="315" src="https://www.youtube.com/embed/Mo0p1GGeasM?si=pigvJwCiWEIoZxlO" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

For a more comprehensive demonstration, view the [full video on YouTube](https://youtu.be/Mo0p1GGeasM).


## Thanks

QuestNav exists because of many sidebar discussions, technical deep-dives, and what-if conversations with coworkers and members of the _FIRST_ community. Special thanks to everyone who has contributed to the project:

<a href="https://github.com/QuestNav/QuestNav/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=QuestNav/QuestNav" alt="QuestNav contributors" />
</a>

## Support

:::info
For questions, troubleshooting help, or to share your experiences with QuestNav, join the community discussion on our [Discord](https://discord.gg/CsRjKfn8Xa).
:::


## Next Steps
Ready to get started? The fastest way to get up and running is by following the [Quick Start](./quick-start) guide. You can also use the [QuestNav Setup Page](https://setup.questnav.gg/) to configure your headset automatically.