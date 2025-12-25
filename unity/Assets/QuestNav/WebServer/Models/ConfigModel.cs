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
using System;
using System.Collections.Generic;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Current configuration values response
    /// </summary>
    [Serializable]
    public class ConfigResponse
    {
        public bool success;
        public int teamNumber;
        public string debugIpOverride;
        public bool enableAutoStartOnBoot;
        public bool enablePassthroughStream;
        public bool enableDebugLogging;
        public long timestamp;
    }

    /// <summary>
    /// Request to update configuration
    /// </summary>
    [Serializable]
    public class ConfigUpdateRequest
    {
        public int? TeamNumber;
        public string debugIpOverride;
        public bool? EnableAutoStartOnBoot;
        public bool? EnablePassthroughStream;
        public bool? EnableDebugLogging;
    }

    /// <summary>
    /// Simple success/failure response
    /// </summary>
    [Serializable]
    public class SimpleResponse
    {
        public bool success;
        public string message;
    }

    /// <summary>
    /// Response for log retrieval
    /// </summary>
    [Serializable]
    public class LogsResponse
    {
        public bool success;
        public List<LogCollector.LogEntry> logs;
    }

    /// <summary>
    /// Response for system information
    /// </summary>
    [Serializable]
    public class SystemInfoResponse
    {
        public string appName;
        public string version;
        public string unityVersion;
        public string buildDate;
        public string platform;
        public string deviceModel;
        public string operatingSystem;
        public int connectedClients;
        public int serverPort;
        public long timestamp;
    }
}
