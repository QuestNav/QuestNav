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
using QuestNav.Core;
using SQLite;
using UnityEngine.Scripting;

namespace QuestNav.Config
{
    public class Config
    {
        public class Network
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// The team number used for connecting to NetworkTables
            /// Cannot be combined with <see cref="DebugIpOverride"/> at the same time.
            /// </summary>
            public int TeamNumber { get; set; } = QuestNavConstants.Network.DEFAULT_TEAM_NUMBER;

            /// <summary>
            /// An optional value that allows NetworkTables to bypass FIRST's
            /// <see href="https://docs.wpilib.org/en/stable/docs/networking/networking-introduction/ip-configurations.html">IP configuration</see>
            /// and manually specify the IP of a NetworkTables server. This is intended to only be used for debugging.
            /// Cannot be combined with <see cref="TeamNumber"/> at the same time.
            /// </summary>
            public string DebugIpOverride { get; set; } = "";
        }

        public class System
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether the headset should automatically start the QuestNav application when it turns on
            /// </summary>
            public bool EnableAutoStartOnBoot { get; set; } = true;
        }

        public class Camera
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether the passthrough camera should be streamed over NT and WebUI
            /// </summary>
            public bool EnablePassthroughStream { get; set; } = false;
        }

        public class Logging
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether debug logging for NetworkTables should be logged to the Unity and WebUI consoles.
            /// </summary>
            public bool EnableDebugLogging { get; set; } = false;
        }
    }
}
