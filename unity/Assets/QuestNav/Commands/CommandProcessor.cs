using QuestNav.Commands.Commands;
using QuestNav.Protos.Generated;
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

    /// <summary>
    /// Processes commands received from the robot and executes appropriate actions
    /// </summary>
    public class CommandProcessor : ICommandProcessor
    {
        /// <summary>
        /// Network connection for command communication
        /// </summary>
        private NetworkTableConnection networkTableConnection;

        /// <summary>
        /// Command handler for pose reset operations
        /// </summary>
        private PoseResetCommand poseResetCommand;

        /// <summary>
        /// ID of the last processed command to prevent duplicate execution
        /// </summary>
        private uint lastCommandIdProcessed;

        /// <summary>
        /// Initializes a new command processor with required dependencies
        /// </summary>
        /// <param name="networkTableConnection">Network connection for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public CommandProcessor(
            NetworkTableConnection networkTableConnection,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform
        )
        {
            // Command context
            this.networkTableConnection = networkTableConnection;

            // Commands
            poseResetCommand = new PoseResetCommand(
                networkTableConnection,
                vrCamera,
                vrCameraRoot,
                resetTransform
            );
        }

        /// <summary>
        /// Processes incoming commands from the robot and executes them if they haven't been processed before
        /// </summary>
        public void ProcessCommands()
        {
            ProtobufQuestNavCommand receivedCommand = networkTableConnection.GetCommandRequest();
            if (receivedCommand.CommandId != lastCommandIdProcessed)
            {
                switch (receivedCommand.Type)
                {
                    case QuestNavCommandType.CommandTypeUnspecified:
                        break;
                    case QuestNavCommandType.PoseReset:
                        QueuedLogger.Log("Executing Pose Reset Command");
                        poseResetCommand.Execute(receivedCommand);
                        break;
                    default:
                        QueuedLogger.Log(
                            "Execute called with unknown command",
                            QueuedLogger.LogLevel.Warning
                        );
                        break;
                }
            }
            // Don't double process
            lastCommandIdProcessed = networkTableConnection.GetCommandRequest().CommandId;
        }
    }
}
