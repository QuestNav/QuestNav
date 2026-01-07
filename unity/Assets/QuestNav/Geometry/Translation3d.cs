using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    public class Translation3d
    {
        [JsonProperty("x")]
        public double X { get; set; }
        [JsonProperty("y")]
        public double Y { get; set; }
        [JsonProperty("z")]
        public double Z { get; set; }
        
        [JsonConstructor]
        public Translation3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Translation3d(ProtobufTranslation3d protobuf)
        {
            X = protobuf.X;
            Y = protobuf.Y;
            Z = protobuf.Z;
        }

        public ProtobufTranslation3d ToProtobuf()
        {
            var proto = new ProtobufTranslation3d
            {
                X = X,
                Y = Y,
                Z = Z,
            };
            return proto;
        }
    }
}
