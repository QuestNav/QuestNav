namespace QuestNav.Commands
{
    /// <summary>
    /// State of a command in its lifecycle
    /// </summary>
    public enum CommandState
    {
        /// <summary>Command is ready to execute but hasn't started yet</summary>
        Ready,
        
        /// <summary>Command is currently executing</summary>
        InProgress,
        
        /// <summary>Command has successfully completed execution</summary>
        Completed,
        
        /// <summary>Command execution failed</summary>
        Failed
    }
    
    /// <summary>
    /// Interface for individual command implementations
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the current state of the command
        /// </summary>
        CommandState State { get; }
        
        /// <summary>
        /// Executes this command
        /// </summary>
        /// <returns>True if command execution is complete, false if still in progress</returns>
        bool Execute();
        
        /// <summary>
        /// Resets the command to its initial state
        /// </summary>
        void Reset();
    }
}