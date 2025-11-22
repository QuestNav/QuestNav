using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Collects Unity log messages for display in the web interface.
    /// Maintains a circular buffer of the 500 most recent log entries.
    /// Subscribes to Application.logMessageReceived for real-time capture.
    /// Served via /api/logs endpoint with filtering and export capabilities.
    /// Must be initialized on main thread before ConfigServer starts.
    /// Implemented as a singleton MonoBehaviour for lifecycle management.
    /// </summary>
    public class LogCollector : MonoBehaviour
    {
        #region Constants
        /// <summary>
        /// Maximum number of logs to keep in memory
        /// </summary>
        private const int MAX_LOGS = 500;
        #endregion

        #region Static Instance
        /// <summary>
        /// Singleton instance of LogCollector
        /// </summary>
        private static LogCollector instance;

        /// <summary>
        /// Gets the singleton instance of LogCollector.
        /// Creates a new instance if one doesn't exist.
        /// </summary>
        public static LogCollector Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("LogCollector");
                    instance = go.AddComponent<LogCollector>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Fields
        /// <summary>
        /// Circular buffer queue of log entries
        /// </summary>
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();
        #endregion

        #region Nested Types
        /// <summary>
        /// Represents a single Unity log entry with timestamp.
        /// Uses properties for proper JSON.NET serialization.
        /// </summary>
        [Serializable]
        public class LogEntry
        {
            /// <summary>
            /// The log message content
            /// </summary>
            public string message { get; set; }

            /// <summary>
            /// Stack trace (for errors and exceptions)
            /// </summary>
            public string stackTrace { get; set; }

            /// <summary>
            /// Log type (Log, Warning, Error, Assert, Exception)
            /// </summary>
            public string type { get; set; }

            /// <summary>
            /// Unix timestamp in milliseconds when log was created
            /// </summary>
            public long timestamp { get; set; }
        }
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the log collector and subscribes to Unity log events.
        /// Ensures singleton pattern is maintained.
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

            // Subscribe to Unity log callbacks
            Application.logMessageReceived += HandleLog;
        }

        /// <summary>
        /// Unsubscribes from Unity log events when destroyed.
        /// Ensures no memory leaks from event handlers.
        /// </summary>
        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles log messages from Unity's logging system.
        /// Adds new entries to circular buffer and maintains size limit.
        /// Thread-safe implementation using lock for queue access.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="stackTrace">Stack trace (for errors)</param>
        /// <param name="type">Type of log message</param>
        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type.ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            lock (logQueue)
            {
                logQueue.Enqueue(entry);

                // Maintain circular buffer - keep only the most recent logs
                while (logQueue.Count > MAX_LOGS)
                {
                    logQueue.Dequeue();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the most recent log entries.
        /// Returns logs in chronological order (oldest first).
        /// </summary>
        /// <param name="count">Maximum number of logs to return (default: 100)</param>
        /// <returns>List of recent log entries</returns>
        public List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (logQueue)
            {
                var logs = new List<LogEntry>(logQueue);

                // Return most recent logs (up to count)
                int startIndex = Math.Max(0, logs.Count - count);
                return logs.GetRange(startIndex, logs.Count - startIndex);
            }
        }

        /// <summary>
        /// Clears all collected logs.
        /// Useful for resetting log history via web interface.
        /// </summary>
        public void ClearLogs()
        {
            lock (logQueue)
            {
                logQueue.Clear();
            }
        }
        #endregion
    }
}
