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
