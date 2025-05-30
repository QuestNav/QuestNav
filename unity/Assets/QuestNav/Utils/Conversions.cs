using QuestNav.Protos;
using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// Provides utility methods for converting between FRC and Unity coordinate systems.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Converts from FRC coordinate system to Unity coordinate system.
        /// </summary>
        /// <param name="frc">Array of 3 float values representing position in FRC coordinates.</param>
        /// <param name="height">The height value to use for the Unity Y-coordinate. Should be the same as what the headset auto-detects.</param>
        /// <returns>A Vector3 in Unity coordinate system, or Vector3.zero if input array is invalid.</returns>
        public static Vector3 FrcToUnity(Pose2d pose2d, float height)
        {
            
            return new Vector3((float) -pose2d.Translation.Y, height, (float) pose2d.Translation.X);
        }
        
        /// <summary>
        /// Converts from Unity coordinate system to FRC coordinate system.
        /// </summary>
        /// <param name="unity">The position in Unity coordinates.</param>
        /// <param name="rotation">The rotation value to use for the third element of the FRC array.</param>
        /// <returns>An array of 3 float values representing position in FRC coordinates.</returns>
        public static float[] UnityToFrc(Vector3 unity, float rotation)
        {
            return new float[] { unity.z, -unity.x, rotation };
        }
    }
}