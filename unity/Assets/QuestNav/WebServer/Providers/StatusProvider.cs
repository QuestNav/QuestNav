using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Provides access to runtime status information for the web interface.
    /// Collects headset pose, tracking, battery, and network status.
    /// Updated at 3Hz from QuestNav.SlowUpdate() and served via /api/status endpoint.
    /// Plain C# class designed for dependency injection.
    /// Thread-safe for concurrent access from main thread (UpdateStatus) and background thread (GetStatus).
    /// </summary>
    public class StatusProvider
    {
        #region Fields
        /// <summary>
        /// Lock object for thread-safe access to status fields
        /// </summary>
        private readonly object statusLock = new object();

        /// <summary>
        /// Current position of VR headset (FRC coordinates)
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Current rotation of VR headset (FRC coordinates)
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
        /// Current robot IP address (resolved from team number or debug override)
        /// </summary>
        private string robotIpAddress = "";

        /// <summary>
        /// Current frames per second
        /// </summary>
        private float fps;

        /// <summary>
        /// Unity frame count
        /// </summary>
        private int frameCount;

        /// <summary>
        /// Number of connected web clients
        /// </summary>
        private int connectedClients;

        /// <summary>
        /// Whether the camera arbiter currently has a high-priority reservation.
        /// True when the AprilTag detector is enabled and pinning the camera resolution.
        /// </summary>
        private bool passthroughResolutionLockedByAprilTag;

        /// <summary>
        /// The resolution actually being applied to the camera, or null if disabled.
        /// May differ from the passthrough stream's requested resolution when locked.
        /// </summary>
        private Vector2Int? effectivePassthroughResolution;
        #endregion

        #region Public Update Methods
        /// <summary>
        /// Updates all status information at once.
        /// Called from WebServerManager.UpdateStatus() which is invoked from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="pos">Current position of VR headset (FRC coordinates)</param>
        /// <param name="rot">Current rotation of VR headset (FRC coordinates)</param>
        /// <param name="tracking">Whether headset is currently tracking</param>
        /// <param name="lostEvents">Number of tracking loss events this session</param>
        /// <param name="battery">Battery level (0.0 to 1.0)</param>
        /// <param name="status">Battery charging status</param>
        /// <param name="connected">Whether connected to NetworkTables</param>
        /// <param name="ip">Current IP address of headset</param>
        /// <param name="team">Current team number for connection</param>
        /// <param name="robotIp">Robot IP address (from debug override or calculated from team number)</param>
        /// <param name="currentFps">Current frames per second</param>
        /// <param name="currentFrameCount">Unity frame count</param>
        public void UpdateStatus(
            Vector3 pos,
            Quaternion rot,
            bool tracking,
            int lostEvents,
            float battery,
            BatteryStatus status,
            bool connected,
            string ip,
            int team,
            string robotIp,
            float currentFps,
            int currentFrameCount
        )
        {
            lock (statusLock)
            {
                position = pos;
                rotation = rot;
                isTracking = tracking;
                trackingLostEvents = lostEvents;
                batteryLevel = battery;
                batteryStatus = status;
                isConnected = connected;
                ipAddress = ip;
                teamNumber = team;
                robotIpAddress = robotIp;
                fps = currentFps;
                frameCount = currentFrameCount;
            }
        }

        /// <summary>
        /// Updates the number of connected web clients.
        /// Can be called from ConfigServer background thread.
        /// Thread-safe.
        /// </summary>
        /// <param name="clients">Number of active web clients</param>
        public void UpdateConnectedClients(int clients)
        {
            lock (statusLock)
            {
                connectedClients = clients;
            }
        }

        /// <summary>
        /// Updates the camera arbitration state for the web UI. Called from
        /// <see cref="WebServerManager"/> in response to
        /// <see cref="QuestNav.Camera.CameraResourceManager.OnResolutionChanged"/>.
        /// </summary>
        /// <param name="lockedByAprilTag">True when AprilTag detector holds the camera lock.</param>
        /// <param name="effectiveResolution">Actual camera resolution, or null when disabled.</param>
        public void UpdateCameraStatus(bool lockedByAprilTag, Vector2Int? effectiveResolution)
        {
            lock (statusLock)
            {
                passthroughResolutionLockedByAprilTag = lockedByAprilTag;
                this.effectivePassthroughResolution = effectiveResolution;
            }
        }
        #endregion

        #region Public Query Methods
        /// <summary>
        /// Gets current status as an object for JSON serialization.
        /// Called from ConfigServer background thread via /api/status endpoint.
        /// Returns anonymous object suitable for JSON.NET serialization.
        /// Position and rotation are provided in FRC robot coordinates.
        /// Thread-safe.
        /// </summary>
        /// <returns>Status data object with all current values</returns>
        public object GetStatus()
        {
            lock (statusLock)
            {
                var eulerAngles = rotation.eulerAngles;

                return new
                {
                    // Pose (in FRC coordinates, converted by QuestNav before passing)
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
                        pitch = eulerAngles.z,
                        yaw = eulerAngles.y,
                        roll = eulerAngles.x,
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
                    robotIpAddress = robotIpAddress,

                    // Performance
                    fps = Mathf.Round(fps),
                    frameCount = frameCount,

                    // Web Interface
                    connectedClients = connectedClients,

                    // Camera arbitration (AprilTag wins over passthrough stream)
                    passthroughResolutionLockedByAprilTag = passthroughResolutionLockedByAprilTag,
                    effectivePassthroughResolution = effectivePassthroughResolution.HasValue
                        ? (object)
                            new
                            {
                                width = effectivePassthroughResolution.Value.x,
                                height = effectivePassthroughResolution.Value.y,
                            }
                        : null,

                    // Timestamp
                    timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
            }
        }
        #endregion
    }
}
