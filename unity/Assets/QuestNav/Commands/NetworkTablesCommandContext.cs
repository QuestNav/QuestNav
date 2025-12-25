// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
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
