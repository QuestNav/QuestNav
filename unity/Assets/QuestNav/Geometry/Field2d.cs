using Newtonsoft.Json;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Field dimensions from the "field" object of a WPILib AprilTag layout JSON.
    /// </summary>
    public class Field2d
    {
        /// <summary>
        /// Field length along the FRC X axis, in meters
        /// </summary>
        [JsonProperty("length")]
        public double Length { get; set; }

        /// <summary>
        /// Field width along the FRC Y axis, in meters
        /// </summary>
        [JsonProperty("width")]
        public double Width { get; set; }

        // Newtonsoft binds constructor parameters by name, so these must match the JSON keys
        [JsonConstructor]
        public Field2d(double length, double width)
        {
            Length = length;
            Width = width;
        }
    }
}
