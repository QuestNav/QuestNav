using System;
using QuestNav.Core;
using QuestNav.Native.NTCore;
using QuestNav.Network;
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
        /// Publishes frame data to NetworkTables.
        /// </summary>
        /// <param name="frameIndex">Current frame index</param>
        /// <param name="timeStamp">Current timestamp</param>
        /// <param name="position">Current position</param>
        /// <param name="rotation">Current rotation</param>
        /// <param name="eulerAngles">Current euler angles</param>
        void PublishFrameData(int frameIndex, double timeStamp, Vector3 position, Quaternion rotation,
            Vector3 eulerAngles);

        /// <summary>
        /// Publishes device data to NetworkTables.
        /// </summary>
        /// <param name="currentlyTracking">Is the quest tracking currently</param>
        /// <param name="trackingLostEvents">Number of tracking lost events this session</param>
        /// <param name="batteryPercent">Current battery percentage</param>
        void PublishDeviceData(bool currentlyTracking, int trackingLostEvents, float batteryPercent);
        
        /// <summary>
        /// Updates the team number.
        /// </summary>
        /// <param name="teamNumber">The team number</param>
        void UpdateTeamNumber(int teamNumber);

        public long GetCommandRequest();

        public float[] GetPoseResetPosition();

        public void SetCommandResponse(long response);
    }

    /// <summary>
    /// Enum representing the possible connection states.
    /// </summary>
    public enum ConnectionState
    {
        NONE,
        INITIALIZING,
        ATTEMPTING_CONNECTION,
        CHECKING_NETWORK_REACHABILITY,
        NETWORK_UNREACHABLE,
        RESOLVING_ADDRESS,
        ADDRESS_RESOLUTION_FAILED,
        CONNECTING_TO_ADDRESS,
        CONNECTION_FAILED,
        CONNECTION_TIMEOUT,
        CONNECTED,
        DISCONNECTED,
        RECONNECT_DELAY,
        FORCING_RECONNECTION
    }
    }

    /// <summary>
    /// Manages NetworkTables connections for communication with an FRC robot.
    /// </summary>
    public class NetworkTableConnection : INetworkTableConnection
    {
        /// <summary>
        /// NetworkTables connection for FRC data communication
        /// </summary>
        private NtInstance ntInstance;
        
        // Publisher topics
        private IntegerPublisher frameCountPublisher;
        private DoublePublisher timestampPublisher;
        private FloatArrayPublisher positionPublisher;
        private FloatArrayPublisher quaternionPublisher;
        private FloatArrayPublisher eulerAnglesPublisher;
        private IntegerPublisher trackingLostPublisher;
        private IntegerPublisher currentlyTrackingPublisher;
        private IntegerPublisher batteryPercentPublisher;
        private IntegerPublisher commandResponsePublisher;
        
        // Subscriber topics
        private IntegerSubscriber commandRequestSubscriber;
        private FloatArraySubscriber poseResetSubscriber;
        
        public NetworkTableConnection()
        {
            // Instantiate instance
            ntInstance = new NtInstance("questnav");
            // Instantiate publisher topics
            frameCountPublisher = ntInstance.GetIntegerPublisher(QuestNavConstants.Topics.FRAME_COUNT, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            timestampPublisher = ntInstance.GetDoublePublisher(QuestNavConstants.Topics.TIMESTAMP, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            positionPublisher = ntInstance.GetFloatArrayPublisher(QuestNavConstants.Topics.POSITION, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            quaternionPublisher = ntInstance.GetFloatArrayPublisher(QuestNavConstants.Topics.QUATERNION, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            eulerAnglesPublisher = ntInstance.GetFloatArrayPublisher(QuestNavConstants.Topics.EULER_ANGLES, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            trackingLostPublisher = ntInstance.GetIntegerPublisher(QuestNavConstants.Topics.TRACKING_LOST_COUNTER,
                QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            currentlyTrackingPublisher = ntInstance.GetIntegerPublisher(QuestNavConstants.Topics.CURRENTLY_TRACKING,
                QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            batteryPercentPublisher = ntInstance.GetIntegerPublisher(QuestNavConstants.Topics.BATTERY_PERCENT,
                QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            commandResponsePublisher = ntInstance.GetIntegerPublisher(QuestNavConstants.Topics.COMMAND_RESPONSE, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            
            // Instantiate subscriber topics
            commandRequestSubscriber = ntInstance.GetIntegerSubscriber(QuestNavConstants.Topics.COMMAND_REQUEST, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
            poseResetSubscriber = ntInstance.GetFloatArraySubscriber(QuestNavConstants.Topics.RESET_POSE, QuestNavConstants.Network.NT_PUBLISHER_SETTINGS);
        }

        #region Properties

        /// <summary>
        /// Gets whether the connection is currently established.
        /// </summary>
        public bool IsConnected => ntInstance.IsConnected();
        
        /// <summary>
        /// Updates the team number and restarts the connection.
        /// </summary>
        /// <param name="teamNumber">The new team number</param>
        public void UpdateTeamNumber(int teamNumber)
        {
            ntInstance.SetTeamNumber(teamNumber);
        }
        #endregion

        #region Data Publishing Methods

        /// <summary>
        /// Publishes current frame data to NetworkTables
        /// </summary>
        public void PublishFrameData(int frameIndex, double timeStamp, Vector3 position, Quaternion rotation,
            Vector3 eulerAngles)
        {
            // Check if connection is established before publishing data
            if (!ntInstance.IsConnected())
            {
                return; // Exit early if we aren't connected
            }

            // Publish frame count and timestamp
            frameCountPublisher?.Set(frameIndex);
            timestampPublisher?.Set(timeStamp);

            // Publish position as float array
            positionPublisher?.Set(new[] { position.x, position.y, position.z });

            // Publish quaternion as float array
            quaternionPublisher?.Set(new[] { rotation.x, rotation.y, rotation.z, rotation.w });

            // Publish euler angles as float array
            eulerAnglesPublisher?.Set(new[] { eulerAngles.x, eulerAngles.y, eulerAngles.z });
        }

        public void PublishDeviceData(bool currentlyTracking, int trackingLostEvents, float batteryPercent)
        {
            // Check if connection is established before publishing data
            if (!ntInstance.IsConnected())
            {
                return; // Exit early if connection isn't established
            }

            // Publish tracking lost events counter
            trackingLostPublisher?.Set(trackingLostEvents);

            // Publish currently tracking
            currentlyTrackingPublisher?.Set(currentlyTracking ? 1 : 0);
            
            // Publish battery percent
            batteryPercentPublisher?.Set((int) batteryPercent);
        }

        #endregion

        #region Command Processing

        public long GetCommandRequest()
        {
            return commandRequestSubscriber.Get(QuestNavConstants.Commands.IDLE);
        }
        
        public float[] GetPoseResetPosition()
        {
            return poseResetSubscriber.Get(new []{0.0f, 0.0f, 0.0f});
        }

        public void SetCommandResponse(long response)
        {
            commandResponsePublisher.Set(response);
        }

        #endregion
    }