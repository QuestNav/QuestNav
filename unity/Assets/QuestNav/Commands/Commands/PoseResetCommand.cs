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
        /// Executes the pose reset command
        /// </summary>
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
                validPose =
                    poseX >= 0
                    && poseX <= QuestNavConstants.Field.FIELD_LENGTH
                    && poseY >= 0
                    && poseY <= QuestNavConstants.Field.FIELD_WIDTH;

                if (!validPose)
                {
                    QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
                }
            }

            // Apply pose reset if data is valid
            if (validPose)
            {
                // Convert field coordinates to Unity coordinates
                var (targetCameraPosition, targetCameraRotation) = Conversions.FrcToUnity(
                    resetPose,
                    vrCamera.position,
                    vrCamera.rotation
                );

                // Calculate Y rotation difference between current camera and target
                float currentCameraY = vrCamera.rotation.eulerAngles.y;
                float targetCameraY = targetCameraRotation.eulerAngles.y;
                float rotationDifference = Mathf.DeltaAngle(currentCameraY, targetCameraY);

                // Get camera offset in LOCAL space relative to root BEFORE rotation
                Vector3 localCameraOffset = vrCameraRoot.InverseTransformPoint(vrCamera.position);

                // Apply rotation to root
                vrCameraRoot.Rotate(0, rotationDifference, 0);

                // Recalculate position after rotation
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
                        ErrorMessage = "Failed to get valid pose data (Out of bounds or invalid)",
                        Success = false,
                    }
                );
                QueuedLogger.LogError("Failed to get valid pose data");
            }
        }
    }
}
