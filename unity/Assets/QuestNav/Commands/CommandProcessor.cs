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

    public class CommandProcessor : ICommandProcessor
    {
        // Command context
        private NetworkTableConnection networkTableConnection;

        // Commands
        private PoseResetCommand poseResetCommand;

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

        public void ProcessCommands()
        {
            ProtobufQuestNavCommand receivedCommand = networkTableConnection.GetCommandRequest();

            switch (receivedCommand.CommandId)
            {
                case 0:
                    break;
                case 1:
                    QueuedLogger.Log(
                        "Executing Pose Reset Command"
                    );
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
    }
}
