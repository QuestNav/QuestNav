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
namespace QuestNav.Commands
{
    /// <summary>
    /// Command context for web-initiated commands (e.g., from Web Interface).
    /// Does not send NetworkTables responses since the command was not initiated by the robot.
    /// </summary>
    public class WebCommandContext : ICommandContext
    {
        /// <summary>
        /// No-op success response for web commands.
        /// Web commands don't need to send NetworkTables responses.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that succeeded (uint32 from protobuf)</param>
        public void SendSuccessResponse(uint commandId)
        {
            // No NetworkTables response needed for web-initiated commands
        }

        /// <summary>
        /// No-op error response for web commands.
        /// Web commands don't need to send NetworkTables responses.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command that failed (uint32 from protobuf)</param>
        /// <param name="errorMessage">Description of the error that occurred</param>
        public void SendErrorResponse(uint commandId, string errorMessage)
        {
            // No NetworkTables response needed for web-initiated commands
        }
    }
}
