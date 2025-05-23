using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera pose to a specified position
    /// </summary>
    public class PoseResetCommand : CommandBase
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
        /// Executes the pose reset command
        /// </summary>
        /// <returns>True when the command is complete</returns>
        protected override bool ExecuteCommand()
        {
            QueuedLogger.Log("Received pose reset request, initiating reset...");
            
            // Read pose data from network tables
            float[] resetPose = networkTableConnection.GetPoseResetPosition();
            float poseX = resetPose[0];
            float poseY = resetPose[1];
            float poseTheta = resetPose[2];
            
            // Validate pose data
            bool validPose = !float.IsNaN(poseX) && !float.IsNaN(poseY) && !float.IsNaN(poseTheta);
            
            // Additional validation for field boundaries
            if (validPose) {
                validPose = 
                    poseX < 0 || poseX > QuestNavConstants.Field.FIELD_LENGTH ||
                    poseY < 0 || poseY > QuestNavConstants.Field.FIELD_WIDTH;
                
                if (!validPose) {
                    QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
                }
            }
            
            // Apply pose reset if data is valid
            if (validPose) {
                // Convert field coordinates to Unity coordinates
                Vector3 targetPosition = Conversions.FrcToUnity(resetPose, vrCamera.position.y);
                
                // Convert field rotation to Unity rotation
                Quaternion targetRotation = Quaternion.Euler(0, poseTheta * Mathf.Rad2Deg, 0);
                
                // Calculate differences
                Vector3 positionDifference = targetPosition - vrCamera.position;
                float rotationDifference = Mathf.DeltaAngle(
                    vrCamera.rotation.eulerAngles.y,
                    targetRotation.eulerAngles.y
                );
                
                // Apply position change
                vrCameraRoot.position += positionDifference;
                
                // Apply rotation change
                vrCameraRoot.Rotate(0, rotationDifference, 0);
                
                QueuedLogger.Log($"Pose reset applied: X={poseX}, Y={poseY}, Theta={poseTheta}");
                QueuedLogger.Log($"Position adjusted by {positionDifference}, rotation by {rotationDifference}");
                
                networkTableConnection.SetCommandResponse(QuestNavConstants.Commands.POSE_RESET_SUCCESS);
            } else {
                networkTableConnection.SetCommandResponse(QuestNavConstants.Commands.IDLE);
                QueuedLogger.LogWarning("Failed to get valid pose data");
            }
            
            return true; // Command is always complete after one execution
        }
        
        /// <summary>
        /// Called when command execution starts
        /// </summary>
        protected override void OnStart()
        {
            QueuedLogger.Log("Starting pose reset operation");
        }
        
        /// <summary>
        /// Called when command execution ends
        /// </summary>
        protected override void OnEnd(bool interrupted)
        {
            if (interrupted)
            {
                QueuedLogger.LogWarning("Pose reset was interrupted");
            }
            else if (State == CommandState.Completed)
            {
                QueuedLogger.Log("Pose reset completed successfully");
            }
            else if (State == CommandState.Failed)
            {
                QueuedLogger.LogError("Pose reset failed");
            }
        }
    }
}