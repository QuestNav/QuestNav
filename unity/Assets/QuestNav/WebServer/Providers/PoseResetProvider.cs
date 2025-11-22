using System;
using System.Reflection;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Provides VR pose reset functionality for the configuration system.
    /// Handles recentering VR tracking to origin (0,0,0) with identity rotation.
    /// Directly manipulates VR camera transforms instead of using PoseResetCommand.
    /// </summary>
    public class PoseResetProvider : MonoBehaviour
    {
        #region Fields
        /// <summary>
        /// Flag indicating pose reset was requested from background thread
        /// </summary>
        private bool poseResetRequested = false;

        /// <summary>
        /// Reference to VR camera transform
        /// </summary>
        private Transform vrCamera;

        /// <summary>
        /// Reference to VR camera root transform
        /// </summary>
        private Transform vrCameraRoot;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Initializes transform references via reflection.
        /// Finds QuestNav instance and caches VR camera transforms.
        /// </summary>
        void Start()
        {
            // Find QuestNav instance via reflection
            var questNavType = Type.GetType("QuestNav.Core.QuestNav, QuestNav");
            if (questNavType == null)
            {
                Debug.LogError("[PoseResetProvider] QuestNav.Core.QuestNav type not found");
                return;
            }

            var questNav = FindFirstObjectByType(questNavType);
            if (questNav == null)
            {
                Debug.LogError("[PoseResetProvider] QuestNav instance not found in scene");
                return;
            }

            // Get VR camera transforms via reflection
            var vrCameraField = questNavType.GetField(
                "vrCamera",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var vrCameraRootField = questNavType.GetField(
                "vrCameraRoot",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (vrCameraField == null || vrCameraRootField == null)
            {
                Debug.LogError("[PoseResetProvider] Failed to find VR camera transform fields");
                return;
            }

            vrCamera = vrCameraField.GetValue(questNav) as Transform;
            vrCameraRoot = vrCameraRootField.GetValue(questNav) as Transform;

            if (vrCamera == null || vrCameraRoot == null)
            {
                Debug.LogError("[PoseResetProvider] VR camera transforms are null");
                return;
            }

            Debug.Log("[PoseResetProvider] Initialized successfully");
        }

        /// <summary>
        /// Checks for pending pose reset requests on main thread.
        /// Executes reset when flag is set.
        /// </summary>
        void Update()
        {
            if (poseResetRequested)
            {
                poseResetRequested = false;
                ExecutePoseReset();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Requests pose reset. Can be called from any thread.
        /// Sets flag that will be checked on main thread in Update().
        /// </summary>
        public void RequestPoseReset()
        {
            poseResetRequested = true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Executes pose reset to origin by directly manipulating VR transforms.
        /// Uses the same algorithm as PoseResetCommand to make current position become (0,0,0).
        /// This follows the pattern from PoseResetCommand.cs without needing protobuf dependencies.
        /// </summary>
        private void ExecutePoseReset()
        {
            // Validate transforms are initialized
            if (vrCamera == null || vrCameraRoot == null)
            {
                Debug.LogError("[PoseResetProvider] VR camera transforms not initialized");
                return;
            }

            Debug.Log("[PoseResetProvider] Executing pose reset - recentering tracking to origin");

            try
            {
                /*
                 * RECENTER TRACKING ALGORITHM:
                 *
                 * Goal: Make the current camera position become the new origin (0,0,0) with no rotation.
                 * This is the inverse of PoseResetCommand - instead of moving TO a position,
                 * we make the current position BECOME the origin.
                 *
                 * VR Hierarchy:
                 * - vrCameraRoot: The "world origin" that we can move/rotate
                 * - vrCamera: The actual headset position (controlled by VR tracking, we can't move this directly)
                 *
                 * Algorithm (same as PoseResetCommand but with target = (0,0,0)):
                 * 1. Target position = (0, 0, 0)
                 * 2. Target rotation = Identity (no rotation)
                 * 3. Calculate rotation difference between current camera and target
                 * 4. Apply rotation to root
                 * 5. Recalculate position after rotation
                 * 6. Apply the new position to vrCameraRoot
                 */

                // Step 1: Define target position and rotation (origin with no rotation)
                Vector3 targetCameraPosition = Vector3.zero;
                Quaternion targetCameraRotation = Quaternion.identity;

                // Step 2: Calculate rotation difference between current camera and target
                // This compensates for the user's current head rotation
                Quaternion newRotation =
                    targetCameraRotation * Quaternion.Inverse(vrCamera.localRotation);

                // Step 3: Apply rotation to root
                vrCameraRoot.rotation = newRotation;

                // Step 4: Recalculate position after rotation
                // This formula ensures the camera ends up at targetCameraPosition
                // Formula from PoseResetCommand.cs line 128
                Vector3 newRootPosition =
                    targetCameraPosition - (newRotation * vrCamera.localPosition);

                // Step 5: Apply the new position to vrCameraRoot
                vrCameraRoot.position = newRootPosition;

                Debug.Log(
                    $"[PoseResetProvider] Pose reset completed - camera recentered to origin"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PoseResetProvider] Failed to execute pose reset: {ex.Message}");
                Debug.LogError($"[PoseResetProvider] Stack trace: {ex.StackTrace}");
            }
        }
        #endregion
    }
}
