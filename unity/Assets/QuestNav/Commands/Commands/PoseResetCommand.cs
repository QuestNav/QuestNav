using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.Utils;
using UnityEngine;
using Wpi.Proto;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera pose to a specified position
    /// </summary>
    public class PoseResetCommand : ICommand
    {
        private readonly INetworkTableConnection networkTableConnection;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;

        /// <summary>
        /// Initializes a new instance of the PoseResetCommand
        /// </summary>
        /// <param name="networkTableConnection">The network connection to use for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public PoseResetCommand(
            INetworkTableConnection networkTableConnection,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform
        )
        {
            this.networkTableConnection = networkTableConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
        }

        /// <summary>
        /// The formatted name for PoseResetCommand
        /// </summary>
        public string commandNiceName => "PoseReset";

        /// <summary>
        /// Executes the pose reset command by applying the target pose to the VR camera system
        /// </summary>
        /// <param name="receivedCommand">The command containing pose reset payload with target position</param>
        public void Execute(ProtobufQuestNavCommand receivedCommand)
        {
            QueuedLogger.Log("Received pose reset request, initiating reset...");

            // Read pose data from network tables
            ProtobufPose2d resetPose = receivedCommand.PoseResetPayload.TargetPose;
            double poseX = resetPose.Translation.X;
            double poseY = resetPose.Translation.Y;
            double poseTheta = resetPose.Rotation.Value;

            // Validate pose data
            bool validPose =
                !double.IsNaN(poseX) && !double.IsNaN(poseY) && !double.IsNaN(poseTheta);

            // Additional validation for field boundaries
            if (validPose)
            {
                bool inBounds =
                    poseX >= 0
                    && poseX <= QuestNavConstants.Field.FIELD_LENGTH
                    && poseY >= 0
                    && poseY <= QuestNavConstants.Field.FIELD_WIDTH;

                if (!inBounds)
                {
                    QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
                }
            }

            // Apply pose reset if data is valid
            if (validPose)
            {
                /*
                 * POSE RESET ALGORITHM EXPLANATION:
                 *
                 * The challenge: We need to move the VR camera to a specific field position, but the user
                 * might be standing anywhere in their physical play space. We can't move the user physically,
                 * so we move the virtual world around them.
                 *
                 * VR Hierarchy:
                 * - vrCameraRoot: The "world origin" that we can move/rotate
                 * - vrCamera: The actual headset position (controlled by VR tracking, we can't move this directly)
                 *
                 * Algorithm Steps:
                 * 1. Convert target field coordinates to Unity world coordinates
                 * 2. Calculate how much we need to rotate the world to align headset orientation
                 * 3. Capture the headset's offset from world origin BEFORE rotation
                 * 4. Rotate the world origin to align orientations
                 * 5. Move the world origin so the headset ends up at the target position
                 *
                 * This ensures the user's physical position in their room doesn't change, but their
                 * virtual position on the field matches what the robot expects.
                 */

                // Step 1: Convert FRC field coordinates (meters, standard orientation) to Unity coordinates
                // This accounts for coordinate system differences (FRC: X forward, Y left vs Unity: Z forward, X right)
                var (targetCameraPosition, targetCameraRotation) = Conversions.FrcToUnity(
                    resetPose,
                    vrCamera.position,
                    vrCamera.rotation
                );

                // Step 2: Calculate how much to rotate the world to align headset with target orientation
                // We only care about Y-axis rotation (yaw) since pitch/roll should remain user-controlled
                float currentCameraY = vrCamera.rotation.eulerAngles.y;
                float targetCameraY = targetCameraRotation.eulerAngles.y;
                float rotationDifference = Mathf.DeltaAngle(currentCameraY, targetCameraY);

                // Step 3: Capture headset offset from world origin in LOCAL space BEFORE we rotate
                // This is crucial - we need the offset in the coordinate system that will be rotated
                Vector3 localCameraOffset = vrCameraRoot.InverseTransformPoint(vrCamera.position);

                // Step 4: Rotate the world origin to align orientations
                // This rotates the entire virtual world around the user
                vrCameraRoot.Rotate(0, rotationDifference, 0);

                // Step 5: Calculate where to position the world origin so headset ends up at target
                // After rotation, we need to recalculate the world-space offset and adjust accordingly
                Vector3 worldCameraOffset =
                    vrCameraRoot.TransformPoint(localCameraOffset) - vrCameraRoot.position;
                Vector3 targetRootPosition = targetCameraPosition - worldCameraOffset;
                vrCameraRoot.position = targetRootPosition;

                QueuedLogger.Log($"Pose reset applied: X={poseX}, Y={poseY}, Theta={poseTheta}");

                networkTableConnection.SetCommandResponse(
                    new ProtobufQuestNavCommandResponse
                    {
                        CommandId = receivedCommand.CommandId,
                        Success = true,
                    }
                );
                QueuedLogger.Log("Pose reset completed successfully");
            }
            else
            {
                networkTableConnection.SetCommandResponse(
                    new ProtobufQuestNavCommandResponse
                    {
                        CommandId = receivedCommand.CommandId,
                        ErrorMessage = "Failed to get valid pose data (invalid)",
                        Success = false,
                    }
                );
                QueuedLogger.LogError("Failed to get valid pose data");
            }
        }
    }
}
