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
        private int frameCount;

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
        /// Checkbox for auto start on boot
        /// </summary>
        [SerializeField]
        private Toggle autoStartToggle;

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
        private int batteryPercent;

        /// <summary>
        /// Counter for display update delay
        /// </summary>
        private int delayCounter;

        /// <summary>
        /// Increments once every time tracking is lost after having it acquired
        /// </summary>
        private int trackingLostEvents;

        ///<summary>
        /// Whether we have tracking
        /// </summary>
        private bool currentlyTracking;

        ///<summary>
        /// Whether we had tracking
        /// </summary>
        private bool hadTracking;

        #region Component References

        /// <summary>
        /// Reference to the network table connection component
        /// </summary>
        private NetworkTableConnection networkTableConnection;

        /// <summary>
        /// Reference to the command processor component
        /// </summary>
        private CommandProcessor commandProcessor;

        /// <summary>
        /// Reference to the UI manager component
        /// </summary>
        private UIManager uiManager;
        #endregion
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the connection and UI components
        /// </summary>
        private void Awake()
        {
            // Initializes components
            networkTableConnection = new NetworkTableConnection();
            commandProcessor = new CommandProcessor(
                networkTableConnection,
                vrCamera,
                vrCameraRoot,
                resetTransform
            );
            uiManager = new UIManager(
                networkTableConnection,
                teamInput,
                ipAddressText,
                conStateText,
                teamUpdateButton,
                autoStartToggle
            );

            // Set Oculus display frequency
            OVRPlugin.systemDisplayFrequency = QuestNavConstants.Display.DISPLAY_FREQUENCY;
            // Schedule "SlowUpdate" loop for non loop critical applications
            InvokeRepeating(nameof(SlowUpdate), 0, 1f / QuestNavConstants.Timing.SLOW_UPDATE_HZ);
            InvokeRepeating(nameof(MainUpdate), 0, 1f / QuestNavConstants.Timing.MAIN_UPDATE_HZ);
        }

        /// <summary>
        /// Handles frame updates for data publishing and command processing
        /// </summary>
        private void MainUpdate()
        {
            // Collect and publish current frame data
            UpdateFrameData();
            networkTableConnection.PublishFrameData(frameCount, timeStamp, position, rotation);

            // Process robot commands
            commandProcessor.ProcessCommands();
        }

        /// <summary>
        /// This loop runs slower for performance reasons. Expensive methods that aren't loop critical
        /// should be placed here (e.g. logging)
        /// </summary>
        private void SlowUpdate()
        {
            // Log internal NetworkTable info
            networkTableConnection.LoggerPeriodic();

            // Update UI periodically
            uiManager.UIPeriodic();

            // Collect and publish current device data at a slower rate
            UpdateDeviceData();
            networkTableConnection.PublishDeviceData(
                currentlyTracking,
                trackingLostEvents,
                batteryPercent
            );

            // Flush logs
            QueuedLogger.Flush();
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the current frame data from the VR headset
        /// </summary>
        private void UpdateFrameData()
        {
            frameCount = Time.frameCount;
            timeStamp = Time.time;
            position = cameraRig.centerEyeAnchor.position;
            rotation = cameraRig.centerEyeAnchor.rotation;
        }

        /// <summary>
        /// Updates the current device data from the VR headset
        /// </summary>
        private void UpdateDeviceData()
        {
            CheckTrackingLoss();
            batteryPercent = (int)(SystemInfo.batteryLevel * 100);
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
