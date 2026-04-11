---
title: Field Layout & Native Interop
---

# Field Layout & Native Interop

This page describes how field layout data is loaded and how QuestNav interfaces with its native C/C++ libraries.

## Field Layout

### Loading

The field layout is loaded during `QuestNav.Awake()` from a JSON file in `StreamingAssets/apriltag/fieldlayouts/`. The filename is currently **hardcoded**:

```csharp
await aprilTagFieldLayout.LoadJsonFromFileAsync("2026-rebuilt-welded.json");
```

To use a different field layout, this line must be edited and the app rebuilt.

### JSON Format

The layout file follows the WPILib field layout format. Each tag entry contains:

```json
{
  "ID": 1,
  "pose": {
    "translation": { "x": 16.697198, "y": 0.655294, "z": 1.4859 },
    "rotation": {
      "quaternion": { "W": 0.4539905, "X": 0.0, "Y": 0.0, "Z": 0.8910065 }
    }
  }
}
```

- **Translation**: Tag center position in meters, in FRC field coordinates (origin at blue alliance corner)
- **Rotation**: Tag orientation as a quaternion in FRC coordinates (the tag's normal direction)

### Tag Size

The physical tag size (the black square of the tag36h11 family) is hardcoded:

```csharp
new AprilTagFieldLayout(0.1651)  // meters
```

This value is specific to FRC competition fields. If using different-sized tags, this constructor argument must be changed.

### 3D Corner Computation

`AprilTagFieldLayout.GetTagCorners()` computes the four 3D corner positions of each tag in field coordinates. From the tag center pose, corners are offset in the tag's local frame:

| Corner | Tag-local offset |
|--------|-----------------|
| Bottom-Right | `(0, -halfSize, -halfSize)` |
| Bottom-Left | `(0, +halfSize, -halfSize)` |
| Upper-Left | `(0, +halfSize, +halfSize)` |
| Upper-Right | `(0, -halfSize, +halfSize)` |

where `halfSize = tagSize / 2 = 0.08255m`.

Each offset is a `Transform3d` applied to the tag's field pose via `tagPose.Plus(cornerTransform)`, producing corners in FRC field coordinates.

:::warning
The corner ordering (BR, BL, UL, UR) is intentionally non-standard. The Meta Quest passthrough camera produces a mirrored image, causing the AprilTag detector to return 2D corners in clockwise order. The 3D corners must match this order for correct 2D-3D correspondences. **Do not reorder them to match the standard counter-clockwise AprilTag convention.**
:::

## Native Library Architecture

QuestNav uses three native C/C++ libraries accessed via P/Invoke (`[DllImport]`). All use `CallingConvention.Cdecl`.

### Library Overview

| Library | Source | Purpose | Platform Files |
|---------|--------|---------|----------------|
| **libapriltag** | University of Michigan | Tag detection in grayscale images | `libapriltag.so` (Quest), `apriltag.dll` (Editor) |
| **libposelib** | PoseLib project | Multi-tag PnP pose estimation | `libposelib.so` (Quest), `poselib.dll` (Editor) |
| **libntcore** | WPILib | NetworkTables 4 protocol | `libntcore.so` + deps (Quest), `ntcore.dll` + deps (Editor) |

### Interop Pattern

All native interop follows a consistent pattern:

1. **Native struct mirrors** with `[StructLayout(LayoutKind.Sequential)]` match C struct memory layout exactly
2. **`*Natives` classes** contain all `[DllImport]` declarations as static methods
3. **Managed wrappers** hold unsafe pointers to native memory and implement `IDisposable` for cleanup

### AprilTag Detection (libapriltag)

Key native functions:

| Function | Purpose |
|----------|---------|
| `apriltag_detector_create()` | Create a detector instance |
| `apriltag_detector_add_family_bits()` | Register the tag36h11 family |
| `apriltag_detector_detect()` | Run detection on a grayscale image |
| `tag36h11_create()` / `destroy()` | Tag family lifecycle |
| `image_u8_create()` / `destroy()` | Grayscale image buffer lifecycle |

The `zarray_get` and `zarray_size` functions from the C library are **re-implemented in C#** by directly reading native struct memory, because the C originals are `static inline` and cannot be P/Invoked.

### PoseLib Solver (libposelib)

A single function:

```
poselib_estimate_absolute_pose_simple(
    points2D[],       // Flat array of 2D image coordinates
    points3D[],       // Flat array of 3D field coordinates  
    numPoints,        // Number of correspondences
    cameraModelId,    // POSELIB_CAMERA_PINHOLE (0)
    cameraParams[],   // [fx, fy, cx, cy]
    imageWidth,
    imageHeight,
    maxReprojError,   // 12 pixels (hardcoded)
    &qw, &qx, &qy, &qz,  // Output quaternion
    &tx, &ty, &tz,         // Output translation
    &numInliers             // Output inlier count
)
```

**Camera intrinsics** come from `PassthroughCameraAccess.Intrinsics`:
- Focal length: `Intrinsics.FocalLength` (fx, fy)
- Principal point: `Intrinsics.PrincipalPoint` (cx, cy)
- Image dimensions: `Intrinsics.SensorResolution` (not `RequestedResolution` — they may differ if the capture resolution is downscaled)

### Memory Management

| Resource | Strategy |
|----------|----------|
| `ImageU8` | Static cached buffer; reallocated only on resolution change. Returned wrapper does not own the handle. |
| `AprilTagDetectionResults` | Wraps a `zarray_t*`. Freed when `Detect()` is called again (libapriltag overwrites previous results). Not explicitly disposed in the coroutine. |
| `AprilTagDetector` | `IDisposable`. Disposes all added families, clears, then destroys. |
| `AprilTagFamily` | `IDisposable` with a `Disposed` flag guard (safe for double-dispose). |

:::caution
`AprilTagDetectionResults` from `Detect()` is valid only until the next `Detect()` call. Do not hold references to detection objects across frames.
:::

## WPILib Geometry Library

QuestNav includes a C# port of WPILib's Java geometry classes in the `QuestNav.QuestNav.Geometry` namespace. These provide double-precision 3D math operations.

| Class | Description |
|-------|-------------|
| `Pose3d` | Translation3d + Rotation3d. JSON-serializable. Converts to/from protobuf. |
| `Rotation3d` | Quaternion-backed rotation. Constructs from matrix, axis-angle, or Euler angles. |
| `Translation3d` | 3D vector with norm, distance, and rotation operations. |
| `Transform3d` | Relative transform between two poses. |
| `Quaternion` | Double-precision quaternion. **Not** `UnityEngine.Quaternion`. |
| `Field2d` | Field dimensions container. |

:::warning Namespace collision
`QuestNav.QuestNav.Geometry.Quaternion` and `UnityEngine.Quaternion` are different types. The geometry quaternion uses double precision and `(W, X, Y, Z)` constructor order. Files that need both types add `using Quaternion = UnityEngine.Quaternion;` at the top. The double namespace (`QuestNav.QuestNav.Geometry`) is intentional — do not "fix" it.
:::
