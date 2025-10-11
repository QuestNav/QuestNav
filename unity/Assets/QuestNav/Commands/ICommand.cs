using QuestNav.Protos.Generated;

namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for individual command implementations
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the nice name of the command
        /// </summary>
        string commandNiceName { get; }

        /// <summary>
        /// Executes this command with the provided command data
        /// </summary>
        /// <param name="receivedCommand">The command data received from the robot</param>
        void Execute(ProtobufQuestNavCommand receivedCommand);
    }
}
