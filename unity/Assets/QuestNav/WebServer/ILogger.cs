namespace QuestNav.Config
{
    /// <summary>
    /// Logger interface for ConfigServer to avoid direct Unity API calls on background threads.
    /// Implementations bridge logging from background threads to Unity's main thread.
    /// </summary>
    public interface ILogger
    {
        /// <summary>Logs an informational message.</summary>
        void Log(string message);

        /// <summary>Logs a warning message.</summary>
        void LogWarning(string message);

        /// <summary>Logs an error message.</summary>
        void LogError(string message);
    }
}
