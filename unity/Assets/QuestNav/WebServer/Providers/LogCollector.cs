// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
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
    /// Plain C# class designed for dependency injection with explicit lifecycle management.
    /// Implements IDisposable for proper cleanup.
    /// </summary>
    public class LogCollector : System.IDisposable
    {
        #region Constants
        /// <summary>
        /// Maximum number of logs to keep in memory
        /// </summary>
        private const int MAX_LOGS = 500;
        #endregion

        #region Fields
        /// <summary>
        /// Circular buffer queue of log entries
        /// </summary>
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();

        /// <summary>
        /// Flag indicating if disposal has been performed
        /// </summary>
        private bool disposed = false;
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
            public string Message { get; set; }

            /// <summary>
            /// Stack trace (for errors and exceptions)
            /// </summary>
            public string StackTrace { get; set; }

            /// <summary>
            /// Log type (Log, Warning, Error, Assert, Exception)
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Unix timestamp in milliseconds when log was created
            /// </summary>
            public long Timestamp { get; set; }
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Initializes the log collector and subscribes to Unity log events.
        /// Must be called on Unity main thread.
        /// </summary>
        public void Initialize()
        {
            // Subscribe to Unity log callbacks
            Application.logMessageReceived += HandleLog;
        }

        /// <summary>
        /// Disposes of the log collector and unsubscribes from Unity log events.
        /// Implements IDisposable pattern for proper resource cleanup.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                Application.logMessageReceived -= HandleLog;
                disposed = true;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles log messages from Unity's logging system.
        /// Adds new entries to circular buffer and maintains size limit.
        /// Thread-safe implementation using lock for queue access.
        /// Strips full file paths from both messages and stack traces for cleaner display.
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="stackTrace">Stack trace (for errors)</param>
        /// <param name="type">Type of log message</param>
        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                Message = CleanFilePaths(message),
                StackTrace = CleanStackTrace(stackTrace),
                Type = type.ToString(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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

        /// <summary>
        /// Cleans file paths in messages by replacing full paths with just filenames.
        /// Example: [C:\Users\...\NetworkTableConnection.cs] -> [NetworkTableConnection.cs]
        /// </summary>
        /// <param name="message">Original message with potential full paths</param>
        /// <returns>Cleaned message with only filenames</returns>
        private string CleanFilePaths(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Replace full paths in square brackets [C:\path\to\File.cs] -> [File.cs]
            // This handles paths from QueuedLogger which adds [filepath] prefix
            return System.Text.RegularExpressions.Regex.Replace(
                message,
                @"\[([A-Za-z]:[/\\](?:[^/\\:\]]+[/\\])*)?([^/\\:\]]+\.cs)\]",
                "[$2]"
            );
        }

        /// <summary>
        /// Cleans stack trace by replacing full file paths with just filenames.
        /// Example: C:\Users\...\NetworkTableConnection.cs -> NetworkTableConnection.cs
        /// </summary>
        /// <param name="stackTrace">Original stack trace with full paths</param>
        /// <returns>Cleaned stack trace with only filenames</returns>
        private string CleanStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return stackTrace;

            // Replace full paths with just filenames using regex
            // Matches patterns like: C:\path\to\File.cs:123 or /path/to/File.cs:123
            return System.Text.RegularExpressions.Regex.Replace(
                stackTrace,
                @"[A-Za-z]:[/\\](?:[^/\\:]+[/\\])*([^/\\:]+\.cs)",
                "$1"
            );
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the most recent log entries.
        /// Returns logs in chronological order (oldest first).
        /// Thread-safe for access from background thread (ConfigServer).
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
        /// Thread-safe for access from background thread (ConfigServer).
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
