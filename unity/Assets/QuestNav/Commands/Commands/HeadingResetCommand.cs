using System;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera heading to match the reset transform
    /// </summary>
    public class HeadingResetCommand : CommandBase
    {
        private readonly INetworkTableConnection networkConnection;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;
        
        /// <summary>
        /// Initializes a new instance of the HeadingResetCommand
        /// </summary>
        /// <param name="networkConnection">The network connection to use for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public HeadingResetCommand(
            INetworkTableConnection networkConnection,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform)
        {
            this.networkConnection = networkConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
        }
        
        /// <summary>
        /// Executes the heading reset
        /// </summary>
        /// <returns>True because this command completes in one execution</returns>
        protected override bool ExecuteCommand()
        {
            try
            {
                QueuedLogger.Log("Executing heading reset...");
                
                // Calculate rotation difference between camera and reset transform
                float rotationAngleY = vrCamera.rotation.eulerAngles.y - resetTransform.rotation.eulerAngles.y;

                // Apply rotation correction
                vrCameraRoot.transform.Rotate(0, -rotationAngleY, 0);

                // Apply position correction
                Vector3 distanceDiff = resetTransform.position - vrCamera.position;
                vrCameraRoot.transform.position += distanceDiff;
                
                // Set success response
                networkConnection.SetCommandResponse(QuestNavConstants.Commands.HEADING_RESET_SUCCESS);
                
                // Log the details of what was corrected
                QueuedLogger.Log($"Heading reset applied: Rotated {-rotationAngleY} degrees, translated by {distanceDiff}");
                
                // Command completes in one step
                return true;
            }
            catch (Exception e)
            {
                QueuedLogger.LogError($"Error during heading reset: {e.Message}");
                QueuedLogger.LogException(e);
                
                // We're already handling the exception here, so we don't want CommandBase to handle it again
                // Instead, we'll mark the command as failed directly
                SetFailed();
                
                // Set error response
                networkConnection.SetCommandResponse(QuestNavConstants.Commands.IDLE);
                
                // Command is now complete (with error)
                return true;
            }
        }
        
        /// <summary>
        /// Called when command execution starts
        /// </summary>
        protected override void OnStart()
        {
            QueuedLogger.Log("Starting heading reset operation");
        }
        
        /// <summary>
        /// Called when command execution ends
        /// </summary>
        protected override void OnEnd(bool interrupted)
        {
            if (interrupted)
            {
                QueuedLogger.LogWarning("Heading reset was interrupted");
            }
            else if (State == CommandState.Completed)
            {
                QueuedLogger.Log("Heading reset completed successfully");
            }
            else if (State == CommandState.Failed)
            {
                QueuedLogger.LogError("Heading reset failed");
            }
        }
    }
}