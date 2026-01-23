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
        private readonly double[] intrinsicsArray;
        private readonly int resolutionX;
        private readonly int resolutionY;
        private readonly AprilTagFieldLayout fieldLayout;
        
        public PoseLibSolver(AprilTagFieldLayout fieldLayout, PassthroughCameraAccess.CameraIntrinsics intrinsics)
        {
            this.fieldLayout = fieldLayout;
            
            intrinsicsArray = new double[]
            {
                intrinsics.FocalLength.x,
                intrinsics.FocalLength.y,
                intrinsics.PrincipalPoint.x,
                intrinsics.PrincipalPoint.y,
            };

            resolutionX = intrinsics.SensorResolution.x;
            resolutionY = intrinsics.SensorResolution.y;
        }
        
        public PoseLibResult PoseLibSolve(
            AprilTagDetectionResults detections
        )
        {
            var corners2d = new List<double>();
            var corners3d = new List<double>();

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
             

            int status = PoseLibNatives.poselib_estimate_absolute_pose_simple(
                corners2d.ToArray(),
                corners3d.ToArray(),
                (ulong) (detections.NumberOfDetections * 4),
                (int)PoseLibNatives.PoseLibCameraModelIdNative.POSELIB_CAMERA_PINHOLE,
                resolutionX,
                resolutionY,
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
