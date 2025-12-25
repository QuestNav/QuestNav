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
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Unity implementation of ILogger that forwards log messages to Unity's Debug system.
    /// Safe to use from ConfigBootstrap (MonoBehaviour) on the main thread.
    /// This adapter allows ConfigServer (running on background thread) to safely log
    /// messages to Unity's Debug console via the ILogger interface.
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        /// Logs an informational message to Unity console
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Logs a warning message to Unity console
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message to Unity console
        /// </summary>
        /// <param name="message">The error message to log</param>
        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
