using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// A thread-safe logging system that queues log messages and supports deduplication of identical messages.
    /// Messages are batched and flushed to Unity's Debug system periodically.
    /// </summary>
    public static class QueuedLogger
    {
        /// <summary>
        /// Defines the supported log levels for the queued logger
        /// </summary>
        public enum LogLevel
        {
            /// <summary>Informational messages</summary>
            Info,

            /// <summary>Warning messages</summary>
            Warning,

            /// <summary>Error messages</summary>
            Error,
        }

        /// <summary>
        /// Queue to hold log entries before they are flushed
        /// </summary>
        private static readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();

        /// <summary>
        /// Cache the last entry to support deduplication of identical messages
        /// </summary>
        private static LogEntry lastEntry = null;

        /// <summary>
        /// Internal class to represent a single log entry with metadata
        /// </summary>
        private class LogEntry
        {
            /// <summary>The log message content</summary>
            public string Message { get; private set; }

            /// <summary>The filename where the log was called from</summary>
            public string CallingFileName { get; private set; }

            /// <summary>Number of times this identical message was logged</summary>
            public int Count { get; set; }

            /// <summary>The log level of this entry</summary>
            public LogLevel Level { get; private set; }

            /// <summary>Associated exception if this is an exception log</summary>
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
                string callingFileName,
                System.Exception exception = null
            )
            {
                Message = message;
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

        /// <summary>
        /// Queues a message with the given log level.
        /// If the message is identical (and has no associated exception) to the previous entry, its count is increased.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level (defaults to Info)</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void Log(
            string message,
            LogLevel level = LogLevel.Info,
            [CallerFilePath] string callerFilePath = ""
        )
        {
            string callingFileName = GetFileNameFromPath(callerFilePath);

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
            }
        }

        /// <summary>
        /// Queues a warning message.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogWarning(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.Warning, callerFilePath);
        }

        /// <summary>
        /// Queues an error message.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="callerFilePath">Automatically populated with the calling file path</param>
        public static void LogError(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.Error, callerFilePath);
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

            if (
                lastEntry != null
                && lastEntry.Level == LogLevel.Error
                && lastEntry.Exception != null
                && lastEntry.Message == message
                && lastEntry.CallingFileName == callingFileName
            )
            {
                lastEntry.Count++;
            }
            else
            {
                lastEntry = new LogEntry(message, LogLevel.Error, callingFileName, exception);
                logQueue.Enqueue(lastEntry);
            }
        }

        /// <summary>
        /// Flushes all queued messages in order using the appropriate Debug method,
        /// and then clears the queue.
        /// </summary>
        public static void Flush()
        {
            while (logQueue.Count > 0)
            {
                LogEntry entry = logQueue.Dequeue();
                switch (entry.Level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(entry.ToString());
                        break;
                    case LogLevel.Error:
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
            lastEntry = null;
        }

        /// <summary>
        /// Extracts the filename from a full file path for cleaner log output.
        /// </summary>
        /// <param name="filePath">The full file path</param>
        /// <returns>Just the filename portion of the path</returns>
        private static string GetFileNameFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            return Path.GetFileName(filePath);
        }
    }
}
