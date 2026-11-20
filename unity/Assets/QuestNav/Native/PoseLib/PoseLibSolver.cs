using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Meta.XR;
using QuestNav.Camera;
using QuestNav.Native.AprilTag;
using QuestNav.QuestNav.AprilTag;
using QuestNav.QuestNav.Native.PoseLib;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Native.PoseLib
{
    public class PoseLibSolver
    {
        // [fx, fy, cx, cy] in pixels of the captured image, not of the sensor.
        private double[] intrinsicsArray;
        private Vector2Int imageResolution;
        private bool hasIntrinsics;
        private readonly AprilTagFieldLayout fieldLayout;

        public PoseLibSolver(AprilTagFieldLayout fieldLayout)
        {
            this.fieldLayout = fieldLayout;
        }

        /// <summary>
        /// Sets the intrinsics used for solving. Meta reports static sensor intrinsics, while
        /// detections are in pixels of the captured image, so they are converted to
        /// <paramref name="imageResolution"/>. Call again whenever the capture resolution changes.
        /// </summary>
        /// <returns>False if the intrinsics are not available yet; solving then fails until set.</returns>
        public bool SetIntrinsics(
            PassthroughCameraAccess.CameraIntrinsics sensorIntrinsics,
            Vector2Int imageResolution
        )
        {
            if (
                !ImageIntrinsics.TryFromSensor(
                    sensorIntrinsics.FocalLength,
                    sensorIntrinsics.PrincipalPoint,
                    sensorIntrinsics.SensorResolution,
                    imageResolution,
                    out var intrinsics
                )
            )
            {
                hasIntrinsics = false;
                return false;
            }

            intrinsicsArray = new[] { intrinsics.Fx, intrinsics.Fy, intrinsics.Cx, intrinsics.Cy };
            this.imageResolution = intrinsics.Resolution;
            hasIntrinsics = true;
            return true;
        }

        public PoseLibResult PoseLibSolve(AprilTagDetectionResults detections)
        {
            // Materialize the collection so the IReadOnlyList overload can iterate twice
            // (once to count and once to fill the buffers).
            var list = new List<AprilTagDetection>(detections.NumberOfDetections);
            foreach (var detection in detections)
            {
                list.Add(detection);
            }
            return PoseLibSolve(list);
        }

        /// <summary>
        /// Overload that accepts a pre-filtered list of detections. Used by
        /// <c>AprilTagManager.AprilTagFrameCaptureCoroutine</c> to drop ignored tag IDs
        /// before solving without allocating a fake <see cref="AprilTagDetectionResults"/>.
        /// </summary>
        public PoseLibResult PoseLibSolve(IReadOnlyList<AprilTagDetection> detections)
        {
            int count = detections?.Count ?? 0;
            if (count == 0)
            {
                return null;
            }
            if (!hasIntrinsics)
            {
                QueuedLogger.LogError("PoseLib solve skipped: camera intrinsics are not set");
                return null;
            }

            var corners2d = new List<double>(count * 8);
            var corners3d = new List<double>(count * 12);

            foreach (var detection in detections)
            {
                corners2d.Add(detection.CornerBottomRight0.x);
                corners2d.Add(detection.CornerBottomRight0.y);

                corners2d.Add(detection.CornerBottomLeft1.x);
                corners2d.Add(detection.CornerBottomLeft1.y);

                corners2d.Add(detection.CornerUpperLeft2.x);
                corners2d.Add(detection.CornerUpperLeft2.y);

                corners2d.Add(detection.CornerUpperRight3.x);
                corners2d.Add(detection.CornerUpperRight3.y);

                var corner3dTranslations = fieldLayout.GetTagCorners(detection.Id);
                foreach (var corner3d in corner3dTranslations)
                {
                    corners3d.Add(corner3d.X);
                    corners3d.Add(corner3d.Y);
                    corners3d.Add(corner3d.Z);
                }
            }

            // Native code reads numPoints entries from both buffers, so they must agree.
            int numPoints = corners2d.Count / 2;
            if (numPoints == 0 || corners3d.Count != numPoints * 3)
            {
                QueuedLogger.LogError(
                    $"PoseLib solve skipped: {corners2d.Count} 2D values do not match "
                        + $"{corners3d.Count} 3D values"
                );
                return null;
            }

            // 12 px RANSAC reprojection-error threshold was tuned for the Quest 3 / 3S
            // 1280-wide passthrough frames. (An earlier attempt to scale this with
            // colors.Length was based on a misread of the Meta SDK's GetColors over-
            // allocation; see AprilTagManager comment in AprilTagFrameCaptureCoroutine.)
            int status = PoseLibNatives.poselib_estimate_absolute_pose_simple(
                corners2d.ToArray(),
                corners3d.ToArray(),
                (ulong)numPoints,
                (int)PoseLibNatives.PoseLibCameraModelIdNative.POSELIB_CAMERA_PINHOLE,
                imageResolution.x,
                imageResolution.y,
                intrinsicsArray,
                4,
                12,
                out var resultPose,
                out ulong resultInliers
            );

            if (status == 0)
            {
                return new PoseLibResult(resultPose, resultInliers, numPoints);
            }

            QueuedLogger.LogError($"PoseLib solve failed! Error code: {status}");
            return null;
        }
    }
}
