using QuestNav.Core;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Estimation;
using QuestNav.Utils;
using QuestNav.QuestNav.Geometry;
using UnityEngine;
using Wpi.Proto;
using Quaternion = UnityEngine.Quaternion;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera pose and the Kalman filter to a specified position.
    /// </summary>
    public class PoseResetCommand : ICommand
    {
        private readonly ICommandContext commandContext;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;
        private readonly IVioAprilTagPoseEstimator poseEstimator;

        /// <summary>
        /// Initializes a new instance of the PoseResetCommand.
        /// </summary>
        public PoseResetCommand(
            ICommandContext commandContext,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform,
            IVioAprilTagPoseEstimator poseEstimator
        )
        {
            this.commandContext = commandContext;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            this.poseEstimator = poseEstimator;
        }

        /// <summary>
        /// The formatted name for PoseResetCommand.
        /// </summary>
        public string CommandNiceName => "PoseReset";

        /// <summary>
        /// Executes the pose reset by applying the target pose to the VR camera system
        /// and resetting the Kalman filter to match.
        /// </summary>
        public void Execute(ProtobufQuestNavCommand receivedCommand)
        {
            QueuedLogger.Log("Received pose reset request, initiating reset...");

            // Read pose data from network tables
            ProtobufPose3d resetPose = receivedCommand.PoseResetPayload.TargetPose;
            double poseX = resetPose.Translation.X;
            double poseY = resetPose.Translation.Y;
            double poseZ = resetPose.Translation.Z;
            double poseQx = resetPose.Rotation.Q.X;
            double poseQy = resetPose.Rotation.Q.Y;
            double poseQz = resetPose.Rotation.Q.Z;
            double poseQW = resetPose.Rotation.Q.W;

            // Validate pose data
            bool validPose =
                !double.IsNaN(poseX)
                && !double.IsNaN(poseY)
                && !double.IsNaN(poseZ)
                && !double.IsNaN(poseQx)
                && !double.IsNaN(poseQy)
                && !double.IsNaN(poseQz)
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
                // Step 1: Convert FRC field coordinates to Unity coordinates
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

                // Step 6: Reset the Kalman filter so it agrees with the new VIO reference frame.
                // After moving vrCameraRoot, the next VIO reading will be relative to this new
                // origin. The filter must be told so it doesn't interpret the jump as displacement.
                var resetPose3d = new Pose3d(
                    targetCameraPosition.x, targetCameraPosition.y, targetCameraPosition.z,
                    new Rotation3d(new QuestNav.Geometry.Quaternion(poseQW, poseQx, poseQy, poseQz))
                );
                poseEstimator.ResetPosition(resetPose3d, Time.timeAsDouble);

                QueuedLogger.Log(
                    $"Pose reset applied: X={poseX}, Y={poseY}, Z={poseZ} Rotation W={poseQW}, X={poseQx}, "
                        + $"Y={poseQy}, Z={poseQz}"
                );

                commandContext.SendSuccessResponse(receivedCommand.CommandId);
                QueuedLogger.Log("Pose reset completed successfully");
            }
            else
            {
                commandContext.SendErrorResponse(
                    receivedCommand.CommandId,
                    "Failed to get valid pose data (invalid)"
                );
                QueuedLogger.LogError("Failed to get valid pose data");
            }
        }
    }
}