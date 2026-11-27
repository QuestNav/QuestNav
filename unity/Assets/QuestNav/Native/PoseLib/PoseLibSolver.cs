using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Meta.XR;
using QuestNav.Camera;
using QuestNav.Native.AprilTag;
using QuestNav.QuestNav.AprilTag;
using QuestNav.QuestNav.Geometry;
using QuestNav.QuestNav.Native.PoseLib;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Native.PoseLib
{
    public class PoseLibSolver
    {
        // RANSAC reprojection-error threshold in pixels, tuned for the Quest 3 / 3S 1280-wide
        // passthrough frames. (An earlier attempt to scale this with colors.Length was based on
        // a misread of the Meta SDK's GetColors over-allocation; see AprilTagManager comment in
        // AprilTagFrameCaptureCoroutine.)
        public const double MAX_REPROJECTION_ERROR_PX = 12.0;

        /// <summary>Corners of a tag that must fit the solved pose for the tag to count.</summary>
        public const int MIN_INLIER_CORNERS_PER_TAG = 3;

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
            var tagIds = new List<int>(count);

            foreach (var detection in detections)
            {
                tagIds.Add(detection.Id);
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

            int status = PoseLibNatives.poselib_estimate_absolute_pose_simple(
                corners2d.ToArray(),
                corners3d.ToArray(),
                (ulong)numPoints,
                (int)PoseLibNatives.PoseLibCameraModelIdNative.POSELIB_CAMERA_PINHOLE,
                imageResolution.x,
                imageResolution.y,
                intrinsicsArray,
                4,
                MAX_REPROJECTION_ERROR_PX,
                out var resultPose,
                out ulong resultInliers
            );

            if (status == 0)
            {
                var result = new PoseLibResult(resultPose, resultInliers, numPoints);
                result.SupportingTags = CountSupportingTags(
                    tagIds,
                    corners2d,
                    corners3d,
                    result.CameraPose,
                    intrinsicsArray,
                    MAX_REPROJECTION_ERROR_PX
                );
                return result;
            }

            QueuedLogger.LogError($"PoseLib solve failed! Error code: {status}");
            return null;
        }

        /// <summary>
        /// Counts distinct tag IDs with at least <see cref="MIN_INLIER_CORNERS_PER_TAG"/>
        /// corners that reproject within <paramref name="maxReprojectionError"/> px of their
        /// detection under the solved pose. A tag whose corners mostly miss the solve did not
        /// constrain it, so it does not count toward the two-tag floor.
        /// </summary>
        /// <param name="tagIds">Tag ID of each detection, four corners each</param>
        /// <param name="corners2d">Detected corners in pixels [u0, v0, u1, v1, ...]</param>
        /// <param name="corners3d">Field corners [x0, y0, z0, ...] in the same order</param>
        /// <param name="worldToCamera">Solved field-to-CV-camera transform (x_cam = R * X + t)</param>
        /// <param name="intrinsics">[fx, fy, cx, cy] in pixels</param>
        /// <param name="maxReprojectionError">Inlier threshold in pixels</param>
        public static int CountSupportingTags(
            IReadOnlyList<int> tagIds,
            IReadOnlyList<double> corners2d,
            IReadOnlyList<double> corners3d,
            Pose3d worldToCamera,
            IReadOnlyList<double> intrinsics,
            double maxReprojectionError
        )
        {
            double maxSquared = maxReprojectionError * maxReprojectionError;
            var supporting = new HashSet<int>();
            for (int tag = 0; tag < tagIds.Count; tag++)
            {
                int inliers = 0;
                for (int corner = 0; corner < 4; corner++)
                {
                    int i = tag * 4 + corner;
                    var cam =
                        new Translation3d(
                            corners3d[3 * i],
                            corners3d[3 * i + 1],
                            corners3d[3 * i + 2]
                        ).RotateBy(worldToCamera.Rotation) + worldToCamera.Translation;
                    if (cam.Z <= 0)
                        continue;

                    double du = intrinsics[0] * cam.X / cam.Z + intrinsics[2] - corners2d[2 * i];
                    double dv =
                        intrinsics[1] * cam.Y / cam.Z + intrinsics[3] - corners2d[2 * i + 1];
                    if (du * du + dv * dv < maxSquared)
                        inliers++;
                }
                if (inliers >= MIN_INLIER_CORNERS_PER_TAG)
                    supporting.Add(tagIds[tag]);
            }
            return supporting.Count;
        }
    }
}
