---
title: Wiring 
---
# Wiring

Proper wiring is essential for reliable communication between your Quest headset and robot. This guide covers Ethernet connections, power options, and best practices for secure wiring.

## Ethernet Connection

QuestNav requires a direct Ethernet connection between your Quest headset and the robot's network.

:::info
As per FRC robot rules (R707 in the 2024 rules), wireless communication is not allowed within the robot. The Ethernet connection ensures compliance with competition rules.
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

Depending on your selected adapter, you have different power configuration options:

### Option 1: Power Passthrough (Recommended)

If you selected an adapter with power passthrough capabilities:

1. Connect your robot's power source to the USB-C power input on the adapter
    - Use a regulated 5V power supply (2A minimum)
    - Many teams use a voltage regulator connected to the robot battery
2. The headset will charge while operating, allowing for extended runtime

### Option 2: Battery Operation

If using an adapter without power passthrough:

1. Fully charge the Quest headset before each match
2. The Quest battery typically lasts 2-3 hours in QuestNav mode
3. Consider a portable battery bank for extended practice sessions

:::note
While the Quest can operate on its internal battery, this approach is not recommended for competition use due to the risk of battery depletion during long events.
:::

### Power Requirements

- **Voltage**: 5V DC ±5%
- **Current**: 2-3A recommended
- **Connector**: USB-C

## Power Source Options

There are several ways to power the Quest headset + USB to Ethernet adapter on an FRC robot:

### 1. Power via RoboRIO USB Port

The USB ports on the RoboRIO provide a stable 5V source:

1. Connect a USB-A to USB-C cable from the RoboRIO to your adapter
2. This is the simplest option, requiring no additional components

:::warning
The RoboRIO USB ports will not supply enough power to charge the Quest headset while in use, only delay its death. You'll need to monitor charge levels throughout the event.
:::

### 2. Power via 5V USB Battery Bank

Using a portable battery bank offers several benefits:

1. It provides clean, reliable power to the headset and adapter
2. The battery should supply enough power to sustain the Quest headset indefinitely
3. Charge state can be monitored externally using the power meter on the battery bank

:::danger
Only use 5V output from power banks. Avoid banks that support USB-C Power Delivery (PD) or use a USB-A to USB-C cable to force 5V delivery. Some adapters will boot loop when voltage greater than 5V is applied.
:::

### 3. Power via 5V Regulator

Using a dedicated voltage regulator from your robot's main battery:

1. Connect a quality 5V regulator to your robot's power distribution panel
2. Ensure adequate current capability (minimum 3A output)
3. Connect the regulator output to your USB-C adapter's power input

Recommended USB-compliant 5V regulators:
- [Redux Robotics Zinc-V](https://shop.reduxrobotics.com/zinc-v/)
- [Grapple Robotics MitoCANdria](https://www.thethriftybot.com/products/mitocandria)

Recommended 5V regulators (requires soldering and custom circuitry):
- [Pololu D36V50F5 Regulator](https://www.pololu.com/product/4091)

:::tip
For non-compliant regulators, use a USB breakout board like [this one](https://a.co/d/gLUZN0Z) that includes onboard sense resistors so that the headset knows that it's connected to a 5V power source.
:::

## Wiring Best Practices

Follow these guidelines to ensure reliable operation:

### Secure Connections

- Use zip ties or cable clips to secure cables to the robot frame
- Leave small service loops at connection points to prevent tension
- Avoid tight bends in cables (maintain at least 1" bend radius)
- Label cables for easy identification during maintenance

:::info
Service loops are small, intentional slack sections in the cable that prevent tension from being applied directly to connectors. They're essential for preventing connection failures during robot movement.
:::

### Redundancy

- Consider having a backup Ethernet cable and adapter ready
- Create quick-disconnect points for faster field repairs
- Test connections before each match

:::tip
Create a simple checklist to verify all connections before matches. A quick visual inspection can catch loose cables before they cause problems.
:::

## Troubleshooting Common Issues

### No network connection
- Check adapter LED indicators
- Try a different Ethernet cable
- Verify the adapter is properly seated in the Quest's USB-C port

:::note
Most Ethernet adapters have status LEDs that indicate link and activity. No lights usually indicates a power or connection issue.
:::

### Intermittent connection
- Look for loose connectors or cable damage
- Ensure cables are properly secured to prevent movement

### Headset not charging
- Verify power supply output and connections
- Check for bent pins in the USB-C connector
- Test with a known working power source

:::danger
If your Quest is losing charge during operation despite being connected to power, check your power source immediately. Running out of battery during a match is very bad!
:::

## Video Guide
[Placeholder for Wiring Video Guide]

## Next Steps
With your Quest properly wired, proceed to the [Robot Code Setup](./robot-code) section to configure your robot's software for QuestNav integration.
