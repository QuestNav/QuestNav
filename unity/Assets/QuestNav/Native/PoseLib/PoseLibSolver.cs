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
        public PoseLibResult PoseLibSolve(
            AprilTagDetectionResults detections,
            AprilTagFieldLayout fieldLayout,
            PassthroughFrameSource passthroughFrameSource
        )
        {
            var corners2d = new List<double>();
            var corners3d = new List<double>();

            foreach (var detection in detections)
            {
                corners2d.Add(detection.CornerBottomRight1.x);
                corners2d.Add(detection.CornerBottomRight1.y);

                corners2d.Add(detection.CornerBottomLeft0.x);
                corners2d.Add(detection.CornerBottomLeft0.y);

                corners2d.Add(detection.CornerUpperLeft3.x);
                corners2d.Add(detection.CornerUpperLeft3.y);

                corners2d.Add(detection.CornerUpperRight2.x);
                corners2d.Add(detection.CornerUpperRight2.y);

                var corner3dTranslations = fieldLayout.GetTagCorners(detection.Id);
                foreach (var corner3d in corner3dTranslations)
                {
                    corners3d.Add(corner3d.X);
                    corners3d.Add(corner3d.Y);
                    corners3d.Add(corner3d.Z);
                }
            }
            var pcaIntrinsics = passthroughFrameSource.cameraAccess.Intrinsics;
            double[] intrinsicsArray = new double[]
            {
                pcaIntrinsics.FocalLength.x,
                pcaIntrinsics.FocalLength.y,
                pcaIntrinsics.PrincipalPoint.x,
                pcaIntrinsics.PrincipalPoint.y,
            };

            int status = PoseLibNatives.poselib_estimate_absolute_pose_simple(
                corners2d.ToArray(),
                corners3d.ToArray(),
                (ulong) (detections.NumberOfDetections * 4),
                (int)PoseLibNatives.PoseLibCameraModelIdNative.POSELIB_CAMERA_PINHOLE,
                passthroughFrameSource.cameraAccess.CurrentResolution.x,
                passthroughFrameSource.cameraAccess.CurrentResolution.y,
                intrinsicsArray,
                4,
                12,
                out var resultPose,
                out ulong resultInliers
            );

            if (status == 0)
            {
                return new PoseLibResult(resultPose, resultInliers);
            }

            QueuedLogger.LogError($"PoseLib solve failed! Error code: {status}");
            return null;
        }
    }
}
