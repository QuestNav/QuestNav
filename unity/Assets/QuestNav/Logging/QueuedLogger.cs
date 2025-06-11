using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QuestNav.Utils
{
    public static class QueuedLogger
    {
        // Define supported log levels.
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
        }

        // Queue to hold log entries.
        private static readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();

        // Cache the last entry to support deduplication.
        private static LogEntry lastEntry = null;

        // Internal class to represent a log entry.
        private class LogEntry
        {
            public string Message { get; private set; }
            public string CallingFileName { get; private set; }
            public int Count { get; set; }
            public LogLevel Level { get; private set; }
            public System.Exception Exception { get; private set; }

            public LogEntry(string message, LogLevel level, string callingFileName, System.Exception exception = null)
            {
                Message = message;
                Level = level;
                CallingFileName = callingFileName;
                Exception = exception;
                Count = 1;
            }

            public override string ToString()
            {
                string prefix = string.IsNullOrEmpty(CallingFileName) ? "" : $"[{CallingFileName}] ";
                string messageWithPrefix = $"{prefix}{Message}";
                return Count > 1 ? $"{messageWithPrefix} (repeated {Count} times)" : messageWithPrefix;
            }
        }

        /// <summary>
        /// Queues a message with the given log level.
        /// If the message is identical (and has no associated exception) to the previous entry, its count is increased.
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, [CallerFilePath] string callerFilePath = "")
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
        public static void LogWarning(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.Warning, callerFilePath);
        }

        /// <summary>
        /// Queues an error message.
        /// </summary>
        public static void LogError(string message, [CallerFilePath] string callerFilePath = "")
        {
            Log(message, LogLevel.Error, callerFilePath);
        }

        /// <summary>
        /// Queues an exception log entry using the exception's message.
        /// </summary>
        public static void LogException(System.Exception exception, [CallerFilePath] string callerFilePath = "")
        {
            LogException(exception.Message, exception, callerFilePath);
        }

        /// <summary>
        /// Queues an exception log entry with a custom message and exception details.
        /// </summary>
        public static void LogException(string message, System.Exception exception, [CallerFilePath] string callerFilePath = "")
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
                            Debug.LogException(entry.Exception);
                            // If multiple identical exceptions were queued, log an additional error message.
                            if (entry.Count > 1)
                            {
                                string prefix = string.IsNullOrEmpty(entry.CallingFileName) ? "" : $"[{entry.CallingFileName}] ";
                                Debug.LogError($"{prefix}{entry.Message} (repeated {entry.Count} times)");
                            }
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
        /// Extracts the filename from a full file path.
        /// </summary>
        private static string GetFileNameFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";
            
            return Path.GetFileName(filePath);
        }
    }
}