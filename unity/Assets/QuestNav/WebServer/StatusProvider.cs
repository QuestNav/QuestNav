using UnityEngine;

namespace QuestNav.Config
{
    /// <summary>
    /// Provides access to runtime status information for the web interface.
    /// Singleton that collects headset pose, tracking, battery, and network status.
    /// Updated at 3Hz from QuestNav.SlowUpdate() and served via /api/status endpoint.
    /// Must be initialized on main thread before ConfigServer starts.
    /// </summary>
    public class StatusProvider : MonoBehaviour
    {
        private static StatusProvider instance;

        // Headset Status
        private Vector3 position;
        private Quaternion rotation;
        private bool isTracking;
        private int trackingLostEvents;
        private float batteryLevel;
        private BatteryStatus batteryStatus;

        // Network Status
        private bool isConnected;
        private string ipAddress = "0.0.0.0";
        private int teamNumber;

        // Performance
        private float fps;
        private int frameCount;

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

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Update FPS
            fps = 1f / Time.deltaTime;
            frameCount = Time.frameCount;
        }

        // Public methods to update status
        public void UpdatePose(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }

        public void UpdateTracking(bool tracking, int lostEvents)
        {
            isTracking = tracking;
            trackingLostEvents = lostEvents;
        }

        public void UpdateBattery(float level, BatteryStatus status)
        {
            batteryLevel = level;
            batteryStatus = status;
        }

        public void UpdateNetwork(bool connected, string ip, int team)
        {
            isConnected = connected;
            ipAddress = ip;
            teamNumber = team;
        }

        // Get current status as an object for JSON serialization
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
    }
}
