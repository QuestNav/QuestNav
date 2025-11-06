using QuestNav.Commands;
using QuestNav.Config;
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
        /// posXText text
        /// </summary>
        [SerializeField]
        private TMP_Text posXText;

        /// <summary>
        /// posYText text
        /// </summary>
        [SerializeField]
        private TMP_Text posYText;

        /// <summary>
        /// posZText text
        /// </summary>
        [SerializeField]
        private TMP_Text posZText;

        /// <summary>
        /// X rotation text
        /// </summary>
        [SerializeField]
        private TMP_Text xRotText;

        /// <summary>
        /// Y rotation text
        /// </summary>
        [SerializeField]
        private TMP_Text yRotText;

        /// <summary>
        /// Z rotation text
        /// </summary>
        [SerializeField]
        private TMP_Text zRotText;

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
                posXText,
                posYText,
                posZText,
                xRotText,
                yRotText,
                zRotText,
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
        /// Main update loop that runs at high frequency (100Hz) for time-critical operations.
        /// This is the core of the QuestNav system, responsible for:
        /// 1. Capturing VR headset pose data (position/rotation) from the Oculus SDK
        /// 2. Converting Unity coordinates to FRC field coordinates
        /// 3. Publishing pose data to NetworkTables for robot consumption
        /// 4. Processing incoming commands from the robot (pose resets, etc.)
        ///
        /// Performance Note: This runs 100 times per second, so all operations here
        /// must be lightweight. Heavy operations should go in SlowUpdate().
        /// </summary>
        private void MainUpdate()
        {
            // Collect current VR headset pose data from Oculus tracking system
            // This includes position (x,y,z) and rotation (quaternion) in Unity world space
            UpdateFrameData();

            // Convert Unity coordinates to FRC field coordinates and publish to NetworkTables
            // The robot subscribes to this data to know where the headset is on the field
            networkTableConnection.PublishFrameData(frameCount, timeStamp, position, rotation);

            // Check for and execute any pending commands from the robot
            // Commands include pose resets, calibration requests, etc.
            commandProcessor.ProcessCommands();
        }

        /// <summary>
        /// Slower update loop that runs at 3Hz for non-critical operations.
        /// This handles expensive operations that don't need to run every frame:
        /// 1. NetworkTables internal logging and diagnostics
        /// 2. UI updates (connection status, IP address, team number display)
        /// 3. Device health monitoring (tracking status, battery level)
        /// 4. Log message processing and output to Unity console
        ///
        /// Design Rationale: Running these operations at 3Hz instead of 100Hz
        /// significantly reduces CPU overhead while maintaining responsive UI updates.
        /// </summary>
        private void SlowUpdate()
        {
            // Process and display NetworkTables internal messages (connection status, errors, etc.)
            // This helps with debugging connection issues without impacting performance
            networkTableConnection.LoggerPeriodic();

            // Update UI elements like connection status, IP address display, team number validation
            // UI updates don't need to be real-time, 3Hz provides smooth visual feedback
            uiManager.UIPeriodic();
            uiManager.UpdatePositionText(position, rotation);

            // Monitor device health: tracking status, battery level, tracking loss events
            // This data helps diagnose issues but doesn't need high-frequency updates
            UpdateDeviceData();
            networkTableConnection.PublishDeviceData(
                currentlyTracking,
                trackingLostEvents,
                batteryPercent
            );

            // Update status provider for web interface
            UpdateStatusProvider();

            // Flush queued log messages to Unity console
            // Batching log output improves performance and reduces console spam
            QueuedLogger.Flush();
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Captures the current VR headset pose data from the Oculus tracking system.
        ///
        /// This function extracts:
        /// - Frame count: Unity's internal frame counter for data synchronization
        /// - Timestamp: Unity's time since startup for temporal correlation
        /// - Position: 3D world position of the headset's center eye point in Unity coordinates
        /// - Rotation: Quaternion representing the headset's orientation in Unity coordinates
        ///
        /// Technical Details:
        /// - Uses OVRCameraRig.centerEyeAnchor which provides the averaged position between left/right eyes
        /// - Position is in Unity world space (meters), with Y-up coordinate system
        /// - Rotation quaternion represents the headset's orientation relative to the tracking origin
        /// - This data will be converted to FRC field coordinates before transmission to the robot
        ///
        /// Performance: This is called 100 times per second, so it uses direct property access
        /// rather than more expensive operations like transforms or calculations.
        /// </summary>
        private void UpdateFrameData()
        {
            // Unity's frame counter - useful for detecting dropped frames or synchronization issues
            frameCount = Time.frameCount;

            // Time since Unity startup in seconds - provides temporal correlation for robot code
            timeStamp = Time.time;

            // Get the center eye position - this is the averaged position between left and right eyes
            // This represents the "head" position that the robot should track
            position = cameraRig.centerEyeAnchor.position;

            // Get the headset orientation as a quaternion
            // This includes pitch (looking up/down), yaw (turning left/right), and roll (tilting head)
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
#if UNITY_ANDROID && !UNITY_EDITOR
            currentlyTracking = OVRManager.tracker.isPositionTracked;

            // Increment the tracking loss counter if we have tracking loss
            if (!currentlyTracking && hadTracking)
            {
                trackingLostEvents++;
                QueuedLogger.LogWarning($"Tracking Lost! Times this session: {trackingLostEvents}");
            }

            hadTracking = currentlyTracking;
#endif
        }

        /// <summary>
        /// Updates the status provider with current runtime data for the web interface
        /// </summary>
        private void UpdateStatusProvider()
        {
            var statusProvider = StatusProvider.Instance;
            statusProvider.UpdatePose(position, rotation);
            statusProvider.UpdateTracking(currentlyTracking, trackingLostEvents);
            statusProvider.UpdateBattery(SystemInfo.batteryLevel, SystemInfo.batteryStatus);
            statusProvider.UpdateNetwork(
                networkTableConnection.IsConnected,
                uiManager.IPAddress,
                uiManager.TeamNumber
            );
        }
        #endregion
    }
}
