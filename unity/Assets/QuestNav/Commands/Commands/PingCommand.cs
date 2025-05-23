using System;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Simple ping command that responds immediately
    /// </summary>
    public class PingCommand : CommandBase
    {
        private readonly INetworkTableConnection networkTableConnection;
        
        /// <summary>
        /// Initializes a new instance of the PingCommand
        /// </summary>
        /// <param name="networkTableConnection">The network connection to use for command communication</param>
        public PingCommand(INetworkTableConnection networkTableConnection)
        {
            this.networkTableConnection = networkTableConnection;
        }
        
        /// <summary>
        /// Executes the ping command
        /// </summary>
        /// <returns>Always returns true as ping completes immediately</returns>
        protected override bool ExecuteCommand()
        {
            try
            {
                QueuedLogger.Log("Ping received, responding...");
                
                // Verify network connection is available
                if (networkTableConnection == null)
                {
                    throw new InvalidOperationException("Network connection is null");
                }
                
                // Set ping response
                networkTableConnection.SetCommandResponse(QuestNavConstants.Commands.PING_RESPONSE);
                
                // Log additional diagnostics
                QueuedLogger.Log("Ping response sent successfully");
                
                return true;
            }
            catch (Exception e)
            {
                QueuedLogger.LogError($"Error during ping command: {e.Message}");
                QueuedLogger.LogException(e);
                
                // Mark command as failed directly
                SetFailed();
                
                return true;
            }
        }
        
        /// <summary>
        /// Called when command execution starts
        /// </summary>
        protected override void OnStart()
        {
            QueuedLogger.Log("Starting ping command");
        }
        
        /// <summary>
        /// Called when command execution ends
        /// </summary>
        protected override void OnEnd(bool interrupted)
        {
            if (State == CommandState.Completed)
            {
                QueuedLogger.Log("Ping command completed successfully");
            }
            else if (State == CommandState.Failed)
            {
                QueuedLogger.LogError("Ping command failed");
            }
        }
    }
}