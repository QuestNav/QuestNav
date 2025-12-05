using QuestNav.Core;
using QuestNav.Native.NTCore;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Network
{
    /// <summary>
    /// Interface for NetworkTables connection management.
    /// </summary>
    public interface INetworkTableConnection
    {
        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets whether the connection is ready to connect.
        /// </summary>
        /// <returns>true when either an IP or team number has been set</returns>
        bool IsReadyToConnect { get; }

        /// <summary>
        /// Gets the current NT time
        /// </summary>
        long Now { get; }

        /// <summary>
        /// Publishes frame data to NetworkTables.
        /// </summary>
        /// <param name="frameCount">Current frame index</param>
        /// <param name="timeStamp">Current timestamp</param>
        /// <param name="position">Current field-relative position of the Quest headset</param>
        /// <param name="rotation">The rotation of the quest headset</param>
        /// <param name="isTracking">Is the headset is currently tracking its position</param>
        void PublishFrameData(
            int frameCount,
            double timeStamp,
            Vector3 position,
            Quaternion rotation,
            bool isTracking
        );

        /// <summary>
        /// Publishes device data to NetworkTables.
        /// </summary>
        /// <param name="currentlyTracking">Is the quest tracking currently</param>
        /// <param name="trackingLostCounter">Number of tracking lost events this session</param>
        /// <param name="batteryPercent">Current battery percentage</param>
        void PublishDeviceData(bool currentlyTracking, int trackingLostCounter, int batteryPercent);

        /// <summary>
        /// Updates the team number.
        /// </summary>
        /// <param name="teamNumber">The team number</param>
        void UpdateTeamNumber(int teamNumber);

        /// <summary>
        /// Gets all command requests from the robot since the last read, or an empty array if none available
        /// </summary>
        /// <returns>All command requests since the last read</returns>
        TimestampedValue<ProtobufQuestNavCommand>[] GetCommandRequests();

        /// <summary>
        /// Sends a command processing success response back to the robot
        /// </summary>
        /// <param name="commandId">command_id</param>
        void SendCommandSuccessResponse(uint commandId);

        /// <summary>
        /// Sends a command processing error response back to the robot
        /// </summary>
        /// <param name="commandId">command_id</param>
        /// <param name="errorMessage">error message</param>
        void SendCommandErrorResponse(uint commandId, string errorMessage);

        /// <summary>
        /// Processes and logs NetworkTables internal messages
        /// </summary>
        void LoggerPeriodic();
    }
}

/// <summary>
/// Manages NetworkTables connections for communication with an FRC robot.
/// </summary>
public class NetworkTableConnection : INetworkTableConnection
{
    #region Fields
    /// <summary>
    /// NetworkTables connection for FRC data communication
    /// </summary>
    private NtInstance ntInstance;

    /// <summary>
    /// Logger for NetworkTables internal messages
    /// </summary>
    private PolledLogger ntInstanceLogger;

    /// <summary>
    /// Publisher for frame data (position/rotation updates)
    /// </summary>
    private ProtobufPublisher<ProtobufQuestNavFrameData> frameDataPublisher;

    /// <summary>
    /// Publisher for device data (tracking status, battery, etc.)
    /// </summary>
    private ProtobufPublisher<ProtobufQuestNavDeviceData> deviceDataPublisher;

    /// <summary>
    /// Publisher for command responses (Quest to robot)
    /// </summary>
    private ProtobufPublisher<ProtobufQuestNavCommandResponse> commandResponsePublisher;

    /// <summary>
    /// Subscriber for command requests (robot to Quest)
    /// </summary>
    private ProtobufSubscriber<ProtobufQuestNavCommand> commandRequestSubscriber;

    /// <summary>
    /// Flag indicating if a team number has been set
    /// </summary>
    private bool teamNumberSet = false;

    /// <summary>
    /// Flag indicating if an IP address has been set
    /// </summary>
    private bool ipAddressSet = false;
    #endregion

    /// <summary>
    /// Initializes a new NetworkTables connection with publishers and subscribers for QuestNav communication.
    ///
    /// QUESTNAV COMMUNICATION TOPICS:
    /// Publishers (Quest → Robot):
    /// - /QuestNav/frameData: High-frequency pose updates (100Hz)
    /// - /QuestNav/deviceData: Device status updates (3Hz)
    /// - /QuestNav/response: Command execution results
    ///
    /// Subscribers (Robot → Quest):
    /// - /QuestNav/request: Commands from robot (pose resets, etc.)
    ///
    /// PROTOBUF SERIALIZATION:
    /// Uses Protocol Buffers for efficient, versioned message serialization.
    /// This provides type safety, backward compatibility, and compact encoding.
    /// </summary>
    public NetworkTableConnection()
    {
        // Create NetworkTables instance with QuestNav namespace
        // This isolates QuestNav topics from other NetworkTables data
        ntInstance = new NtInstance(QuestNavConstants.Topics.NT_BASE_PATH);

        // Set up logging to capture NetworkTables internal messages
        // Helps diagnose connection issues, protocol errors, etc.
        ntInstanceLogger = ntInstance.CreateLogger(
            QuestNavConstants.Logging.NT_LOG_LEVEL_MIN,
            QuestNavConstants.Logging.NT_LOG_LEVEL_MAX
        );

        /*
         * PUBLISHER SETUP - Quest sends data TO robot
         * Each publisher is configured with:
         * - Topic name: Hierarchical path for organization
         * - Protobuf schema: Ensures type safety and versioning
         * - Publisher options: Reliability, frequency, etc.
         */

        // High-frequency pose data (100Hz) - robot needs this for real-time tracking
        frameDataPublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavFrameData>(
            QuestNavConstants.Topics.FRAME_DATA,
            "questnav.protos.data.ProtobufQuestNavFrameData",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );

        // Low-frequency device status (3Hz) - robot uses this for diagnostics
        deviceDataPublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavDeviceData>(
            QuestNavConstants.Topics.DEVICE_DATA,
            "questnav.protos.data.ProtobufQuestNavDeviceData",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );

        // Command responses - Quest confirms command execution to robot
        commandResponsePublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavCommandResponse>(
            QuestNavConstants.Topics.COMMAND_RESPONSE,
            "questnav.protos.commands.ProtobufQuestNavCommandResponse",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );

        /*
         * SUBSCRIBER SETUP - Quest receives data FROM robot
         * Robot can send commands like pose resets, calibration requests, etc.
         */
        commandRequestSubscriber = ntInstance.GetProtobufSubscriber<ProtobufQuestNavCommand>(
            QuestNavConstants.Topics.COMMAND_REQUEST,
            "questnav.protos.commands.ProtobufQuestNavCommand",
            new PubSubOptions
            {
                SendAll = true,
                KeepDuplicates = true,
                Periodic = 0.005,
                PollStorage = 20,
            }
        );
    }

    #region Properties

    /// <summary>
    /// Gets whether the connection is currently established.
    /// </summary>
    public bool IsConnected => ntInstance.IsConnected();

    /// <summary>
    /// Gets whether the connection is currently established.
    /// </summary>
    public bool IsReadyToConnect => teamNumberSet || ipAddressSet;

    /// <summary>
    /// Gets the current NT time
    /// </summary>
    public long Now => ntInstance.Now();

    /// <summary>
    /// Updates the team number and configures the NetworkTables connection.
    /// Checks WebServerConstants.debugNTServerAddressOverride - if set, uses direct IP connection instead of team number.
    /// NetworkTables automatically handles reconnection when server configuration changes.
    /// </summary>
    /// <param name="teamNumber">The FRC team number (ignored if debug IP override is set)</param>
    public void UpdateTeamNumber(int teamNumber)
    {
        // Check if using debug IP override or standard team number mode
        if (string.IsNullOrEmpty(WebServerConstants.debugNTServerAddressOverride))
        {
            // Standard mode: Use team number to resolve robot address
            QueuedLogger.Log($"Setting Team number to {teamNumber}");
            ntInstance.SetTeamNumber(teamNumber, WebServerConstants.ntServerPort);
            teamNumberSet = true;
            ipAddressSet = false;
        }
        else
        {
            // Debug mode: Use direct IP address (bypasses team number resolution)
            QueuedLogger.LogWarning(
                $"[DEBUG MODE] Using IP Override: {WebServerConstants.debugNTServerAddressOverride} - This should only be used for debugging!"
            );
            ntInstance.SetAddresses(
                new (string addr, int port)[]
                {
                    (
                        WebServerConstants.debugNTServerAddressOverride,
                        WebServerConstants.ntServerPort
                    ),
                }
            );
            ipAddressSet = true;
            teamNumberSet = false;
        }
    }
    #endregion

    #region Data Publishing Methods

    /// <summary>
    /// Reusable frame data object to avoid allocations
    /// </summary>
    private readonly ProtobufQuestNavFrameData frameData = new();

    /// <summary>
    /// Publishes current frame data to NetworkTables including position, rotation, and timing information
    /// </summary>
    /// <param name="frameCount">Unity frame count</param>
    /// <param name="timeStamp">Unity time stamp</param>
    /// <param name="position">Current VR headset position</param>
    /// <param name="rotation">Current VR headset rotation</param>
    /// <param name="isTracking">Is the headset is currently tracking its position</param>
    public void PublishFrameData(
        int frameCount,
        double timeStamp,
        Vector3 position,
        Quaternion rotation,
        bool isTracking
    )
    {
        frameData.FrameCount = frameCount;
        frameData.Timestamp = timeStamp;
        frameData.Pose3D = Conversions.UnityToFrc3d(position, rotation);
        frameData.IsTracking = isTracking;

        // Publish data
        frameDataPublisher.Set(frameData);
    }

    /// <summary>
    /// Reusable device data object to avoid allocations
    /// </summary>
    private readonly ProtobufQuestNavDeviceData deviceData = new();

    /// <summary>
    /// Publishes current device data to NetworkTables including tracking status and battery level
    /// </summary>
    /// <param name="currentlyTracking">Whether the headset is currently tracking</param>
    /// <param name="trackingLostCounter">Number of times tracking was lost this session</param>
    /// <param name="batteryPercent">Current battery percentage</param>
    public void PublishDeviceData(
        bool currentlyTracking,
        int trackingLostCounter,
        int batteryPercent
    )
    {
        deviceData.CurrentlyTracking = currentlyTracking;
        deviceData.TrackingLostCounter = trackingLostCounter;
        deviceData.BatteryPercent = batteryPercent;

        // Publish data
        deviceDataPublisher.Set(deviceData);
    }

    #endregion

    #region Command Processing

    /// <summary>
    /// Default command returned when no command is available from the robot
    /// </summary>
    private readonly ProtobufQuestNavCommand defaultCommand = new()
    {
        Type = QuestNavCommandType.CommandTypeUnspecified,
        CommandId = 0,
    };

    /// <summary>
    /// Gets all command requests from the robot since the last read, or an empty array if none available
    /// </summary>
    /// <returns>All command requests since the last read</returns>
    public TimestampedValue<ProtobufQuestNavCommand>[] GetCommandRequests()
    {
        return commandRequestSubscriber.ReadQueueValues();
    }

    /// <summary>
    /// Sends a command response back to the robot
    /// </summary>
    /// <param name="response">The response containing success status and any error messages</param>
    private void SetCommandResponse(ProtobufQuestNavCommandResponse response)
    {
        commandResponsePublisher.Set(response);
    }

    /// <summary>
    /// Sends a command processing success response back to the robot
    /// </summary>
    /// <param name="commandId">command_id</param>
    public void SendCommandSuccessResponse(uint commandId)
    {
        SetCommandResponse(
            new ProtobufQuestNavCommandResponse { CommandId = commandId, Success = true }
        );
    }

    /// <summary>
    /// Sends a command processing error response back to the robot
    /// </summary>
    /// <param name="commandId">command_id</param>
    /// <param name="errorMessage">error message</param>
    public void SendCommandErrorResponse(uint commandId, string errorMessage)
    {
        SetCommandResponse(
            new ProtobufQuestNavCommandResponse
            {
                CommandId = commandId,
                Success = false,
                ErrorMessage = errorMessage,
            }
        );
    }

    #endregion

    #region Logging

    /// <summary>
    /// Processes and logs any pending NetworkTables internal messages.
    /// Respects the enableDebugLogging tunable - when disabled, only WARNING and above are logged.
    /// </summary>
    public void LoggerPeriodic()
    {
        var messages = ntInstanceLogger.PollForMessages();
        if (messages == null)
            return;

        // Determine minimum log level based on debug logging setting
        int minLevel = WebServerConstants.enableDebugLogging
            ? QuestNavConstants.Logging.NTLogLevel.DEBUG1 // Show all debug messages
            : QuestNavConstants.Logging.NTLogLevel.WARNING; // Only show warnings and errors

        foreach (var message in messages)
        {
            // Filter messages based on log level
            if (message.level >= minLevel)
            {
                QueuedLogger.Log($"[NTCoreInternal/{message.filename}] {message.message}");
            }
        }
    }

    #endregion
}
