using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using QuestNav.Core;
using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// A thread-safe logging system that queues log messages and supports deduplication of identical messages.
    /// Messages are batched and flushed to Unity's Debug system periodically.
    /// </summary>
    public static class QueuedLogger
    {
        #region Fields
        /// <summary>
        /// Thread context to always log in main thread
        /// </summary>
        private static SynchronizationContext mainThreadContext;

        /// <summary>
        /// Lock object for thread-safe access to logQueue and lastEntry
        /// </summary>
        private static readonly object logLock = new object();

        /// <summary>
        /// Queue to hold log entries before they are flushed
        /// </summary>
        private static readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();

        /// <summary>
        /// Cache the last entry to support deduplication of identical messages.
        /// Access must be synchronized with logLock.
        /// </summary>
        private static LogEntry lastEntry = null;
        #endregion

        #region Enums
        /// <summary>
        /// Defines the supported log levels for the queued logger
        /// </summary>
        public enum LogLevel
        {
            /// <summary>Informational messages</summary>
            INFO,

            /// <summary>Warning messages</summary>
            WARNING,

            /// <summary>Error messages</summary>
            ERROR,
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// Internal class to represent a single log entry with metadata
        /// </summary>
        public class LogEntry
        {
            /// <summary>The log message content</summary>
            public string Message { get; private set; }

            /// <summary>
            /// Unix timestamp in milliseconds when log was created
            /// </summary>
            public long Timestamp { get; set; }

            /// <summary>
            /// The filename where the log was called from
            /// </summary>
            public string CallingFileName { get; private set; }

            /// <summary>
            /// Number of times this identical message was logged
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// The log level of this entry
            /// </summary>
            public LogLevel Level { get; private set; }

            /// <summary>
            /// Associated exception if this is an exception log
            /// </summary>
            public System.Exception Exception { get; private set; }

            /// <summary>
            /// Creates a new log entry
            /// </summary>
            /// <param name="message">The log message</param>
            /// <param name="level">The log level</param>
            /// <param name="callingFileName">The filename where the log was called from</param>
            /// <param name="exception">Optional exception associated with this log entry</param>
            public LogEntry(
                string message,
                LogLevel level,
                [CallerFilePath] string callingFileName = "",
                System.Exception exception = null
            )
            {
                Message = message;
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Level = level;
                CallingFileName = callingFileName;
                Exception = exception;
                Count = 1;
            }

            public override string ToString()
            {
                string prefix = string.IsNullOrEmpty(CallingFileName)
                    ? ""
                    : $"[{CallingFileName}] ";
                string messageWithPrefix = $"{prefix}{Message}";
                return Count > 1
                    ? $"{messageWithPrefix} (repeated {Count} times)"
                    : messageWithPrefix;
            }
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Initializes the main thread context. Must be called from the main thread (e.g., from a MonoBehaviour's Awake).
        /// </summary>
        public static void Initialize()
        {
            mainThreadContext = SynchronizationContext.Current;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Queues a message with the given log level.
        /// If the message is identical (and has no associated exception) to the previous entry, its count is increased.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level (defaults to Info)</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void Log(
            string message,
            LogLevel level = LogLevel.INFO,
            [CallerFilePath] string callerFilePath = ""
        )
        {
            string callingFileName = GetFileNameFromPath(callerFilePath);

            lock (logLock)
            {
                if (
                    lastEntry != null
                    && lastEntry.Message == message
                    && lastEntry.Level == level
                    && lastEntry.CallingFileName == callingFileName
                    && lastEntry.Exception == null
                )
                {
                    lastEntry.Count++;
                }
                else
                {
                    lastEntry = new LogEntry(message, level, callingFileName);
                    logQueue.Enqueue(lastEntry);

                    // Maintain circular buffer - keep only the most recent logs
                    while (logQueue.Count > QuestNavConstants.Logging.MAX_LOGS)
                    {
                        logQueue.Dequeue();
                    }
                }
            }
        }

        /// <summary>
        /// Queues a warning message.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogWarning(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.WARNING, callerFilePath);
        }

        /// <summary>
        /// Queues an error message.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogError(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.ERROR, callerFilePath);
        }

        /// <summary>
        /// Queues an exception log entry using the exception's message.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogException(
            System.Exception exception,
            [CallerFilePath] string callerFilePath = ""
        )
        {
            LogException(exception.Message, exception, callerFilePath);
        }

        /// <summary>
        /// Queues an exception log entry with a custom message and exception details.
        /// </summary>
        /// <param name="message">Custom message to accompany the exception</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogException(
            string message,
            System.Exception exception,
            [CallerFilePath] string callerFilePath = ""
        )
        {
            string callingFileName = GetFileNameFromPath(callerFilePath);

            lock (logLock)
            {
                if (
                    lastEntry != null
                    && lastEntry.Level == LogLevel.ERROR
                    && lastEntry.Exception != null
                    && lastEntry.Message == message
                    && lastEntry.CallingFileName == callingFileName
                )
                {
                    lastEntry.Count++;
                }
                else
                {
                    lastEntry = new LogEntry(message, LogLevel.ERROR, callingFileName, exception);
                    logQueue.Enqueue(lastEntry);

                    // Maintain circular buffer - keep only the most recent logs
                    while (logQueue.Count > QuestNavConstants.Logging.MAX_LOGS)
                    {
                        logQueue.Dequeue();
                    }
                }
            }
        }

        /// <summary>
        /// Flushes all queued messages in order using the appropriate Debug method,
        /// and then clears the queue.
        /// </summary>
        public static void Flush()
        {
            // Copy entries under lock, then log outside lock to avoid holding lock during Debug calls
            List<LogEntry> entriesToFlush;
            lock (logLock)
            {
                entriesToFlush = new List<LogEntry>(logQueue);
                logQueue.Clear();
                lastEntry = null;
            }

            // Only flush on main thread
            invokeOnMainThread(() =>
            {
                foreach (LogEntry entry in entriesToFlush)
                {
                    switch (entry.Level)
                    {
                        case LogLevel.WARNING:
                            Debug.LogWarning(entry.ToString());
                            break;
                        case LogLevel.ERROR:
                            if (entry.Exception != null)
                            {
                                // Log the custom message with prefix and count
                                Debug.LogError(entry.ToString());
                                Debug.LogException(entry.Exception);
                            }
                            else
                            {
                                Debug.LogError(entry.ToString());
                            }
                            break;
                        default:
                            Debug.Log(entry.ToString());
                            break;
                    }
                }
            });
        }

        /// <summary>
        /// Gets the most recent log entries.
        /// Returns logs in chronological order (oldest first).
        /// Thread-safe for access from background thread (ConfigServer).
        /// </summary>
        /// <param name="count">Maximum number of logs to return (default: 100)</param>
        /// <returns>List of recent log entries</returns>
        public static List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (logLock)
            {
                var logs = new List<LogEntry>(logQueue);

                // Return most recent logs (up to count)
                int startIndex = Math.Max(0, logs.Count - count);
                return logs.GetRange(startIndex, logs.Count - startIndex);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Extracts the filename from a full file path for cleaner log output.
        /// </summary>
        /// <param name="filePath">The full file path</param>
        /// <returns>Just the filename portion of the path</returns>
        private static string GetFileNameFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            // Windows paths compiled with [CallerFilePath] use backslashes,
            // but Path.GetFileName on Unix doesn't recognize \ as a separator
            string normalizedPath = filePath.Replace('\\', '/');
            return Path.GetFileName(normalizedPath);
        }

        /// <summary>
        /// Invokes an action on the main thread using the captured SynchronizationContext.
        /// Falls back to direct invocation if no context was captured.
        /// </summary>
        private static void invokeOnMainThread(Action action)
        {
            if (mainThreadContext != null)
            {
                mainThreadContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
        #endregion
    }
}
