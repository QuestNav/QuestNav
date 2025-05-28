using Proto.Commands;
using QuestNav.Commands.Commands;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for command processing.
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Processes commands received from the robot.
        /// </summary>
        void ProcessCommands();
    }
    
    public class CommandProcessor : ICommandProcessor
    {
        // Command context
        private NetworkTableConnection networkTableConnection;
        private Transform vrCamera;
        private Transform vrCameraRoot;
        private Transform resetTransform;
        
        // Commands
        private PoseResetCommand poseResetCommand;
        public CommandProcessor(NetworkTableConnection networkTableConnection, Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            // Command context
            this.networkTableConnection = networkTableConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            
            // Commands
            poseResetCommand = new PoseResetCommand(networkTableConnection, vrCamera, vrCameraRoot, resetTransform);
        }

        public void ProcessCommands()
        {
            Command receivedCommand = networkTableConnection.GetCommandRequest();
            
            switch (receivedCommand.CommandId)
            {
                case 0:
                    QueuedLogger.Log("Execute called with empty command", QueuedLogger.LogLevel.Warning);
                    break;
                case 1:
                    poseResetCommand.Execute(receivedCommand);
                    break;
                default:
                    QueuedLogger.Log("Execute called with unknown command", QueuedLogger.LogLevel.Warning);
                    break;
            }
        }
    }
}