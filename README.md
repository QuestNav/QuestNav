# About QuestNav

**[JUMP TO DOCS](https://questnav.gg/)**

QuestNav enables streaming Oculus VR headset pose information to an FRC robot using the Network Tables 4 (NT4) protocol. This pose data provides robot control systems with accurate mapping and navigation capabilities in competition fields, practice spaces, or any location.

QuestNav produces a more stable and reliable tracking pose than any other FRC vision solution (LimeLight, Photon Vision, etc.),
making it an ideal choice for teams seeking precise robot localization.

## Key Benefits

- More stable and reliable tracking than other FRC vision solutions
- Lower cost compared to most FRC vision solutions
- Multiple SLAM cameras for redundant, calibrated VIO (Visual Inertial Odometry)
- Camera tracking fused with onboard IMU for accurate position and velocity estimates
- Up to 120Hz robot odometry refresh rate (linked to the Quest headset framerate)
- Powered by [Qualcomm XR2G2 platform](https://www.qualcomm.com/products/mobile/snapdragon/xr-vr-ar/snapdragon-xr2-gen-2-platform) with multiple CPU/GPU cores, 8GB RAM, dedicated video hardware, and 128GB+ storage
- Well-supported ecosystem with off-the-shelf 3D mapping libraries
- Self-contained, rechargeable battery

The Quest 3S headset is recommended for FRC applications due to its lower cost and excellent tracking performance.
The depth projector on the Quest 3 doesn't provide significant benefits for robot navigation.

## How It Works

QuestNav uses the Quest headset's Visual-Inertial Odometry (VIO) system - the same technology that powers VR
gaming - to track position in 3D space with remarkable accuracy. The system:

1. Captures visual data through the headset's cameras
2. Combines this with inertial data from the built-in IMU
3. Processes this information to determine position and orientation in real-time
4. Transmits this data to the robot via a wired Ethernet connection
5. Makes the information available through Network Tables for robot code to use

The same technology that prevents motion sickness in VR by precisely tracking head movements is now used to track your
robot's position with high precision.

## Software Architecture

QuestNav implements a simple bidirectional communication structure between the VR headset and robot, enabling:
- Heading/pose reset requests
- Configuration updates
- Ping functionality
- Real-time pose data streaming

## Demo Video

Check out this demo video to see QuestNav in action:

View the [full video on YouTube](https://youtu.be/Mo0p1GGeasM).


## Thanks

QuestNav exists because of many sidebar discussions, technical deep-dives, and what-if conversations with coworkers and members of the FIRST community. Special thanks to the following contributors who made this project possible:

- [@juchong](https://github.com/juchong)
- [@SunnyBat](https://github.com/SunnyBat)
- [@ThadHouse](https://github.com/ThadHouse)
- [@SeanErn](https://github.com/SeanErn)
- [@jasondaming](https://github.com/jasondaming)
- [@allengregoryiv](https://github.com/allengregoryiv)

## Support
For questions, troubleshooting help, or to share your experiences with QuestNav, join the community discussion on our [Discord](https://discord.gg/Zfan2qgkRZ).

# **Ready to get started? Read the docs [here!](https://questnav.gg/)**