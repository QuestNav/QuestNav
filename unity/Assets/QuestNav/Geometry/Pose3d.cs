using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    public class Pose3d
    {
        [JsonProperty("translation")]
        public Translation3d Translation { get; set; }
        [JsonProperty("rotation")]
        public Rotation3d Rotation { get; set; }
        
        [JsonConstructor]
        public Pose3d(Translation3d translation, Rotation3d rotation)
        {
            Translation = translation;
            Rotation = rotation;
        }

        public Pose3d(ProtobufPose3d protobuf)
        {
            Translation = new Translation3d(protobuf.Translation);
            Rotation = new Rotation3d(protobuf.Rotation);
        }

        public ProtobufPose3d ToProtobuf()
        {
            var proto = new ProtobufPose3d
            {
                Translation = Translation.ToProtobuf(),
                Rotation = Rotation.ToProtobuf(),
            };
            return proto;
        }
    }
}
 