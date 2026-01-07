using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    public class Rotation3d
    {
        [JsonProperty("quaternion")]
        public Quaternion Quaternion { get; set; }
        
        [JsonConstructor]
        public Rotation3d(Quaternion quaternion)
        {
            Quaternion = quaternion;
        }

        public Rotation3d(ProtobufRotation3d protobuf)
        {
            Quaternion = new Quaternion(protobuf.Q);
        }

        public ProtobufRotation3d ToProtobuf()
        {
            var proto = new ProtobufRotation3d
            {
                Q = Quaternion.ToProtobuf(),
            };
            return proto;
        }
        
    }
}
