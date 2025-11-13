using UnityEngine;

namespace QuestNav.Config
{
    /// <summary>
    /// Unity implementation of ILogger that forwards log messages to Unity's Debug system.
    /// Safe to use from ConfigBootstrap (MonoBehaviour) on the main thread.
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>Logs an informational message to Unity console.</summary>
        public void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>Logs a warning message to Unity console.</summary>
        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>Logs an error message to Unity console.</summary>
        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
