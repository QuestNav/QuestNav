using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Interface for WebServer management.
    /// Provides lifecycle management for the configuration web server.
    /// </summary>
    public interface IWebServerManager
    {
        /// <summary>
        /// Gets whether the HTTP server is currently running
        /// </summary>
        bool IsServerRunning { get; }

        /// <summary>
        /// Initializes the web server system.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Periodic update method. Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        /// <param name="position">Current VR headset position (FRC coordinates)</param>
        /// <param name="rotation">Current VR headset rotation (FRC coordinates)</param>
        /// <param name="isTracking">Whether headset tracking is active</param>
        /// <param name="trackingLostEvents">Number of tracking loss events</param>
        void Periodic(
            Vector3 position,
            Quaternion rotation,
            bool isTracking,
            int trackingLostEvents
        );

        /// <summary>
        /// Stops the web server and cleans up resources.
        /// </summary>
        void Shutdown();
    }
}
