namespace QuestNav.Commands
{
    /// <summary>
    /// Command context for locally-initiated commands (e.g., from Web Interface).
    /// Does not send NetworkTables responses since the command was not initiated by the robot.
    /// </summary>
    public class LocalCommandContext : ICommandContext
    {
        /// <summary>
        /// No-op success response for local commands.
        /// Local commands don't need to send NetworkTables responses.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that succeeded (uint32 from protobuf)</param>
        public void SendSuccessResponse(uint commandId)
        {
            // No NetworkTables response needed for locally-initiated commands
        }

        /// <summary>
        /// No-op error response for local commands.
        /// Local commands don't need to send NetworkTables responses.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that failed (uint32 from protobuf)</param>
        /// <param name="errorMessage">Description of the error that occurred</param>
        public void SendErrorResponse(uint commandId, string errorMessage)
        {
            // No NetworkTables response needed for locally-initiated commands
        }
    }
}
