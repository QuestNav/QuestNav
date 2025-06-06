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
        /// Executes this command
        /// </summary>
        void Execute(ProtobufQuestNavCommand receivedCommand);
    }
}
