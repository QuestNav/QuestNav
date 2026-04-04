using System;
using System.Collections;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Meta.XR;
using QuestNav.Config;
using QuestNav.Native.AprilTag;
using QuestNav.Native.PoseLib;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using Unity.Collections;
using UnityEngine;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagManager
    {
        /// <summary>
        /// Main thread synchronization context for marshalling Unity API calls.
        /// </summary>
        private readonly SynchronizationContext mainThreadContext;

        private readonly AprilTagFieldLayout aprilTagFieldLayout;
        private readonly IVioAprilTagPoseEstimator vioAprilTagPoseEstimator;
        private readonly PassthroughCameraAccess cameraAccess;
        private readonly IConfigManager configManager;
        private AprilTagDetector aprilTagDetector;
        private readonly AprilTagFamily aprilTagFamily = new Tag36h11();
        private readonly PoseLibSolver poseLibSolver;
        private readonly MonoBehaviour coroutineHost;
        private Coroutine captureCoroutine;
        private float frameDelaySeconds;
        private int[] allowedIds;
        private double maxDistance;
        private int minimumNumberOfTags;
        private readonly Matrix<double> aprilTagStdBase;

        public AprilTagManager(
            IConfigManager configManager,
            IVioAprilTagPoseEstimator vioAprilTagPoseEstimator,
            PassthroughCameraAccess cameraAccess,
            AprilTagFieldLayout aprilTagFieldLayout,
            MonoBehaviour coroutineHost
        )
        {
            // Capture main thread context for marshalling Unity API calls
            mainThreadContext = SynchronizationContext.Current;

            this.cameraAccess = cameraAccess;
            this.vioAprilTagPoseEstimator = vioAprilTagPoseEstimator;
            this.configManager = configManager;
            this.aprilTagFieldLayout = aprilTagFieldLayout;
            this.coroutineHost = coroutineHost;
            poseLibSolver = new PoseLibSolver(aprilTagFieldLayout, cameraAccess.Intrinsics);
            aprilTagStdBase = DenseMatrix.OfArray(
                new[,]
                {
                    { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[0] },
                    { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[1] },
                    { VioAprilTagPoseEstimatorConstants.defaultAprilTagStdDevs[2] },
                }
            );

            configManager.OnEnableAprilTagDetectorChanged += OnEnableAprilTagDetectorChanged;
            configManager.OnAprilTagDetectorModeChanged += OnAprilTagDetectorModeChanged;

            QueuedLogger.Log("Initialized AprilTagManager");
        }

        private void OnEnableAprilTagDetectorChanged(bool enable)
        {
            if (enable)
            {
                cameraAccess.enabled = true;
                aprilTagDetector = new AprilTagDetector();
                aprilTagDetector.AddFamily(aprilTagFamily);
                captureCoroutine = coroutineHost.StartCoroutine(AprilTagFrameCaptureCoroutine());
                QueuedLogger.Log("AprilTagDetector Enabled");
            }
            else
            {
                if (captureCoroutine != null)
                {
                    coroutineHost.StopCoroutine(captureCoroutine);
                    captureCoroutine = null;
                }
                cameraAccess.enabled = false;
                aprilTagDetector.Dispose();
                aprilTagFamily.Dispose();
                QueuedLogger.Log("AprilTagDetector Disabled");
            }
        }

        private void OnAprilTagDetectorModeChanged(Config.Config.AprilTagDetectorMode configuration)
        {
            if (
                configuration.Mode
                == Config.Config.AprilTagDetectorMode.DetectionMode.ANCHOR_ENHANCED
            )
            {
                throw new NotImplementedException(
                    "ANCHOR_ENHANCED has not been implemented yet! Please check for a later release."
                );
            }

            InvokeOnMainThread(() =>
            {
                cameraAccess.enabled = false;
                cameraAccess.RequestedResolution = new Vector2Int(
                    configuration.Width,
                    configuration.Height
                );
                cameraAccess.enabled = true;
            });

            frameDelaySeconds = 1.0f / Math.Max(1, configuration.Framerate);
            allowedIds = configuration.AllowedIds;
            maxDistance = configuration.MaxDistance;
            minimumNumberOfTags = configuration.MinimumNumberOfTags;
        }

        //TODO: Fix crash when enabling/disabling detector
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity PassthroughCameraAccess is not enabled. Please enable the component before accessing this API.
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity UnityEngine.DebugLogHandler:LogFormat(LogType, Object, String, Object[])
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity UnityEngine.Logger:Log(LogType, Object, Object)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity UnityEngine.Debug:LogError(Object, Object)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity Meta.XR.PassthroughCameraAccess:ValidateIsEnabled() (at .\Library\PackageCache\com.meta.xr.mrutilitykit@7a74cdb228ff\Core\Scripts\PassthroughCameraAccess.cs:145)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity Meta.XR.PassthroughCameraAccess:GetTexture() (at .\Library\PackageCache\com.meta.xr.mrutilitykit@7a74cdb228ff\Core\Scripts\PassthroughCameraAccess.cs:127)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity QuestNav.Camera.<FrameCaptureCoroutine>d__38:MoveNext() (at C:\Users\sernstes\Documents\Code\QuestNav\unity\Assets\QuestNav\Camera\PassthroughFrameSource.cs:571)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity UnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr) (at \home\bokken\build\output\unity\unity\Runtime\Export\Scripting\Coroutines.cs:17)
        // 2026/04/04 01:36:20.408 19555 19589 Error Unity
        // 2026/04/04 01:36:20.419 19555 19589 Error Unity NullReferenceException: Object reference not set to an instance of an object.
        // 2026/04/04 01:36:20.419 19555 19589 Error Unity   at QuestNav.Camera.PassthroughFrameSource+<FrameCaptureCoroutine>d__38.MoveNext () [0x001b0] in C:\Users\sernstes\Documents\Code\QuestNav\unity\Assets\QuestNav\Camera\PassthroughFrameSource.cs:584
        // 2026/04/04 01:36:20.419 19555 19589 Error Unity   at UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) [0x00027] in \home\bokken\build\output\unity\unity\Runtime\Export\Scripting\Coroutines.cs:17

        private IEnumerator AprilTagFrameCaptureCoroutine()
        {
            while (true)
            {
                NativeArray<Color32> colors;

                try
                {
                    colors = cameraAccess.GetColors();
                }
                catch (NullReferenceException ex)
                {
                    QueuedLogger.LogError(
                        $"Error capturing frame - verify 'Headset Cameras' app permission is enabled. {ex.Message}"
                    );
                    yield break;
                }

                var converted = ImageU8.FromPassthroughCamera(
                    colors,
                    cameraAccess.RequestedResolution.x,
                    cameraAccess.RequestedResolution.y
                );
                var results = aprilTagDetector.Detect(converted);

                if (results.NumberOfDetections >= 2)
                {
                    QueuedLogger.Log("2+ Tags Detected");

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

                    var (frcPos, frcRot) = Conversions.CvToFrc(poseLibResult.CameraPose);

                    vioAprilTagPoseEstimator.AddAprilTagObservation(
                        new Translation3d(frcPos.x, frcPos.y, frcPos.z),
                        cameraAccess.Timestamp.Second,
                        aprilTagStdBase
                    );

                    QueuedLogger.Log(
                        $"Received new PoseLib estimate: Pos({frcPos.x:F3}, {frcPos.y:F3}, {frcPos.z:F3}) Rot({frcRot.eulerAngles.x:F3}, {frcRot.eulerAngles.y:F3}, {frcRot.eulerAngles.z})"
                    );
                }

                yield return new WaitForSeconds(frameDelaySeconds);
            }
        }

        /// <summary>
        /// Invokes an action on the main thread using the captured SynchronizationContext.
        /// Falls back to direct invocation if no context was captured.
        /// </summary>
        private void InvokeOnMainThread(Action action)
        {
            if (mainThreadContext == null)
            {
                action();
            }
            else
            {
                mainThreadContext.Post(_ => action(), null);
            }
        }
    }
}
