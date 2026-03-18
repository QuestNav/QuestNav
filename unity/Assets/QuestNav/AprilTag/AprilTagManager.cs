using System;
using System.Collections;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Meta.XR;
using QuestNav.Config;
using QuestNav.Native.AprilTag;
using QuestNav.Native.PoseLib;
using QuestNav.QuestNav.Estimation;
using QuestNav.Utils;
using Unity.Collections;
using UnityEngine;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagManager
    {
        private readonly AprilTagFieldLayout aprilTagFieldLayout;
        private readonly IVioAprilTagPoseEstimator vioAprilTagPoseEstimator;
        private readonly PassthroughCameraAccess cameraAccess;
        private readonly IConfigManager configManager;
        private AprilTagDetector aprilTagDetector;
        private readonly AprilTagFamily aprilTagFamily = new Tag36h11();
        private readonly PoseLibSolver poseLibSolver;
        private readonly MonoBehaviour coroutineHost;
        private float frameDelaySeconds;
        private int[] allowedIds;
        private double maxDistance;
        private int minimumNumberOfTags;
        private readonly Matrix<double> aprilTagStdBase;
        
        //TODO: use interface here when passing into QuestNav.cs
        public AprilTagManager(IConfigManager configManager, IVioAprilTagPoseEstimator vioAprilTagPoseEstimator, PassthroughCameraAccess cameraAccess, AprilTagFieldLayout aprilTagFieldLayout, MonoBehaviour coroutineHost)
        {
            this.cameraAccess = cameraAccess;
            this.vioAprilTagPoseEstimator = vioAprilTagPoseEstimator;
            this.configManager = configManager;
            this.aprilTagFieldLayout = aprilTagFieldLayout;
            this.coroutineHost = coroutineHost;
            poseLibSolver = new PoseLibSolver(aprilTagFieldLayout, cameraAccess.Intrinsics);
            aprilTagStdBase = DenseMatrix.OfArray(new[,]
            {
                { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[0] },
                { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[1] },
                { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[2] },
            });
            
            configManager.OnEnableAprilTagDetectorChanged += OnEnableAprilTagDetectorChanged;
            configManager.OnAprilTagDetectorModeChanged += OnAprilTagDetectorModeChanged;
        }

        private void OnEnableAprilTagDetectorChanged(bool enable)
        {
            if (enable)
            {
                cameraAccess.enabled = true;
                aprilTagDetector = new AprilTagDetector();
                aprilTagDetector.AddFamily(aprilTagFamily);
                coroutineHost.StartCoroutine(FrameCaptureCoroutine());
            }
            else
            {
                coroutineHost.StopCoroutine(FrameCaptureCoroutine());
                cameraAccess.enabled = false;
                aprilTagDetector.Dispose();
                aprilTagFamily.Dispose();
            }
        }

        private void OnAprilTagDetectorModeChanged(Config.Config.AprilTagDetectorMode configuration)
        {
            if (configuration.Mode == Config.Config.AprilTagDetectorMode.DetectionMode.ANCHOR_ENHANCED)
            {
                throw new NotImplementedException("ANCHOR_ENHANCED has not been implemented yet! Please check for a later release.");
            }
            
            cameraAccess.RequestedResolution = new Vector2Int(
                configuration.Width,
                configuration.Height
            );

            frameDelaySeconds = 1.0f / Math.Max(1, configuration.Framerate);
            allowedIds = configuration.AllowedIds;
            maxDistance = configuration.MaxDistance;
            minimumNumberOfTags = configuration.MinimumNumberOfTags;
        }

        private IEnumerator FrameCaptureCoroutine()
        {
            NativeArray<Color32> colors;
            
            // Capture image every x frames
            try
            {
                colors = cameraAccess.GetColors();
            }
            catch (NullReferenceException ex)
            {
                // This probably means the app hasn't been given permission to access the headset camera.
                QueuedLogger.LogError(
                    $"Error capturing frame - verify 'Headset Cameras' app permission is enabled. {ex.Message}"
                );
                yield break;
            }
            // Convert image
            var converted= ImageU8.FromPassthroughCamera(colors, cameraAccess.RequestedResolution.x, cameraAccess.RequestedResolution.y);
            
            // Detect tags
            var results = aprilTagDetector.Detect(converted);
            
            
            if (results.NumberOfDetections >= 2)
            {
                // Pass into solver
                var poseLibResult = poseLibSolver.PoseLibSolve(results);

                
                //TODO: filter tags based on requirements (distance, id, etc) Dynamic STD filtering is a MUST
                // Calculate dynamic standard deviations
                // Calculate standard deviation based on tag distance and count
                // Uncertainty increases with distance squared and decreases with more tags
                /* Code from my java repo:
                double stdDevFactor = Math.Pow(observation.averageTagDistance(), 2.0) / results.NumberOfDetections;
                double linearStdDev = MULTI_TAG_LINEAR_STD_DEV_BASE * stdDevFactor;
                double angularStdDev = MULTI_TAG_ANGULAR_STD_DEV_BASE * stdDevFactor;

                // Apply camera-specific adjustment factor if available
                if (cameraIndex < CAMERA_STD_DEV_FACTORS.length)
                {
                    linearStdDev *= CAMERA_STD_DEV_FACTORS[cameraIndex];
                    angularStdDev *= CAMERA_STD_DEV_FACTORS[cameraIndex];
                }
                */

                vioAprilTagPoseEstimator.AddAprilTagObservation(poseLibResult.CameraPose.Translation,
                    cameraAccess.Timestamp.Second, aprilTagStdBase);
                
                //DEBUG:
                QueuedLogger.Log($"Received new PoseLib estimate: {poseLibResult.CameraPose}");
            }
            yield return new WaitForSeconds(frameDelaySeconds);
        }
    }
}
