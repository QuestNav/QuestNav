using QuestNav.Commands;
using QuestNav.Network;
using QuestNav.UI;
using QuestNav.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestNav.Core
{
    /// <summary>
    /// Main controller class for QuestNav application.
    /// Manages streaming of VR motion data to a FRC robot through NetworkTables.
    /// Orchestrates pose tracking, reset operations, and network communication.
    /// </summary>
    public class QuestNav : MonoBehaviour
    {
        #region Fields
        /// <summary>
        /// Current frame index from Unity's Time.frameCount
        /// </summary>
        private int frameIndex;

        /// <summary>
        /// Current timestamp from Unity's Time.time
        /// </summary>
        private double timeStamp;

        /// <summary>
        /// Current position of the VR headset
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Current rotation of the VR headset as a Quaternion
        /// </summary>
        private Quaternion rotation;

        /// <summary>
        /// Current rotation of the VR headset in Euler angles
        /// </summary>
        private Vector3 eulerAngles;

        /// <summary>
        /// Reference to the OVR Camera Rig for tracking
        /// </summary>
        [SerializeField]
        private OVRCameraRig cameraRig;

        /// <summary>
        /// Input field for team number entry
        /// </summary>
        [SerializeField]
        private TMP_InputField teamInput;

        /// <summary>
        /// IP address text
        /// </summary>
        [SerializeField]
        private TMP_Text ipAddressText;

        /// <summary>
        /// ConState text
        /// </summary>
        [SerializeField]
        private TMP_Text conStateText;

        /// <summary>
        /// Button to update team number
        /// </summary>
        [SerializeField]
        private Button teamUpdateButton;

        /// <summary>
        /// Button to connect to simulation
        /// </summary>
        [SerializeField]
        private Button connectToSimButton;

        /// <summary>
        /// Reference to the VR camera transform
        /// </summary>
        [SerializeField]
        private Transform vrCamera;

        /// <summary>
        /// Reference to the VR camera root transform
        /// </summary>
        [SerializeField]
        private Transform vrCameraRoot;

        /// <summary>
        /// Reference to the reset position transform
        /// </summary>
        [SerializeField]
        private Transform resetTransform;

        /// <summary>
        /// Current battery percentage of the device
        /// </summary>
        private float batteryPercent;

        /// <summary>
        /// Counter for display update delay
        /// </summary>
        private int delayCounter;
        /// <summary>
        /// Increments once every time tracking is lost after having it aquired
        /// </summary>
        private int trackingLostEvents;

        ///<summary>
        /// Whether we have tracking
        /// </summary>
        private bool currentlyTracking = false;

        ///<summary>
        /// Whether we had tracking
        /// </summary>
        private bool hadTracking = false;

        // Using display frequency constant from QuestNavConstants

        #region Component References
        /// <summary>
        /// Reference to the network table connection component
        /// </summary>
        [SerializeField]
        private NetworkTableConnection networkConnection;

        /// <summary>
        /// Reference to the heartbeat manager component
        /// </summary>
        [SerializeField]
        private HeartbeatManager heartbeatManager;

        /// <summary>
        /// Reference to the command processor component
        /// </summary>
        [SerializeField]
        private CommandProcessor commandProcessor;

        /// <summary>
        /// Reference to the UI manager component
        /// </summary>
        [SerializeField]
        private UIManager uiManager;
        #endregion
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the connection and UI components
        /// </summary>
        void Start()
        {
            // Set Oculus display frequency
            OVRPlugin.systemDisplayFrequency = QuestNavConstants.Display.DISPLAY_FREQUENCY;

            // Initialize UI manager
            uiManager.Initialize(teamInput, ipAddressText, conStateText, teamUpdateButton, connectToSimButton, networkConnection);

            // Initialize command processor
            commandProcessor.Initialize(networkConnection, vrCamera, vrCameraRoot, resetTransform);

            // Initialize heartbeat manager
            heartbeatManager.Initialize(networkConnection);

            // Start connection to robot
            networkConnection.ConnectToRobot();
        }

        /// <summary>
        /// Handles frame updates for data publishing and command processing
        /// </summary>
        void LateUpdate()
        {
            // Update UI periodically
            if (delayCounter >= (int)QuestNavConstants.Display.DISPLAY_FREQUENCY)
            {
                uiManager.UpdateIPAddressText();
                uiManager.UpdateConStateText();
                delayCounter = 0;
            }
            else
            {
                delayCounter++;
            }

            // Check for connection attempt timeout to prevent zombie state
            if (!networkConnection.IsConnected)
            {
                // Only start a new connection attempt if not already trying
                if (!networkConnection.IsConnectionAttemptInProgress)
                {
                    networkConnection.HandleDisconnectedState();
                }
                return;
            }

            // Connected - manage heartbeat, publish data, and process commands
            if (networkConnection.IsConnected)
            {
                // Manage heartbeat to detect zombie connections
                heartbeatManager.ManageHeartbeat();

                // Collect and publish current frame data
                UpdateFrameData();
                networkConnection.PublishFrameData(frameIndex, timeStamp, position, rotation, eulerAngles);

                // Collect and publish current device data
                UpdateDeviceData();
                networkConnection.PublishDeviceData(currentlyTracking, trackingLostEvents, batteryPercent);

                // Process robot commands
                commandProcessor.ProcessCommands();
            }


            // Check for tracking loss

        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the current frame data from the VR headset
        /// </summary>
        private void UpdateFrameData()
        {
            frameIndex = Time.frameCount;
            timeStamp = Time.time;
            position = cameraRig.centerEyeAnchor.position;
            rotation = cameraRig.centerEyeAnchor.rotation;
            eulerAngles = cameraRig.centerEyeAnchor.eulerAngles;
        }
        /// <summary>
        /// Updates the current device data from the VR headset
        /// </summary>
        private void UpdateDeviceData()
        {
            CheckTrackingLoss();
            batteryPercent = SystemInfo.batteryLevel * 100;
        }
        /// <summary>
        /// Checks to see if tracking is lost, and increments a counter if so
        /// </summary>
        private void CheckTrackingLoss()
        {
            currentlyTracking = OVRManager.tracker.isPositionTracked;

            // Increment the tracking loss counter if we have tracking loss
            if (!currentlyTracking && hadTracking)
            {
                trackingLostEvents++;
                QueuedLogger.LogWarning($"Tracking Lost! Times this session: {trackingLostEvents}");
            }

            hadTracking = currentlyTracking;
        }
        #endregion
    }
}