---
title: Upcoming Changes
description: Tracking progress on planned improvements to QuestNav.
id: rewrite
---
# Upcoming Changes
:::info
This page tracks planned and completed improvements to QuestNav.
:::
## Completed

- ✅ **Properly implement NT4 (Network Tables 4)** (#26)
  - Wrapper library for NT4 in C#

- ✅ **Enable streaming passthrough video to devices on the robot network** (#40)
  - Remote visualization capability

- ✅ **AprilTag detection and tracking** (#31)
  - The app detects the field's AprilTags and fuses them with the headset's tracking to report a field-relative pose.
    See [AprilTag Tracking](./4-apriltag-tracking/1-apriltag-overview.md).<br /><br />

  <iframe width="560" height="315" src="https://www.youtube.com/embed/EBK9KA7otfA" title="QuestNav AprilTag detection preview" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

- ✅ **Field calibration** (#34)
  - AprilTag alignment places the headset on the field, so no separate calibration mode or per-tag spatial anchors
    (#32) are needed. See [Field Calibration](./1-getting-started/9-calibration.md#field-calibration).

## In Progress

- **WPILib 2027 and SystemCore support**
  - A 2027 QuestNavLib for SystemCore, used with the same headset app as 2026 robots. Not yet tested on SystemCore
    hardware. See [SystemCore and WPILib 2027](./1-getting-started/14-systemcore-2027.md).

## Planned

- **Add a "competition how-to guide" to simplify use during events** (#27)
  - Documentation for real-world usage

- **Create a setup video that walks teams through the setup and configuration process** (#48)
  - Onboarding documentation