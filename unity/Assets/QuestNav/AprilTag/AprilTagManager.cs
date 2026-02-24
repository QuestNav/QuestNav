using System;
using System.Collections;
using Meta.XR;
using QuestNav.Config;
using QuestNav.Native.AprilTag;
using QuestNav.Native.PoseLib;
using QuestNav.Utils;
using Unity.Collections;
using UnityEngine;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagManager
    {
        private readonly AprilTagFieldLayout aprilTagFieldLayout;
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
        
        //TODO: use interface here when passing into QuestNav.cs
        public AprilTagManager(IConfigManager configManager, PassthroughCameraAccess cameraAccess, AprilTagFieldLayout aprilTagFieldLayout, MonoBehaviour coroutineHost)
        {
            this.cameraAccess = cameraAccess;
            this.configManager = configManager;
            this.aprilTagFieldLayout = aprilTagFieldLayout;
            this.coroutineHost = coroutineHost;
            poseLibSolver = new PoseLibSolver(aprilTagFieldLayout, cameraAccess.Intrinsics);
            
            configManager.OnEnableAprilTagDetectorChanged += OnEnableAprilTagDetectorChanged;
            configManager.OnAprilTagDetectorModeChanged += Configure;
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

        private void Configure(Config.Config.AprilTagDetectorMode configuration)
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
            
            //TODO: filter tags based on requirements (distance, id, etc)
            
            // Pass into solver
            var poseLibResult = poseLibSolver.PoseLibSolve(results);
            
            //TODO: fuse pose using kalman filter
            
            //DEBUG:
            QueuedLogger.Log($"Received new PoseLib estimate: {poseLibResult.CameraPose}");
            
            yield return new WaitForSeconds(frameDelaySeconds);
        }
    }
}
