using System.Threading.Tasks;
using QuestNav.Config;
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
        bool isConnected { get; }

        /// <summary>
        /// Gets whether the connection is ready to connect.
        /// </summary>
        /// <returns>true when either an IP or team number has been set</returns>
        bool isReadyToConnect { get; }

        /// <summary>
        /// Gets the current NT time
        /// </summary>
        long ntNow { get; }

        public Task initializeAsync();

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
        /// <param name="trackingLostCounter">Number of tracking lost events this session</param>
        /// <param name="batteryPercent">Current battery percentage</param>
        void PublishDeviceData(int trackingLostCounter, int batteryPercent);

        /// <summary>
        /// Gets all command requests from the robot since the last read, or an empty array if none available
        /// </summary>
        /// <returns>All command requests since the last read</returns>
        TimestampedValue<ProtobufQuestNavCommand>[] getCommandRequests();

        /// <summary>
        /// Sends a command processing success response back to the robot
        /// </summary>
        /// <param name="commandId">command_id</param>
        void sendCommandSuccessResponse(uint commandId);

        /// <summary>
        /// Sends a command processing error response back to the robot
        /// </summary>
        /// <param name="commandId">command_id</param>
        /// <param name="errorMessage">error message</param>
        void sendCommandErrorResponse(uint commandId, string errorMessage);

        /// <summary>
        /// Processes and logs NetworkTables internal messages
        /// </summary>
        void loggerPeriodic();
    }

    /// <summary>
    /// Manages NetworkTables connections for communication with an FRC robot.
    /// </summary>
    public class NetworkTableConnection : INetworkTableConnection
    {
        #region Fields
        /// <summary>
        /// ConfigManager for reading team number and IP override
        /// </summary>
        private IConfigManager configManager;

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
        public NetworkTableConnection(IConfigManager configManager)
        {
            this.configManager = configManager;

            // Create NetworkTables instance with QuestNav namespace
            // This isolates QuestNav topics from other NetworkTables data
            ntInstance = new NtInstance(QuestNavConstants.Topics.NT_BASE_PATH);

            /*
             * PUBLISHER SETUP - Quest sends data TO robot
             * Each publisher is configured with:
             * - Topic name: Hierarchical path for organization
             * - Protobuf schema: Ensures type safety and versioning
             * - Publisher options: Reliability, frequency, etc.
             */

            // High-frequency pose data (120Hz) - robot needs this for real-time tracking
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
            commandResponsePublisher =
                ntInstance.GetProtobufPublisher<ProtobufQuestNavCommandResponse>(
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

            // Attach local methods to config event methods
            configManager.onTeamNumberChanged += onTeamNumberChanged;
            configManager.onDebugIpOverrideChanged += onDebugIpOverrideChanged;
            configManager.onEnableDebugLoggingChanged += onEnableDebugLoggingChanged;
        }

        public async Task initializeAsync()
        {
            // Load saved values from config
            onTeamNumberChanged(await configManager.getTeamNumberAsync());
            onDebugIpOverrideChanged(await configManager.getDebugIpOverrideAsync());
            onEnableDebugLoggingChanged(await configManager.getEnableDebugLoggingAsync());
        }

        #region Properties

        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        public bool isConnected => ntInstance.IsConnected();

        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        public bool isReadyToConnect => teamNumberSet || ipAddressSet;

        /// <summary>
        /// Gets the current NT time
        /// </summary>
        public long ntNow => ntInstance.Now();
        #endregion

        #region Event Subscribers
        /// <summary>
        /// Changes to team number resolution mode and triggers an asynchronous connection reset
        /// </summary>
        /// <param name="teamNumber">The team number to resolve</param>
        private void onTeamNumberChanged(int teamNumber)
        {
            // Skip if -1 (indicates debug IP override in use)
            if (teamNumber.Equals(-1))
                return;

            // Standard mode: Use team number to resolve robot address
            QueuedLogger.Log($"Setting Team number to {teamNumber}");
            ntInstance.SetTeamNumber(teamNumber, QuestNavConstants.Network.NT_SERVER_PORT);
            teamNumberSet = true;
            ipAddressSet = false;
        }

        private void onDebugIpOverrideChanged(string ipOverride)
        {
            // Skip if empty (indicates team number being used)
            if (string.IsNullOrEmpty(ipOverride))
                return;

            // Debug mode: Use direct IP address (bypasses team number resolution)
            QueuedLogger.LogWarning(
                $"[DEBUG MODE] Using IP Override: {ipOverride} - This should only be used for debugging!"
            );
            ntInstance.SetAddresses(
                new (string addr, int port)[]
                {
                    (ipOverride, QuestNavConstants.Network.NT_SERVER_PORT),
                }
            );
            ipAddressSet = true;
            teamNumberSet = false;
        }

        private void onEnableDebugLoggingChanged(bool enableDebugLogging)
        {
            if (enableDebugLogging)
            {
                ntInstanceLogger = ntInstance.CreateLogger(
                    QuestNavConstants.Logging.NT_LOG_LEVEL_MIN_DEBUG,
                    QuestNavConstants.Logging.NT_LOG_LEVEL_MAX
                );
            }
            else
            {
                ntInstanceLogger = ntInstance.CreateLogger(
                    QuestNavConstants.Logging.NT_LOG_LEVEL_MIN_STANDARD,
                    QuestNavConstants.Logging.NT_LOG_LEVEL_MAX
                );
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
        /// <param name="trackingLostCounter">Number of times tracking was lost this session</param>
        /// <param name="batteryPercent">Current battery percentage</param>
        public void PublishDeviceData(int trackingLostCounter, int batteryPercent)
        {
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
        public TimestampedValue<ProtobufQuestNavCommand>[] getCommandRequests()
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
        public void sendCommandSuccessResponse(uint commandId)
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
        public void sendCommandErrorResponse(uint commandId, string errorMessage)
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
        public void loggerPeriodic()
        {
            var messages = ntInstanceLogger.PollForMessages();
            if (messages == null)
                return;

            foreach (var message in messages)
            {
                QueuedLogger.Log($"[NTCoreInternal/{message.filename}] {message.message}");
            }
        }

        #endregion
    }
}
