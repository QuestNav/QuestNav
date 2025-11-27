using QuestNav.Core;
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
        private readonly ICommandContext commandContext;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;

        /// <summary>
        /// Initializes a new instance of the PoseResetCommand
        /// </summary>
        /// <param name="commandContext">The command context to use for sending responses</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public PoseResetCommand(
            ICommandContext commandContext,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform
        )
        {
            this.commandContext = commandContext;
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
            ProtobufPose3d resetPose = receivedCommand.PoseResetPayload.TargetPose;
            double poseX = resetPose.Translation.X;
            double poseY = resetPose.Translation.Y;
            double poseZ = resetPose.Translation.Z;
            double poseQX = resetPose.Rotation.Q.X;
            double poseQY = resetPose.Rotation.Q.Y;
            double poseQZ = resetPose.Rotation.Q.Z;
            double poseQW = resetPose.Rotation.Q.W;

            // Validate pose data
            bool validPose =
                !double.IsNaN(poseX)
                && !double.IsNaN(poseY)
                && !double.IsNaN(poseZ)
                && !double.IsNaN(poseQX)
                && !double.IsNaN(poseQY)
                && !double.IsNaN(poseQZ)
                && !double.IsNaN(poseQW);

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
                 * 2. Calculate rotation difference between current camera and target
                 * 3. Apply rotation to root
                 * 4. Recalculate position after rotation
                 * 5. Apply the new position to vrCameraRoot
                 *
                 * This ensures the user's physical position in their room doesn't change, but their
                 * virtual position on the field matches what the robot expects.
                 */

                // Step 1: Convert FRC field coordinates (meters, standard orientation) to Unity coordinates
                // This accounts for coordinate system differences (FRC: X forward, Y left vs Unity: Z forward, X right)
                var (targetCameraPosition, targetCameraRotation) = Conversions.FrcToUnity3d(
                    resetPose
                );

                // Step 2: Calculate rotation difference between current camera and target
                Quaternion newRotation =
                    targetCameraRotation * Quaternion.Inverse(vrCamera.localRotation);

                // Step 3: Apply rotation to root
                vrCameraRoot.rotation = newRotation;

                // Step 4: Recalculate position after rotation
                Vector3 newRootPosition =
                    targetCameraPosition - (newRotation * vrCamera.localPosition);

                // Step 5: Apply the new position to vrCameraRoot
                vrCameraRoot.position = newRootPosition;

                QueuedLogger.Log(
                    $"Pose reset applied: X={poseX}, Y={poseY}, Z={poseZ} Rotation X={targetCameraRotation.eulerAngles.x}, "
                        + $"Y={targetCameraRotation.eulerAngles.y}, Z={targetCameraRotation.eulerAngles.z}"
                );

                // Send success response via command context
                // (NetworkTables context sends to robot, Web context is no-op)
                commandContext.SendSuccessResponse(receivedCommand.CommandId);
                QueuedLogger.Log("Pose reset completed successfully");
            }
            else
            {
                // Send error response via command context
                // (NetworkTables context sends to robot, Web context is no-op)
                commandContext.SendErrorResponse(
                    receivedCommand.CommandId,
                    "Failed to get valid pose data (invalid)"
                );
                QueuedLogger.LogError("Failed to get valid pose data");
            }
        }
    }
}
