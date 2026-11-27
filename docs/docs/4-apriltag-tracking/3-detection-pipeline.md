---
title: Detection Pipeline
---

# Detection Pipeline

This page describes the steps from a camera frame to a field-relative headset pose measurement
(`unity/Assets/QuestNav/AprilTag/AprilTagManager.cs`).

The app has one detector mode, **Traditional** (mode `0`). The configuration has an `ANCHOR_ENHANCED` value, but it
is not implemented: the web API rejects it, and a saved value falls back to Traditional at startup.

## Pipeline Overview

```mermaid
flowchart TD
    A["Passthrough Camera"] -->|"NativeArray Color32"| B["Grayscale Conversion"]
    B -->|"image_u8_t"| C["AprilTag Detection"]
    C -->|"2D corner positions"| C2["Drop ignored IDs and IDs not in the layout"]
    C2 --> D{"At least Minimum Tags?"}
    D -->|No| Wait["Wait for next frame"]
    D -->|Yes| E["PoseLib PnP Solver"]
    E -->|"Camera pose in CV space"| F["CvToFrc + lens offset"]
    F -->|"Headset pose in FRC space"| G{"Average tag distance <= Max Tag Distance?"}
    G -->|No| Wait
    G -->|Yes| H["Phase 1 / Phase 2 gates"]
    H -->|Accepted| I["Kalman Filter Update"]
    H -->|Rejected| Wait
    Wait --> A
```

## Step 1: Frame Capture

The detection loop is a Unity coroutine (`AprilTagFrameCaptureCoroutine`). It waits `1 / framerate` seconds
between frames (30 fps by default) and skips iterations where the camera has not delivered a new frame, so it never
runs faster than the camera. Each cycle:

1. Builds the **capture timestamp** in the app's `Time.timeAsDouble` clock from the camera's exposure time:
   `now - (UtcNow - cameraAccess.Timestamp)`, with the age clamped to 0 to 0.5 s. The Kalman filter fuses the
   measurement against the VIO state at that time.
2. Calls `PassthroughCameraAccess.GetColors()` to get a `NativeArray<Color32>` (RGBA32) frame.

The camera resolution comes from the AprilTag tab. While the detector runs it has priority over the passthrough
stream, which then uses the same resolution.

:::note
The **Headset Cameras** permission must be granted on the Quest, or no frames are captured.
:::

## Step 2: Grayscale Conversion

The AprilTag detector requires a single-channel grayscale image. `ImageU8.FromPassthroughCamera()` converts the RGBA32 frame using a Burst-compiled parallel job (`GrayscaleConversionJob`) that processes rows in parallel for performance.

The output is a native `image_u8_t` buffer. A static cache is reused across frames — the buffer is only reallocated when the resolution changes.

## Step 3: AprilTag Detection

The grayscale image is passed to the native libapriltag library via `AprilTagDetector.Detect()`. The detector:

1. Searches for tag36h11 fiducial markers in the image
2. Returns an `AprilTagDetectionResults` collection containing, for each detected tag:
   - **Tag ID** — the numeric identifier
   - **Four corner positions** in image pixel coordinates (2D)
   - **Center position** in pixel coordinates
   - **Hamming distance** — error correction metric

:::info
The Meta Quest passthrough camera produces a **mirrored image**. This causes the AprilTag detector to return corners in clockwise order (Bottom-Right, Bottom-Left, Upper-Left, Upper-Right) rather than the standard counter-clockwise convention. The 3D corner positions are intentionally ordered to match this CW convention.
:::

## Step 4: Filtering

Two kinds of detection are dropped before anything else:

1. IDs in the **Ignored Tag IDs** list
2. IDs that are not in the loaded field layout, such as false decodes that would otherwise spoil the solve

The frame is used only if at least **Minimum Tags Required** (1 to 4, default 2) detections remain.

## Step 5: Multi-Tag PnP Solve

`PoseLibSolver.PoseLibSolve()` estimates the camera's 3D pose from every remaining tag at once. This is a Perspective-n-Point (PnP) problem: given 2D image points and their known 3D field positions, find the camera pose.

### Building Correspondences

For each detected tag, the solver collects:

- **2D points**: The four corner positions from the detector (in pixel coordinates)
- **3D points**: The four corner positions from `AprilTagFieldLayout.GetTagCorners()` (in FRC field coordinates, meters)

The 3D corners are computed by applying tag-local offsets (±halfSize in Y and Z) to the tag's field pose from the JSON layout file. In the tag's local frame:
- X = 0 (the tag face plane)
- Y = ±halfSize (left/right on the tag)
- Z = ±halfSize (up/down on the tag)

These are transformed into field coordinates via `tagPose.Plus(cornerTransform)`.

### PoseLib Solver

All 2D-3D correspondences are flattened into arrays and passed to the native PoseLib function `poselib_estimate_absolute_pose_simple()` with:

- **Camera model**: Pinhole (fx, fy, cx, cy from Meta's SDK)
- **Max reprojection error**: 12 pixels
- **Output**: Camera pose as quaternion (W, X, Y, Z) + translation (X, Y, Z), plus an inlier count

PoseLib uses RANSAC internally, meaning it can tolerate some bad correspondences (e.g., from partially occluded tags). The **inlier count** indicates how many of the input point correspondences were consistent with the estimated pose.

### Output Convention

PoseLib returns a **world-to-camera** transform following the COLMAP convention:

```
P_camera = R * P_world + t
```

The "world" here is whatever coordinate system the 3D points are in — which is **FRC field coordinates**. The camera frame uses CV conventions (X-right, Y-down, Z-forward).

Camera intrinsics are converted from sensor pixels to the captured frame's resolution, and recomputed when the
resolution changes.

## Step 6: CV to FRC Conversion

`Conversions.CvToFrc()` converts PoseLib's world-to-camera output into the camera's position and orientation in FRC field coordinates.

### Position

The camera's position in the field is computed by inverting the world-to-camera transform:

```
C_field = -(R_inv * t)
```

This gives the camera position in FRC field coordinates (meters).

### Orientation

The rotation is converted from CV camera axes to FRC body axes (X-forward, Y-left, Z-up) by composing the inverse rotation with a fixed body rotation:

```
q_FRC = R_inv * q_cv_to_body
```

where `q_cv_to_body = (w=0.5, x=-0.5, y=0.5, z=0.5)`.

See the [Coordinate Systems](./2-coordinate-systems.md) page for the full derivation.

### Lens offset

The pose above is the camera lens's. It is moved to the headset center with the lens offset the Meta SDK reports
(`camToHeadset`), applied in the camera's own frame, so the measurement matches the pose VIO tracks.

## Step 7: Confidence Metrics

Before the observation reaches the Kalman filter, several confidence metrics are computed:

| Metric | Formula | Purpose |
|--------|---------|---------|
| **Inlier ratio** | `AcceptedPoints / TotalPoints` | Fraction of correspondences that PoseLib's RANSAC accepted. Higher = more reliable. |
| **Tag count** | Detections left after filtering | More tags provide stronger geometric constraints. |
| **Average tag distance** | Mean distance from the headset to each tag used | Distant tags are less precise. A frame above **Max Tag Distance** (default 4 m) is dropped here. |
| **Dynamic std dev** | `0.05 × noiseScale × avgDist² / tagCount` | Measurement noise for the Kalman filter. Scales with distance, inversely with tag count. `noiseScale` is the **AprilTag Trust** setting (0.5 to 2.0). |

The dynamic standard deviation controls how much the Kalman filter trusts each observation:
- **Close + many tags** → low std dev → strong correction
- **Far + few tags** → high std dev → gentle nudge

The Z-axis standard deviation is doubled relative to X and Y, reflecting the lower vertical precision of the PnP solution.

## Step 8: Confidence Gating

The observation is evaluated against phase-dependent thresholds before it can update the Kalman filter. See the [Kalman Filter](./4-kalman-filter.md) page for the full two-phase gating logic.

## Timing

The entire pipeline (capture → detect → solve → convert → gate) runs within a single coroutine iteration. At the default 30 fps the headset gets up to 30 correction opportunities per second when tags are visible.

Between AprilTag corrections, the Kalman filter continues to track position using VIO displacement data at 120 Hz, providing smooth dead-reckoning between tag sightings.
