using System.Linq;
using QuestNav.Commands.Commands;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.Utils;
using UnityEngine;
using static QuestNav.Core.QuestNavConstants.Commands;

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
        private INetworkTableConnection networkTableConnection;

        /// <summary>
        /// Command handler for pose reset operations
        /// </summary>
        private PoseResetCommand poseResetCommand;

        /// <summary>
        /// Initializes a new command processor with required dependencies
        /// </summary>
        /// <param name="networkTableConnection">Network connection for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public CommandProcessor(
            INetworkTableConnection networkTableConnection,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform
        )
        {
            // Store network connection for command processing
            this.networkTableConnection = networkTableConnection;

            // Create NetworkTables command context for sending responses to robot
            var commandContext = new NetworkTablesCommandContext(networkTableConnection);

            // Initialize commands with NetworkTables context
            poseResetCommand = new PoseResetCommand(
                commandContext,
                vrCamera,
                vrCameraRoot,
                resetTransform
            );
        }

        /// <summary>
        /// Processes incoming commands from the robot and executes them in order
        /// </summary>
        public void ProcessCommands()
        {
            var receivedCommands = networkTableConnection.GetCommandRequests();
            // Collect all but the most recent PoseReset command, they have been superseded
            var supersededPoseResetCommands = receivedCommands
                .Where(cmd => cmd.Value.Type == QuestNavCommandType.PoseReset)
                .SkipLast(1)
                .ToHashSet();

            foreach (var receivedTimestampedCommand in receivedCommands)
            {
                var receivedCommand = receivedTimestampedCommand.Value;
                switch (receivedCommand.Type)
                {
                    case QuestNavCommandType.CommandTypeUnspecified:
                        break;
                    case QuestNavCommandType.PoseReset:
                        if (supersededPoseResetCommands.Contains(receivedTimestampedCommand))
                        {
                            // The command was superseded by a later command, skip it
                            QueuedLogger.Log(
                                $"Skipping superseded Pose Reset Command. ID: {receivedCommand.CommandId}"
                            );
                            networkTableConnection.SendCommandErrorResponse(
                                receivedCommand.CommandId,
                                "Pose Reset Command superseded"
                            );
                        }
                        else
                        {
                            // Get the age of the command, in milliseconds
                            var ageMs =
                                (
                                    networkTableConnection.NtNow
                                    - receivedTimestampedCommand.LastChange
                                ) / 1000;

                            // Check if the command is fresh
                            if (ageMs < POSE_RESET_TTL_MS)
                            {
                                // The command is fresh, process it
                                QueuedLogger.Log(
                                    $"Executing Pose Reset Command. ID: {receivedCommand.CommandId} "
                                        + $"Age: {ageMs} ms"
                                );
                                poseResetCommand.Execute(receivedCommand);
                            }
                            else
                            {
                                // The command is too old, skip it
                                QueuedLogger.Log(
                                    $"Skipping stale Pose Reset Command. ID: {receivedCommand.CommandId} "
                                        + $"Age: {ageMs} ms > {POSE_RESET_TTL_MS} ms"
                                );
                                networkTableConnection.SendCommandErrorResponse(
                                    receivedCommand.CommandId,
                                    $"Pose Reset Command too old. Age: {ageMs} ms > {POSE_RESET_TTL_MS} ms"
                                );
                            }
                        }
                        break;
                    default:
                        QueuedLogger.Log(
                            $"Execute called with unknown command. ID: {receivedCommand.CommandId} Type: {receivedCommand.Type}",
                            QueuedLogger.LogLevel.NT_WARNING
                        );
                        break;
                }
            }
        }
    }
}
