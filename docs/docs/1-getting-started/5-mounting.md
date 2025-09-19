---
title: Mounting 
---
# Mounting

Securely mounting your Quest headset on your robot is crucial for reliable tracking and navigation. This guide covers mounting options, positioning considerations, and best practices.

## Mount Design

QuestNav requires the Quest headset to be securely mounted to your robot in a stable, rigid location such as the robot base or frame that has a clear and unobstructed view of the field. Keep in mind that the headset's position directly correlates to your robot's position on the field, so allowing the headset to move independent of the robot will lead to position drift on the field.

We recommend using a 3D printed mount designed for your particular headset for this purpose.

:::info
The specific mount design you need depends on your Quest model. QuestNav is optimized for the Quest 3 and Quest 3S headsets, which have a slightly different form factor than older models.
:::

### 3D Printed Mount Options

STLs and STEP files for both Quest 3 and Quest 3S headsets are available at the links below: 

[Quest 3S Headset Mount for Robots and Autonomous Vehicles](https://www.printables.com/model/1100711-quest-3s-headset-mount-for-robots-and-autonomous-v)

[Quest 3 Headset Mount for Robots and Autonomous Vehicles](https://www.printables.com/model/1324702-quest-3-headset-mount-for-robots-and-autonomous-ve)

:::tip
If you don't have access to a 3D printer, check with other local FRC teams, your school's engineering department, or local makerspaces that might offer 3D printing services.
:::

### Printing Guidelines

- **Material**: PLA+, PETG, or CF-Nylon filaments are recommended for durability and rigidity
- **Infill**: 25-30% minimum for structural integrity
- **Layer Height**: 0.2mm or finer for smoother surfaces
- **Supports**: Required for overhangs (on build plate only)

:::warning
Avoid using ABS for printing mounts, as it will shrink and may crack under the vibration and impacts experienced during competitions.
:::

## Mounting Position

The optimal mounting position for your Quest headset depends on several factors:

### Height Considerations

- Mount the headset **at least 12 inches (30cm) above the floor**
- Ensure the headset has a clear view of the surroundings
- Avoid mounting the headset where its view is largely obscured by motors or other robot parts that spin/move a lot

:::danger
Mounting the headset too close to the ground can result in poor tracking as the cameras may only see the field carpet and nearby robot parts rather than distinctive environmental features. It also makes damage from robot-to-robot interactions more likely! 
:::

### Orientation

It's important to remember that the headset was designed to be worn on a person's head. This means that the headset's front-facing and side cameras will favor one direction than another since they're designed to "look down" rather than directly outwards. 

- **Upright Orientation**: Ideally, keep the headset in its normal upright position
- **Level Positioning**: Mount should keep the headset approximately level

:::info
While the Quest can track in any orientation, keeping it in its designed upright position produces the most reliable results for robot navigation. If you wouldn't play VR in that position, try not to mount it that way!
:::

### Field of View Requirements

The Quest headset needs clear visibility to track effectively:

- Ensure the front and side cameras have an unobstructed view
- Avoid mounting near robot mechanisms that could block the cameras
- Consider potential obstructions during robot operation

:::note
The Quest cameras are located on the front and sides of the headset. Make sure your mount and robot design don't block these cameras.
:::

## Secure Attachment

Once positioned, the headset must be firmly secured to prevent movement or vibration:

### Using Zip Ties

1. Thread zip ties through the designated channels on the mount
2. Carefully secure around the headset, avoiding excessive pressure on displays or buttons
3. Use at least 4 zip ties for redundancy (two on each side)
4. Trim excess zip tie length to prevent interference

:::warning
Overtightening zip ties can damage the headset. Secure it firmly but without applying excessive pressure on the plastic casing.
:::

### Additional Security

- Use lock washers or thread locker on mount-to-robot bolts
- Perform a "shake test" to ensure the headset doesn't move

## Wiring Considerations

When mounting, plan for cable management:

- Ensure the USB-C port is accessible for the Ethernet adapter
- Leave sufficient slack in cables to prevent tension
- Secure cables along the mount to prevent snagging

:::danger
Cables under tension can damage connectors or pull loose during matches. Always include a small service loop and secure cables to prevent strain.
:::

## Testing Visibility

After mounting, verify that the Quest has adequate visibility:

1. Power on the headset while mounted
2. Use the pass-through view to confirm cameras can see the environment
3. Check that critical field elements will be visible during operation

:::tip
To access pass-through view, launch the QuestNav app. This allows you to see what the cameras see, helping verify proper positioning.
:::

## Video Guide
[Placeholder for Mounting Video Guide]

## Next Steps
Once your Quest is securely mounted, proceed to the [Wiring](./wiring) section to learn how to connect your headset to the robot's network.