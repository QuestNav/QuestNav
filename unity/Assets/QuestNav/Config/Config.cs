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
            public int id { get; set; }

            public int teamNumber { get; set; } = QuestNavConstants.Network.DEFAULT_TEAM_NUMBER;
            
            public string debugIpOverride { get; set; } = "";
        }

        public class System
        {
            [PrimaryKey] 
            public int id { get; set; }

            public bool enableAutoStartOnBoot { get; set; } = true;
        }

        public class Logging
        {
            [PrimaryKey] 
            public int id { get; set; }
            
            public bool enableDebugLogging { get; set; } = false;
        }
    }
}
