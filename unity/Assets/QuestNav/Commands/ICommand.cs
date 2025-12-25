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
        string CommandNiceName { get; }

        /// <summary>
        /// Executes this command with the provided command data
        /// </summary>
        /// <param name="receivedCommand">The command data received from the robot</param>
        void Execute(ProtobufQuestNavCommand receivedCommand);
    }
}
