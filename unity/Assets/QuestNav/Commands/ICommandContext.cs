namespace QuestNav.Commands
{
    /// <summary>
    /// Provides context for command execution, enabling response communication
    /// back to the command initiator (NetworkTables, Web Interface, etc.)
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        /// Sends a success response to the command initiator
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that succeeded (uint32 from protobuf)</param>
        void SendSuccessResponse(uint commandId);

        /// <summary>
        /// Sends an error response to the command initiator
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that failed (uint32 from protobuf)</param>
        /// <param name="errorMessage">Description of the error that occurred</param>
        void SendErrorResponse(uint commandId, string errorMessage);
    }
}
