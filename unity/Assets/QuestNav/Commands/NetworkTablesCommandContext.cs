using QuestNav.Network;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command context for NetworkTables-initiated commands.
    /// Sends success/error responses back to the robot via NetworkTables.
    /// </summary>
    public class NetworkTablesCommandContext : ICommandContext
    {
        private readonly INetworkTableConnection networkTableConnection;

        /// <summary>
        /// Initializes a new instance of NetworkTablesCommandContext
        /// </summary>
        /// <param name="networkTableConnection">The NetworkTables connection to use for responses</param>
        public NetworkTablesCommandContext(INetworkTableConnection networkTableConnection)
        {
            this.networkTableConnection = networkTableConnection;
        }

        /// <summary>
        /// Sends a success response via NetworkTables
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that succeeded (uint32 from protobuf)</param>
        public void SendSuccessResponse(uint commandId)
        {
            networkTableConnection.SendCommandSuccessResponse(commandId);
        }

        /// <summary>
        /// Sends an error response via NetworkTables
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that failed (uint32 from protobuf)</param>
        /// <param name="errorMessage">Description of the error that occurred</param>
        public void SendErrorResponse(uint commandId, string errorMessage)
        {
            networkTableConnection.SendCommandErrorResponse(commandId, errorMessage);
        }
    }
}
