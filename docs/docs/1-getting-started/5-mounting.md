---
title: Mounting 
---
# Mounting

Properly mounting your Quest headset on your robot is crucial for reliable tracking and navigation. This guide covers mounting options, positioning considerations, and best practices.

## Mount Design

QuestNav requires the Quest headset to be securely mounted to your robot while maintaining visibility of the environment. We recommend using 3D printed mounts specifically designed for this purpose.

:::info
The specific mount design you need depends on your Quest model. QuestNav is optimized for the Quest 3 and Quest 3S headsets, which have a slightly different form factor than older models.
:::

### 3D Printed Mount Options

We provide several 3D printable mount designs optimized for different robot configurations:

1. **Standard Mount**: A basic mount suitable for most robot designs
2. **Low-Profile Mount**: A slimmer mount, designed by 3847 Spectrum.

Download the STL files from the [QuestNav GitHub repository](https://github.com/QuestNav/QuestNav/blob/main/mounts) in the `mounts` directory.

:::tip
If you don't have access to a 3D printer, check with other local FRC teams, your school's engineering department, or local makerspaces that might offer 3D printing services.
:::

### Printing Guidelines

- **Material**: PLA or PETG is recommended for durability
- **Infill**: 25-30% minimum for structural integrity
- **Layer Height**: 0.2mm or finer for smoother surfaces
- **Supports**: Required for overhangs

:::warning
Avoid using ABS for printing mounts, as it can be brittle and crack under the vibration and impacts experienced during competitions.
:::

## Mounting Position

The optimal mounting position for your Quest headset depends on several factors:

### Height Considerations

- Mount the headset **at least 12 inches (30cm) above the floor**
- Ensure the headset has a clear view of the surroundings

:::danger
Mounting the headset too low can result in poor tracking as the cameras may only see the field carpet and nearby robot parts rather than distinctive environmental features.
:::

### Orientation

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