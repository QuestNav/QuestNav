using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a rotation in FRC 3d space
    /// </summary>
    public class Quaternion
    {
        [JsonProperty("X")]
        public double X { get; set; }
        [JsonProperty("Y")]
        public double Y { get; set; }
        [JsonProperty("Z")]
        public double Z { get; set; }
        [JsonProperty("W")]
        public double W { get; set; }
        
        /// <summary>
        /// Builds a new Quaternion
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        [JsonConstructor]
        public Quaternion(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Builds a new Quaternion from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf quaternion to copy</param>
        public Quaternion(ProtobufQuaternion protobuf)
        {
            X = protobuf.X;
            Y = protobuf.Y;
            Z = protobuf.Z;
            W = protobuf.W;
        }

        /// <summary>
        /// Converts the Quaternion to a ProtobufQuaternion
        /// </summary>
        /// <returns>A protobuf holding the quaternion</returns>
        public ProtobufQuaternion ToProtobuf()
        {
            var proto = new ProtobufQuaternion
            {
                X = X,
                Y = Y,
                Z = Z,
                W = W,
            };
            return proto;
        }
    }
}
