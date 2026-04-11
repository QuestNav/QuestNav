---
title: Coordinate Systems
---

# Coordinate Systems

QuestNav operates across three coordinate systems. Understanding these is essential for interpreting pose data and debugging position issues.

## The Three Systems

| System | X | Y | Z | Rotation | Handedness |
|--------|---|---|---|----------|------------|
| **FRC Field** | Forward (toward red alliance) | Left | Up | Counter-clockwise positive | Right-handed |
| **Unity World** | Right | Up | Forward | Clockwise positive | Left-handed |
| **Computer Vision** | Right | Down | Forward | — | Right-handed |

### FRC Field Coordinates

The standard coordinate system used by WPILib and FRC robot code. The origin is at the **blue alliance driver station corner** of the field.

- **X** runs along the field length toward the red alliance wall
- **Y** runs across the field width toward the left (from the blue driver station perspective)
- **Z** points up

This is the coordinate system used by the field layout JSON, all internal geometry (`Pose3d`, `Translation3d`, etc.), the Kalman filter state, and the data published over NetworkTables.

### Unity World Coordinates

The coordinate system used by the Quest headset's internal tracking (VIO). The origin is wherever the headset was when tracking initialized — it is **not** field-relative.

- **X** points right
- **Y** points up
- **Z** points forward

Unity uses a **left-handed** coordinate system, while FRC uses right-handed. This means rotations are in opposite directions and axes must be shuffled and negated during conversion.

### Computer Vision Coordinates

The coordinate system used by PoseLib (the PnP solver) internally. PoseLib follows the COLMAP convention:

- **X** points right in the image
- **Y** points down in the image
- **Z** points forward (into the scene)

## Translation Mappings

### FRC to Unity

```
Unity.x = -FRC.y     (FRC left → Unity right, negated)
Unity.y =  FRC.z     (FRC up → Unity up)
Unity.z =  FRC.x     (FRC forward → Unity forward)
```

### Unity to FRC

```
FRC.x =  Unity.z     (Unity forward → FRC forward)
FRC.y = -Unity.x     (Unity right → FRC left, negated)
FRC.z =  Unity.y     (Unity up → FRC up)
```

### CV to FRC

The CV-to-FRC conversion is more complex because PoseLib returns a **world-to-camera** transform, not a camera-in-world position. The conversion involves two steps:

1. **Invert the transform** to get the camera position in world (FRC) coordinates:

$$
C_{world} = -R^{-1} \cdot t
$$

where $R$ and $t$ are the rotation and translation from PoseLib's output.

2. **Apply a body rotation** to convert the camera's orientation from CV axes to FRC body axes (X-forward, Y-left, Z-up):

$$
q_{body} = R^{-1} \cdot q_{cv \rightarrow body}
$$

where $q_{cv \rightarrow body} = (w=0.5,\ x=-0.5,\ y=0.5,\ z=0.5)$ corresponds to the rotation matrix:

$$
R_{cv \rightarrow body} = \begin{bmatrix} 0 & 0 & 1 \\ -1 & 0 & 0 \\ 0 & -1 & 0 \end{bmatrix}
$$

## Quaternion Mappings

### FRC to Unity Quaternion

```
Unity.x =  FRC.qy
Unity.y = -FRC.qz
Unity.z = -FRC.qx
Unity.w =  FRC.qw
```

### Unity to FRC Quaternion

```
FRC.qx = -Unity.z
FRC.qy =  Unity.x
FRC.qz = -Unity.y
FRC.qw =  Unity.w
```

:::warning
QuestNav has two different `Quaternion` types: `UnityEngine.Quaternion` (float, left-handed) and `QuestNav.QuestNav.Geometry.Quaternion` (double, right-handed, WPILib-compatible). They are **not interchangeable**. The geometry quaternion uses `(W, X, Y, Z)` constructor order, while Unity's uses `(X, Y, Z, W)`.
:::

## The VIO Frame Alignment Problem

When converting VIO data from Unity to FRC coordinates, `UnityToFrc3d` applies the fixed axis mapping above. However, the Unity world axes are determined by the **headset's orientation at VIO startup**. If the headset boots facing east but FRC +X points north, all VIO data will be rotated relative to the true FRC frame.

This is corrected by the **yaw offset** — a scalar rotation computed during Phase 1 of AprilTag alignment. After Phase 1, all VIO displacements are rotated by this offset before being applied to the Kalman filter state:

$$
d_{corrected} = R_{yaw}(yawOffset) \cdot d_{raw}
$$

Without this correction, the headset's movement appears rotated (often by 90 degrees) on the field visualization.

## Important Notes

- `CvToFrc` returns FRC-space values stored in Unity types (`Vector3`, `UnityEngine.Quaternion`). These are **not** Unity world coordinates — they represent FRC field positions using Unity as a math container.
- The output heading published over NetworkTables is the VIO rotation with the yaw offset applied extrinsically: `correctedRotation = latestVioRotation.RotateBy(Rotation3d(0, 0, yawOffset))`.
- The field layout JSON stores all tag positions in FRC coordinates with the origin at the blue alliance corner. Verify your visualization tool uses the same convention.
