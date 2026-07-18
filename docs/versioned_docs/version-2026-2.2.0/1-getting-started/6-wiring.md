---
title: Wiring 
---
# Wiring

Proper wiring is essential for reliable communication between your Quest headset and robot. This guide covers Ethernet connections, power options, and best practices for secure wiring.

## Ethernet Connection

QuestNav requires a direct Ethernet connection between your Quest headset and the robot's network.

:::info
Per FRC rules, wireless communication is not allowed within the robot. The Ethernet connection ensures compliance with competition rules. See the [current game manual](https://www.firstinspires.org/resources/library/frc/season-materials) for details.
:::

### Basic Setup

1. Connect your USB-C to Ethernet adapter to the Quest headset
2. Connect an Ethernet cable between the adapter and your robot's network switch
3. Use the shortest cable possible to minimize signal loss and physical interference

### Cable Selection

- **Cable Type**: CAT5e or CAT6 Ethernet cable
- **Length**: Ideally under 3 feet (1 meter)
- **Shielded**: Recommended in high-EMI environments
- **Strain Relief**: Consider right-angle connectors if space is limited

:::tip
Shielded Ethernet cables (STP) provide better resistance to electromagnetic interference from motors and other robot components compared to unshielded (UTP) cables.
:::

## Power Options

There are two recommended ways to power the Quest headset on a robot. A **USB battery bank is the preferred option** for most teams because it is simple, isolated from the robot's electrical system, and easy to swap between matches. If you'd rather power the headset directly from the robot's 12V battery, a regulated 5V DC-DC converter such as the Redux Robotics [Zinc-V](https://shop.reduxrobotics.com/products/zinc-v) is a solid alternative.

### Preferred: USB Battery Bank

A USB battery bank mounted on the robot provides clean, stable power that is fully isolated from the rest of the robot's electrical system.

1. Mount a USB battery bank securely on the robot
2. Connect a USB-A to USB-C cable from the battery bank to your adapter's power input
3. The battery bank should supply enough power to sustain the Quest headset indefinitely
4. Charge state can be monitored externally using the power meter on the battery bank

:::warning
Some USB-C to Ethernet adapters may boot loop when exposed to USB Power Delivery (PD) voltages higher than 5V. If you experience this, use a USB-A to USB-C cable (which forces 5V output) or a battery bank that does not support PD negotiation.
:::

### Alternative: Robot-Powered via a Regulated 5V Converter

If you prefer to power the headset directly from the robot's 12V system (no separate battery to charge or swap between matches), use a high-quality regulated 5V DC-DC converter. The Redux Robotics [Zinc-V](https://shop.reduxrobotics.com/products/zinc-v) is purpose-built for this kind of FRC vision/co-processor power and is a good fit:

- Fixed 5V output at 4A continuous (5A peak), wide 5.3–16V input range
- Over-voltage, over-current, and thermal protection — important on a robot bus that can spike during impacts
- Locking USB-C connector designed for high-vibration environments
- Slim 12mm profile that fits in tight robot mounting spaces

Wire it from a fused 12V rail on the PDH/PDP and run its USB-C output to your Ethernet adapter's power input. Because the Zinc-V outputs a fixed 5V (no PD negotiation), it sidesteps the same boot-loop issue described in the warning above.

:::warning
If you substitute a generic buck converter instead of a tested unit like the Zinc-V, verify it holds a stable 5V under transient loads and that it includes over-voltage protection. A failure on this rail will brown out the USB-C to Ethernet adapter, causing connectivity loss during the match. The headset itself will continue running on its internal battery, but it will lose its NetworkTables connection to the robot.
:::

### Power Requirements

- **Voltage**: 5V (non USB PD) to 9V USB PD
- **Power**: 18W (5V 4A, or 9V 2A)
- **Connector**: USB-C

### Without Power Passthrough

If your adapter does not support power passthrough:

1. Fully charge the Quest headset before each match
2. The Quest battery typically lasts 2 hours max while running QuestNav

:::warning
Don't rely on the Quest's internal battery during competition — it will not last a full event and can shut the headset down mid-match. Always provide continuous external power, either from a USB battery bank or a regulated 5V rail off the robot.
:::

## Troubleshooting

For wiring-related issues — no network connection, intermittent drops, the adapter not getting power, or the headset not charging from passthrough — see the [Troubleshooting](./troubleshooting) page. The relevant sections are **Connection Issues** and **Headset Not Charging from Passthrough Adapter**.

## Next Steps
With your Quest properly wired, proceed to the [Testing & Simulation](./simulation) section to verify your setup, or skip ahead to [Robot Code Setup](./robot-code) to start integrating QuestNav into your robot's software.
