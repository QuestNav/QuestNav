using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuestNav.Config
{
    /// <summary>
    /// Collects Unity log messages for display in the web interface.
    /// Maintains a circular buffer of 500 most recent log entries.
    /// Subscribes to Application.logMessageReceived for real-time capture.
    /// Served via /api/logs endpoint with filtering and export capabilities.
    /// Must be initialized on main thread before ConfigServer starts.
    /// </summary>
    public class LogCollector : MonoBehaviour
    {
        private static LogCollector instance;
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();
        private const int MAX_LOGS = 500;

        /// <summary>
        /// Represents a single Unity log entry with timestamp.
        /// Uses properties for proper JSON.NET serialization.
        /// </summary>
        [Serializable]
        public class LogEntry
        {
            public string message { get; set; }
            public string stackTrace { get; set; }
            public string type { get; set; }
            public long timestamp { get; set; }
        }

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

        void Awake()
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

        void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

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

                // Keep only the most recent logs
                while (logQueue.Count > MAX_LOGS)
                {
                    logQueue.Dequeue();
                }
            }
        }

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

        public void ClearLogs()
        {
            lock (logQueue)
            {
                logQueue.Clear();
            }
        }
    }
}
