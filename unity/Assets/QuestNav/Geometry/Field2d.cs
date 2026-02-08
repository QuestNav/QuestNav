using Newtonsoft.Json;

namespace QuestNav.QuestNav.Geometry
{
    public class Field2d
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonConstructor]
        public Field2d(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
