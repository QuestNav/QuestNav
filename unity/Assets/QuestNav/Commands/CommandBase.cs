// A base class for commands that handles state management
using System;
using QuestNav.Utils;

namespace QuestNav.Commands
{
    /// <summary>
    /// Base class for all commands that handles state management
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        private CommandState state = CommandState.Ready;
        
        /// <summary>
        /// Gets the current state of the command
        /// </summary>
        public CommandState State => state;
        
        /// <summary>
        /// Template method that handles state transitions and error handling
        /// </summary>
        /// <returns>True if the command has completed or failed, false if still in progress</returns>
        public bool Execute()
        {
            try
            {
                // Only set to InProgress if it's in Ready state
                if (state == CommandState.Ready)
                {
                    state = CommandState.InProgress;
                    OnStart();
                }
                
                // Execute the command implementation if still in progress
                if (state == CommandState.InProgress)
                {
                    bool isFinished = ExecuteCommand();
                    
                    // If command reports it's finished, mark as complete
                    if (isFinished)
                    {
                        state = CommandState.Completed;
                        OnEnd(false);
                    }
                }
                
                // Return true if command is no longer in progress
                return state != CommandState.InProgress;
            }
            catch (Exception e)
            {
                QueuedLogger.LogError($"Error executing {GetType().Name}: {e.Message}");
                QueuedLogger.LogException(e);
                state = CommandState.Failed;
                OnEnd(true);
                return true;
            }
        }
        
        /// <summary>
        /// Called when the command first starts execution
        /// </summary>
        protected virtual void OnStart()
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Implement command-specific logic here
        /// </summary>
        /// <returns>True if command is finished, false if it should continue</returns>
        protected abstract bool ExecuteCommand();
        
        /// <summary>
        /// Called when the command ends, either by completion or interruption
        /// </summary>
        /// <param name="interrupted">True if the command was interrupted, false if it completed normally</param>
        protected virtual void OnEnd(bool interrupted)
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Reset command to initial state
        /// </summary>
        public virtual void Reset()
        {
            // Only call OnEnd if the command was in progress
            if (state == CommandState.InProgress)
            {
                OnEnd(true);
            }
            
            state = CommandState.Ready;
        }
        
        /// <summary>
        /// Utility method to mark command as completed
        /// </summary>
        protected void SetCompleted()
        {
            if (state == CommandState.InProgress)
            {
                state = CommandState.Completed;
                OnEnd(false);
            }
        }
        
        /// <summary>
        /// Utility method to mark command as failed
        /// </summary>
        protected void SetFailed()
        {
            if (state == CommandState.InProgress)
            {
                state = CommandState.Failed;
                OnEnd(true);
            }
        }
    }
}