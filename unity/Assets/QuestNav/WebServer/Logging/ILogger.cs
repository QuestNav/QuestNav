namespace QuestNav.WebServer
{
    /// <summary>
    /// Logger interface for ConfigServer to avoid direct Unity API calls on background threads.
    /// Implementations bridge logging from background threads to Unity's main thread.
    /// ConfigServer runs on a background thread and cannot call Unity APIs directly,
    /// so this interface allows it to safely delegate logging to the main thread.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        void Log(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The error message to log</param>
        void LogError(string message);
    }
}
