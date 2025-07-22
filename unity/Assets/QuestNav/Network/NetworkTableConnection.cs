using System;
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
        /// <returns>true when either an IP or team number has been set</returns>
        /// </summary>
        bool IsReadyToConnect { get; }

        /// <summary>
        /// Publishes frame data to NetworkTables.
        /// </summary>
        /// <param name="frameCount">Current frame index</param>
        /// <param name="timeStamp">Current timestamp</param>
        /// <param name="position">Current field-relative position of the Quest headset</param>
        /// <param name="rotation">The rotation of the quest headset</param>
        void PublishFrameData(
            int frameCount,
            double timeStamp,
            Vector3 position,
            Quaternion rotation
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

        ProtobufQuestNavCommand GetCommandRequest();

        void SetCommandResponse(ProtobufQuestNavCommandResponse response);

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

    private PolledLogger ntInstanceLogger;

    // Publisher topics
    private ProtobufPublisher<ProtobufQuestNavFrameData> frameDataPublisher;
    private ProtobufPublisher<ProtobufQuestNavDeviceData> deviceDataPublisher;
    private ProtobufPublisher<ProtobufQuestNavCommandResponse> commandResponsePublisher;

    // Subscriber topics
    private ProtobufSubscriber<ProtobufQuestNavCommand> commandRequestSubscriber;

    // Ready state variables
    private bool teamNumberSet = false;
    private bool ipAddressSet = false;
    #endregion

    public NetworkTableConnection()
    {
        // Instantiate instance
        ntInstance = new NtInstance(QuestNavConstants.Topics.NT_BASE_PATH);

        // Instantiate logger
        ntInstanceLogger = ntInstance.CreateLogger(
            QuestNavConstants.Logging.NT_LOG_LEVEL_MIN,
            QuestNavConstants.Logging.NT_LOG_LEVEL_MAX
        );

        // Instantiate publisher topics
        frameDataPublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavFrameData>(
            QuestNavConstants.Topics.FRAME_DATA,
            "questnav.protos.data.ProtobufQuestNavFrameData",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );
        deviceDataPublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavDeviceData>(
            QuestNavConstants.Topics.DEVICE_DATA,
            "questnav.protos.data.ProtobufQuestNavDeviceData",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );
        commandResponsePublisher = ntInstance.GetProtobufPublisher<ProtobufQuestNavCommandResponse>(
            QuestNavConstants.Topics.COMMAND_RESPONSE,
            "questnav.protos.commands.ProtobufQuestNavCommandResponse",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
        );

        // Instantiate subscriber topics
        commandRequestSubscriber = ntInstance.GetProtobufSubscriber<ProtobufQuestNavCommand>(
            QuestNavConstants.Topics.COMMAND_REQUEST,
            "questnav.protos.commands.ProtobufQuestNavCommand",
            QuestNavConstants.Network.NT_PUBLISHER_SETTINGS
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
    /// Updates the team number and restarts the connection.
    /// </summary>
    /// <param name="teamNumber">The new team number</param>
    public void UpdateTeamNumber(int teamNumber)
    {
        // Set team number/ip if in debug mode
        if (QuestNavConstants.Network.DEBUG_NT_SERVER_ADDRESS_OVERRIDE.Length == 0)
        {
            QueuedLogger.Log($"Setting Team number to {teamNumber}");
            ntInstance.SetTeamNumber(teamNumber);
            teamNumberSet = true;
        }
        else
        {
            QueuedLogger.Log(
                "Running with NetworkTables IP Override! This should only be used for debugging!"
            );
            ntInstance.SetAddresses(
                new (string addr, int port)[]
                {
                    (
                        QuestNavConstants.Network.DEBUG_NT_SERVER_ADDRESS_OVERRIDE,
                        QuestNavConstants.Network.NT_SERVER_PORT
                    ),
                }
            );
            ipAddressSet = true;
        }
    }
    #endregion

    #region Data Publishing Methods

    private readonly ProtobufQuestNavFrameData frameData = new();

    /// <summary>
    /// Publishes current frame data to NetworkTables
    /// </summary>
    public void PublishFrameData(
        int frameCount,
        double timeStamp,
        Vector3 position,
        Quaternion rotation
    )
    {
        frameData.FrameCount = frameCount;
        frameData.Timestamp = timeStamp;
        frameData.Pose3D = Conversions.UnityToFrc3d(position, rotation);

        // Publish data
        frameDataPublisher.Set(frameData);
    }

    private readonly ProtobufQuestNavDeviceData deviceData = new();

    /// <summary>
    /// Publishes current device data to NetworkTables
    /// </summary>
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
    ///  Default command when no command is sent
    /// </summary>
    private readonly ProtobufQuestNavCommand defaultCommand = new()
    {
        Type = QuestNavCommandType.CommandTypeUnspecified,
        CommandId = 0,
    };

    public ProtobufQuestNavCommand GetCommandRequest()
    {
        return commandRequestSubscriber.Get(defaultCommand);
    }

    public void SetCommandResponse(ProtobufQuestNavCommandResponse response)
    {
        commandResponsePublisher.Set(response);
    }

    #endregion

    #region Logging

    public void LoggerPeriodic()
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
