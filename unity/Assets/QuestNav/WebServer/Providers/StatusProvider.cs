using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Provides access to runtime status information for the web interface.
    /// Singleton that collects headset pose, tracking, battery, and network status.
    /// Updated at 3Hz from QuestNav.SlowUpdate() and served via /api/status endpoint.
    /// Must be initialized on main thread before ConfigServer starts.
    /// Implemented as a singleton MonoBehaviour for lifecycle management.
    /// </summary>
    public class StatusProvider : MonoBehaviour
    {
        #region Static Instance
        /// <summary>
        /// Singleton instance of StatusProvider
        /// </summary>
        private static StatusProvider instance;

        /// <summary>
        /// Gets the singleton instance of StatusProvider.
        /// Creates a new instance if one doesn't exist.
        /// </summary>
        public static StatusProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("StatusProvider");
                    instance = go.AddComponent<StatusProvider>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Fields
        /// <summary>
        /// Current position of VR headset
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Current rotation of VR headset
        /// </summary>
        private Quaternion rotation;

        /// <summary>
        /// Whether headset is currently tracking
        /// </summary>
        private bool isTracking;

        /// <summary>
        /// Number of tracking loss events this session
        /// </summary>
        private int trackingLostEvents;

        /// <summary>
        /// Current battery level (0.0 to 1.0)
        /// </summary>
        private float batteryLevel;

        /// <summary>
        /// Current battery charging status
        /// </summary>
        private BatteryStatus batteryStatus;

        /// <summary>
        /// Whether connected to NetworkTables
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// Current IP address of headset
        /// </summary>
        private string ipAddress = "0.0.0.0";

        /// <summary>
        /// Current team number for connection
        /// </summary>
        private int teamNumber;

        /// <summary>
        /// Current frames per second
        /// </summary>
        private float fps;

        /// <summary>
        /// Unity frame count
        /// </summary>
        private int frameCount;
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the status provider and ensures singleton pattern.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Updates performance metrics (FPS and frame count).
        /// Called every frame by Unity.
        /// </summary>
        private void Update()
        {
            // Update FPS calculation
            fps = 1f / Time.deltaTime;
            frameCount = Time.frameCount;
        }
        #endregion

        #region Public Update Methods
        /// <summary>
        /// Updates the current headset pose data.
        /// Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="pos">Current position of VR headset</param>
        /// <param name="rot">Current rotation of VR headset</param>
        public void UpdatePose(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }

        /// <summary>
        /// Updates the current tracking status.
        /// Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="tracking">Whether headset is currently tracking</param>
        /// <param name="lostEvents">Number of tracking loss events this session</param>
        public void UpdateTracking(bool tracking, int lostEvents)
        {
            isTracking = tracking;
            trackingLostEvents = lostEvents;
        }

        /// <summary>
        /// Updates the current battery status.
        /// Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="level">Battery level (0.0 to 1.0)</param>
        /// <param name="status">Battery charging status</param>
        public void UpdateBattery(float level, BatteryStatus status)
        {
            batteryLevel = level;
            batteryStatus = status;
        }

        /// <summary>
        /// Updates the current network connection status.
        /// Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="connected">Whether connected to NetworkTables</param>
        /// <param name="ip">Current IP address of headset</param>
        /// <param name="team">Current team number for connection</param>
        public void UpdateNetwork(bool connected, string ip, int team)
        {
            isConnected = connected;
            ipAddress = ip;
            teamNumber = team;
        }
        #endregion

        #region Public Query Methods
        /// <summary>
        /// Gets current status as an object for JSON serialization.
        /// Called from ConfigServer background thread via /api/status endpoint.
        /// Returns anonymous object suitable for JSON.NET serialization.
        /// </summary>
        /// <returns>Status data object with all current values</returns>
        public object GetStatus()
        {
            var eulerAngles = rotation.eulerAngles;

            return new
            {
                // Pose
                position = new
                {
                    x = position.x,
                    y = position.y,
                    z = position.z,
                },
                rotation = new
                {
                    x = rotation.x,
                    y = rotation.y,
                    z = rotation.z,
                    w = rotation.w,
                },
                eulerAngles = new
                {
                    pitch = eulerAngles.x,
                    yaw = eulerAngles.y,
                    roll = eulerAngles.z,
                },

                // Tracking
                isTracking = isTracking,
                trackingLostEvents = trackingLostEvents,

                // Battery
                batteryPercent = (int)(batteryLevel * 100),
                batteryLevel = batteryLevel,
                batteryStatus = batteryStatus.ToString(),
                batteryCharging = batteryStatus == BatteryStatus.Charging,

                // Network
                networkConnected = isConnected,
                ipAddress = ipAddress,
                teamNumber = teamNumber,

                // Performance
                fps = Mathf.Round(fps),
                frameCount = frameCount,

                // Timestamp
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
        }
        #endregion
    }
}
