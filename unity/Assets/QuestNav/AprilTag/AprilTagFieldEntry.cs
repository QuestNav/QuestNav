using Newtonsoft.Json;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagFieldEntry
    {
        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("pose")]
        public Pose3d Pose { get; set; }
    }
}
