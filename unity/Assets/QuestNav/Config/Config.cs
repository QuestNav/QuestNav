using QuestNav.Core;
using SQLite;
using UnityEngine.Scripting;

namespace QuestNav.Config
{
    public class Config
    {
        public class Network
        {
            [PrimaryKey]
            public int ID { get; set; }

            public int TeamNumber { get; set; } = QuestNavConstants.Network.DEFAULT_TEAM_NUMBER;

            public string DebugIpOverride { get; set; } = "";
        }

        public class System
        {
            [PrimaryKey]
            public int ID { get; set; }

            public bool EnableAutoStartOnBoot { get; set; } = true;
        }

        public class Logging
        {
            [PrimaryKey]
            public int ID { get; set; }

            public bool EnableDebugLogging { get; set; } = false;
        }
    }
}
