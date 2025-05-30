using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Protos;
using QuestNav.Utils;
using UnityEngine;

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
            Transform resetTransform)
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
        /// Executes the pose reset command
        /// </summary>
        public void Execute(Command receivedCommand)
        {
            QueuedLogger.Log("Received pose reset request, initiating reset...");

            // Read pose data from network tables
            Pose2d resetPose = receivedCommand.PoseResetPayload.TargetPose;
            double poseX = resetPose.Translation.X;
            double poseY = resetPose.Translation.Y;
            double poseTheta = resetPose.Rotation.Radians;

            // Validate pose data
            bool validPose = !double.IsNaN(poseX) && !double.IsNaN(poseY) && !double.IsNaN(poseTheta);

            // Additional validation for field boundaries
            if (validPose)
            {
                validPose =
                    poseX >= 0 && poseX <= QuestNavConstants.Field.FIELD_LENGTH &&
                    poseY >= 0 && poseY <= QuestNavConstants.Field.FIELD_WIDTH;

                if (!validPose)
                {
                    QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
                }
            }

            // Apply pose reset if data is valid
            if (validPose)
            {
                // Convert field coordinates to Unity coordinates
                Vector3 targetPosition = Conversions.FrcToUnity(resetPose, vrCamera.position.y);

                // Convert field rotation to Unity rotation
                float targetYRotation = (float)poseTheta * Mathf.Rad2Deg;

                // Cache current values
                Vector3 currentPosition = vrCamera.position;
                float currentYRotation = vrCamera.rotation.eulerAngles.y;

                // Calculate differences
                Vector3 positionDifference = targetPosition - currentPosition;
                float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

                // Apply position change
                vrCameraRoot.position += positionDifference;

                // Apply rotation change
                vrCameraRoot.Rotate(0, rotationDifference, 0);

                QueuedLogger.Log($"Pose reset applied: X={poseX}, Y={poseY}, Theta={poseTheta}");
                QueuedLogger.Log($"Position adjusted by {positionDifference}, rotation by {rotationDifference}");
                QueuedLogger.Log("Pose reset completed successfully");
                networkTableConnection.SetCommandResponse(new CommandResponse
                {
                    CommandId = receivedCommand.CommandId,
                    Success = true
                });
            }
            else
            {
                networkTableConnection.SetCommandResponse(new CommandResponse
                {
                    CommandId = receivedCommand.CommandId,
                    ErrorMessage = "Failed to get valid pose data (Out of bounds or invalid)",
                    Success = false
                });
                QueuedLogger.LogError("Failed to get valid pose data");
            }
        }
    }
}