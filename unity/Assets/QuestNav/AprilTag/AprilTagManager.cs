using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Meta.XR;
using QuestNav.Camera;
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
        /// Stable identifier used by AprilTagManager with the camera arbiter.
        /// </summary>
        private const string CameraArbiterRequesterId = "AprilTagManager";

        /// <summary>
        /// Main thread synchronization context for marshalling Unity API calls.
        /// </summary>
        private readonly SynchronizationContext mainThreadContext;

        private readonly AprilTagFieldLayout aprilTagFieldLayout;
        private readonly IVioAprilTagPoseEstimator vioAprilTagPoseEstimator;
        private readonly PassthroughCameraAccess cameraAccess;
        private readonly CameraResourceManager cameraArbiter;
        private readonly IConfigManager configManager;
        private AprilTagDetector aprilTagDetector;
        private readonly AprilTagFamily aprilTagFamily = new Tag36h11();
        private readonly PoseLibSolver poseLibSolver;
        private readonly MonoBehaviour coroutineHost;
        private Coroutine captureCoroutine;
        private float frameDelaySeconds;

        /// <summary>
        /// Set of tag IDs to drop from each frame's detections (blacklist). Empty = detect every tag.
        /// Rebuilt from <see cref="Config.AprilTagDetectorMode.IgnoredIds"/> on every config change.
        /// </summary>
        private HashSet<int> ignoredIdSet = new HashSet<int>();
        private double maxDistance;
        private int minimumNumberOfTags;

        /// <summary>
        /// Most recently requested camera resolution. Used to skip arbiter re-reservations
        /// when only the framerate changed (the camera does not need to be bounced).
        /// </summary>
        private Vector2Int currentResolution;

        public AprilTagManager(
            IConfigManager configManager,
            IVioAprilTagPoseEstimator vioAprilTagPoseEstimator,
            PassthroughCameraAccess cameraAccess,
            CameraResourceManager cameraArbiter,
            AprilTagFieldLayout aprilTagFieldLayout,
            MonoBehaviour coroutineHost
        )
        {
            // Capture main thread context for marshalling Unity API calls
            mainThreadContext = SynchronizationContext.Current;

            this.cameraAccess = cameraAccess;
            this.cameraArbiter = cameraArbiter;
            this.vioAprilTagPoseEstimator = vioAprilTagPoseEstimator;
            this.configManager = configManager;
            this.aprilTagFieldLayout = aprilTagFieldLayout;
            this.coroutineHost = coroutineHost;
            poseLibSolver = new PoseLibSolver(aprilTagFieldLayout, cameraAccess.Intrinsics);

            configManager.OnEnableAprilTagDetectorChanged += OnEnableAprilTagDetectorChanged;
            configManager.OnAprilTagDetectorModeChanged += OnAprilTagDetectorModeChanged;

            QueuedLogger.Log("Initialized AprilTagManager");
        }

        private void OnEnableAprilTagDetectorChanged(bool enable)
        {
            if (enable)
            {
                // Reserve the camera at high priority. The arbiter will downgrade the
                // passthrough stream's reservation if it conflicts.
                cameraArbiter.Reserve(
                    CameraArbiterRequesterId,
                    currentResolution,
                    CameraRequestPriority.High
                );
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
                cameraArbiter.Release(CameraArbiterRequesterId);
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

            // Cache the requested resolution so OnEnableAprilTagDetectorChanged(true)
            // can re-reserve at the right size, and so we can detect FPS-only changes
            // below to skip an unnecessary camera bounce.
            var requested = new Vector2Int(configuration.Width, configuration.Height);
            bool resolutionChanged = requested != currentResolution;
            currentResolution = requested;

            // Only ask the arbiter to re-apply when we are currently the active high-priority
            // requester AND the resolution actually changed. A pure FPS change does not need
            // to bounce the camera (the framerate is enforced by the coroutine's WaitForSeconds).
            if (resolutionChanged && captureCoroutine != null)
            {
                cameraArbiter.Reserve(
                    CameraArbiterRequesterId,
                    requested,
                    CameraRequestPriority.High
                );
            }

            frameDelaySeconds = 1.0f / Math.Max(1, configuration.Framerate);
            // Rebuild the ignore set on each config change. The capture coroutine reads
            // ignoredIdSet on every frame; replacing the reference atomically is safe
            // because both reads and writes happen on the Unity main thread (the coroutine
            // and this event handler are both main-thread).
            ignoredIdSet =
                configuration.IgnoredIds == null
                    ? new HashSet<int>()
                    : new HashSet<int>(configuration.IgnoredIds);
            maxDistance = configuration.MaxDistance;
            minimumNumberOfTags = Math.Max(1, configuration.MinimumNumberOfTags);
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
                float captureTimestamp = Time.time;
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

                // Apply the ignored-IDs blacklist before counting / solving. Empty set
                // keeps every detection. Building the kept list in-line avoids allocating
                // an AprilTagDetectionResults clone.
                var kept = new List<AprilTagDetection>(results.NumberOfDetections);
                foreach (var detection in results)
                {
                    if (!ignoredIdSet.Contains(detection.Id))
                    {
                        kept.Add(detection);
                    }
                }

                if (kept.Count >= minimumNumberOfTags)
                {
                    QueuedLogger.Log(
                        $"{kept.Count} usable tag(s) detected (ignore-set hides {results.NumberOfDetections - kept.Count})"
                    );

                    var poseLibResult = poseLibSolver.PoseLibSolve(kept);

                    if (poseLibResult != null)
                    {
                        var (frcPos, frcRot) = Conversions.CvToFrc(poseLibResult.CameraPose);
                        var measuredRotation = new Rotation3d(
                            new Geometry.Quaternion(frcRot.w, frcRot.x, frcRot.y, frcRot.z)
                        );

                        int tagCount = kept.Count;
                        double inlierRatio =
                            (poseLibResult.TotalPoints > 0)
                                ? poseLibResult.AcceptedPoints / poseLibResult.TotalPoints
                                : 0.0;

                        // FIX: previously avgTagDistance was ||camera_position||, i.e. the
                        // camera's distance from the FIELD ORIGIN (blue alliance corner).
                        // That value silently inflates the dynamic std dev whenever the camera
                        // is far from the origin and made the user-facing maxDistance gate
                        // behave incorrectly (a robot in the red corner with a tag 1 m away
                        // would compute distance ~16 m). The pipeline appeared to work today
                        // because (a) maxDistance was unenforced and (b) the dynamic std dev
                        // only affects correction trust, not pass/fail. Switching to the true
                        // mean camera-to-tag distance changes the std dev curve - if pose
                        // behavior regresses after this change, suspect this block first.
                        double avgTagDistance = 0.0;
                        int distanceSamples = 0;
                        foreach (var det in kept)
                        {
                            var tagPose = aprilTagFieldLayout.GetTagPose(det.Id);
                            if (tagPose == null)
                            {
                                continue; // skip detections whose ID isn't in the layout
                            }
                            double dx = tagPose.X - frcPos.x;
                            double dy = tagPose.Y - frcPos.y;
                            double dz = tagPose.Z - frcPos.z;
                            avgTagDistance += Math.Sqrt(dx * dx + dy * dy + dz * dz);
                            distanceSamples++;
                        }
                        if (distanceSamples > 0)
                        {
                            avgTagDistance /= distanceSamples;
                        }

                        if (avgTagDistance > maxDistance)
                        {
                            QueuedLogger.Log(
                                $"AprilTag observation rejected: avgTagDistance={avgTagDistance:F2}m "
                                    + $"> maxDistance={maxDistance:F2}m"
                            );
                            yield return new WaitForSeconds(frameDelaySeconds);
                            continue;
                        }

                        // Dynamic std devs: uncertainty scales with distance^2 and decreases with tag count
                        double stdDevFactor =
                            (avgTagDistance * avgTagDistance) / Math.Max(1, tagCount);
                        double linearStdDev =
                            VioAprilTagPoseEstimatorConstants.MULTI_TAG_LINEAR_STD_DEV_BASE
                            * stdDevFactor;
                        var dynamicStdDevs = DenseMatrix.OfArray(
                            new[,]
                            {
                                { linearStdDev },
                                { linearStdDev },
                                { linearStdDev * 2.0 },
                            }
                        );

                        vioAprilTagPoseEstimator.AddAprilTagObservation(
                            new Translation3d(frcPos.x, frcPos.y, frcPos.z),
                            measuredRotation,
                            captureTimestamp,
                            dynamicStdDevs,
                            tagCount,
                            inlierRatio
                        );

                        QueuedLogger.Log(
                            $"PoseLib estimate: Pos({frcPos.x:F3}, {frcPos.y:F3}, {frcPos.z:F3}) "
                                + $"tags={tagCount} inliers={poseLibResult.AcceptedPoints}/{poseLibResult.TotalPoints} "
                                + $"ratio={inlierRatio:F2} dist={avgTagDistance:F2}m stdDev={linearStdDev:F4}"
                        );
                    }
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
