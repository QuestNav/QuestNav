namespace QuestNav.WebServer
{
    /// <summary>
    /// Interface for WebServer management.
    /// Provides lifecycle management for the configuration web server and related services.
    /// Designed to be initialized via dependency injection from QuestNav.cs.
    /// </summary>
    public interface IWebServerManager
    {
        /// <summary>
        /// Gets whether the HTTP server is currently running
        /// </summary>
        bool IsServerRunning { get; }

        /// <summary>
        /// Gets the base URL of the server
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// Initializes the web server system.
        /// Must be called on Unity main thread during application startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Periodic update method for web server operations.
        /// Should be called from QuestNav.SlowUpdate() at 3Hz.
        /// Handles pending operations that need to run on the main thread.
        /// </summary>
        void Periodic();

        /// <summary>
        /// Updates status information for the web interface.
        /// Should be called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="position">Current VR headset position (FRC coordinates)</param>
        /// <param name="rotation">Current VR headset rotation (FRC coordinates)</param>
        /// <param name="isTracking">Whether headset tracking is active</param>
        /// <param name="trackingLostEvents">Number of tracking loss events this session</param>
        /// <param name="batteryLevel">Battery level (0.0 to 1.0)</param>
        /// <param name="batteryStatus">Battery charging status</param>
        /// <param name="isConnected">Whether connected to NetworkTables</param>
        /// <param name="ipAddress">Current IP address of headset</param>
        /// <param name="teamNumber">Current team number</param>
        /// <param name="robotIpAddress">Robot IP address</param>
        /// <param name="fps">Current frames per second</param>
        /// <param name="frameCount">Unity frame count</param>
        void UpdateStatus(
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            bool isTracking,
            int trackingLostEvents,
            float batteryLevel,
            UnityEngine.BatteryStatus batteryStatus,
            bool isConnected,
            string ipAddress,
            int teamNumber,
            string robotIpAddress,
            float fps,
            int frameCount
        );

        /// <summary>
        /// Stops the web server and cleans up resources.
        /// Should be called on application shutdown.
        /// </summary>
        void Shutdown();
    }
}
