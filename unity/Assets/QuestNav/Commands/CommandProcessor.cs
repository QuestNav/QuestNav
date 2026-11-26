using System.Linq;
using QuestNav.Commands.Commands;
using QuestNav.Config;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;
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
        private INetworkTableConnection networkTableConnection;

        /// <summary>
        /// The pose estimator required for pose reset commands
        /// </summary>
        private IVioAprilTagPoseEstimator vioAprilTagPoseEstimator;

        /// <summary>
        /// Command handler for pose reset operations
        /// </summary>
        private PoseResetCommand poseResetCommand;

        /// <summary>
        /// Cached value of the allowed pose reset timeout, updated via config events
        /// </summary>
        private int cachedPoseResetTtlMs;

        /// <summary>
        /// Initializes a new command processor with required dependencies
        /// </summary>
        /// <param name="networkTableConnection">Network connection for command communication</param>
        /// <param name="vioAprilTagPoseEstimator">Pose estimator for Kalman filter resets</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        /// <param name="configManager">Configuration manager for subscribing to setting changes</param>
        /// <param name="field">Field size from the loaded layout, or null if unknown</param>
        public CommandProcessor(
            INetworkTableConnection networkTableConnection,
            IVioAprilTagPoseEstimator vioAprilTagPoseEstimator,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform,
            IConfigManager configManager,
            Field2d field = null
        )
        {
            // Store network connection for command processing
            this.networkTableConnection = networkTableConnection;

            // Subscribe to config change events
            configManager.OnAllowedPoseResetTimeoutMsChanged += OnAllowedPoseResetTimeoutMsChanged;

            // Create NetworkTables command context for sending responses to robot
            var commandContext = new NetworkTablesCommandContext(networkTableConnection);

            // Initialize commands with NetworkTables context
            poseResetCommand = new PoseResetCommand(
                commandContext,
                vrCamera,
                vrCameraRoot,
                resetTransform,
                vioAprilTagPoseEstimator,
                field
            );
        }

        /// <summary>
        /// Gets the age in milliseconds of a value received at <paramref name="lastChangeNs"/>
        /// </summary>
        /// <param name="ntNowNs">Current NT local time in nanoseconds</param>
        /// <param name="lastChangeNs">NT local time of the value's last change in nanoseconds</param>
        public static long GetAgeMs(long ntNowNs, long lastChangeNs) =>
            (ntNowNs - lastChangeNs) / 1_000_000;

        #region Event Subscribers
        private void OnAllowedPoseResetTimeoutMsChanged(int timeoutMs)
        {
            cachedPoseResetTtlMs = timeoutMs;
        }
        #endregion

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
                            var ageMs = GetAgeMs(
                                networkTableConnection.NtNow,
                                receivedTimestampedCommand.LastChange
                            );

                            // Check if the command is fresh
                            if (ageMs < cachedPoseResetTtlMs)
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
                                        + $"Age: {ageMs} ms > {cachedPoseResetTtlMs} ms"
                                );
                                networkTableConnection.SendCommandErrorResponse(
                                    receivedCommand.CommandId,
                                    $"Pose Reset Command too old. Age: {ageMs} ms > {cachedPoseResetTtlMs} ms"
                                );
                            }
                        }
                        break;
                    default:
                        QueuedLogger.Log(
                            $"Execute called with unknown command. ID: {receivedCommand.CommandId} Type: {receivedCommand.Type}",
                            QueuedLogger.LogLevel.WARNING
                        );
                        break;
                }
            }
        }
    }
}
