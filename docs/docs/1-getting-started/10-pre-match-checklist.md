---
title: Pre-Match Checklist
---
# Pre-Match Checklist

Run through this checklist before every match to make sure QuestNav is powered, connected, and tracking. A quick, consistent routine in the pits and on the field prevents most competition-day surprises.

## Connections and Power

- [ ] **Ethernet and USB-C connections** are seated and secure at the adapter, the headset, and the radio (or switch).
- [ ] **USB battery bank is connected** to the adapter's power input.
- [ ] **Headset is secure** on its mount. Hook-and-loop straps or a bungee cord are recommended as backup retention.

## Headset Status

- [ ] **Headset is powered** — the green LED next to the USB-C port is lit.
- [ ] **Headset is on** — the white LED on the front is lit and gently pulsing.

:::tip
A gently pulsing white LED means the headset is awake. If it is off, tap the power button to wake the Quest. If the green USB-C LED is off, re-check the battery bank and power cable.
:::

## Robot Communication

- [ ] **QuestNav is online** — confirm the robot is communicating with the headset from the Driver Station.
- [ ] **Robot position matches the selected Auto** — verify the robot's pose on the field (e.g. in AdvantageScope or your dashboard) lines up with the autonomous routine you've selected.

:::tip
Publishing QuestNav status (connected, tracking, battery) to your dashboard makes these checks fast. See [Publishing Diagnostics](./robot-code#publishing-diagnostics) for an example.
:::

## AprilTags

- [ ] **The right field layout is selected** in the AprilTag tab of the web interface (a change needs an app restart).
- [ ] **AprilTag alignment** — with tags in view, the alignment state reaches `TAG_ALIGNED` (2027 QuestNavLib: `questNav.getAlignmentState()`). If you seed the pose with `setPose()` instead, it reads `POSE_RESET` until the headset sees tags. The 2026 QuestNavLib can't read the alignment state: check that the reported pose matches where the robot is. The **Logs** tab of the web interface shows `AprilTag initial alignment accepted` when tags first place the headset, but with tags in view the log fills fast and that line is gone within seconds. After it, rejected tag views are logged as `AprilTag rejected (Phase 2)`; `AprilTag rejected (Phase 1)` means the headset isn't aligned yet.

See [Field Calibration](./9-calibration.md#field-calibration) for background.

## Next Steps

After your match, follow the [Post-Match Checklist](./post-match-checklist).
