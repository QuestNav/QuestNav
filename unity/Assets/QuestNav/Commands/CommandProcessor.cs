using System.Collections.Generic;
using QuestNav.Commands.Commands;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for command processing.
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Gets or sets a value indicating whether a reset operation is in progress.
        /// </summary>
        bool ResetInProgress { get; set; }
        
        /// <summary>
        /// Processes commands received from the robot.
        /// </summary>
        void ProcessCommands();
    }
    
    /// <summary>
    /// Processes commands from the network connection
    /// </summary>
    public class CommandProcessor : ICommandProcessor
    {
        private readonly INetworkTableConnection networkConnection;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;
        
        private readonly Dictionary<long, ICommand> commands = new Dictionary<long, ICommand>();
        private ICommand currentCommand = null;
        
        /// <summary>
        /// Gets or sets a value indicating whether a reset operation is in progress.
        /// Maintained for interface compatibility - internal handling uses command state.
        /// </summary>
        public bool ResetInProgress
        {
            get => currentCommand != null && currentCommand.State == CommandState.InProgress;
            set { /* Property provided for interface compatibility only */ }
        }
        
        /// <summary>
        /// Initializes the command processor with required dependencies.
        /// </summary>
        /// <param name="networkConnection">The network connection to use for command communication</param>
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public CommandProcessor(INetworkTableConnection networkConnection, Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            this.networkConnection = networkConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            
            // Register all commands
            RegisterCommands();
        }
        
        /// <summary>
        /// Registers all available commands with their command IDs
        /// </summary>
        private void RegisterCommands()
        {
            // Register each command with its corresponding command ID
            commands[QuestNavConstants.Commands.HEADING_RESET] = new HeadingResetCommand(
                networkConnection, vrCamera, vrCameraRoot, resetTransform);
                
            commands[QuestNavConstants.Commands.POSE_RESET] = new PoseResetCommand(
                networkConnection, vrCamera, vrCameraRoot, resetTransform);
                
            commands[QuestNavConstants.Commands.PING] = new PingCommand(
                networkConnection);
                
            // Add more commands as needed
        }
        
        /// <summary>
        /// Process commands from the network connection
        /// </summary>
        public void ProcessCommands()
        {
            // If we have an active command, continue executing it
            if (currentCommand != null)
            {
                bool finished = currentCommand.Execute();
                
                // If command is done, clear it
                if (finished)
                {
                    QueuedLogger.Log($"Command {currentCommand.GetType().Name} completed with state: {currentCommand.State}");
                    currentCommand = null;
                }
                
                return; // Skip checking for new commands while one is running
            }
            
            // Only check for new commands if we don't have one running
            long commandId = networkConnection.GetCommandRequest();
            
            if (commandId != 0)
            {
                if (commands.TryGetValue(commandId, out ICommand commandHandler))
                {
                    commandHandler.Reset(); // Ensure command is in initial state
                    currentCommand = commandHandler;
                    
                    // Execute the command
                    bool finished = currentCommand.Execute();
                    
                    // If command completed immediately, clear it
                    if (finished)
                    {
                        QueuedLogger.Log($"Command {currentCommand.GetType().Name} completed immediately with state: {currentCommand.State}");
                        currentCommand = null;
                    }
                }
                else
                {
                    // Unknown command
                    QueuedLogger.LogWarning($"Received unknown command ID: {commandId}");
                    networkConnection.SetCommandResponse(QuestNavConstants.Commands.IDLE);
                }
            }
        }
    }
}