using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Unity implementation of ILogger that forwards log messages to Unity's Debug system.
    /// Safe to use from ConfigBootstrap (MonoBehaviour) on the main thread.
    /// This adapter allows ConfigServer (running on background thread) to safely log
    /// messages to Unity's Debug console via the ILogger interface.
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        /// Logs an informational message to Unity console
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Logs a warning message to Unity console
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message to Unity console
        /// </summary>
        /// <param name="message">The error message to log</param>
        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
