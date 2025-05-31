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
        /// <param name="targetPose2d">Target position in FRC coordinates.</param>
        /// <param name="currentPose">The current pose of the Quest in Vector3 format (for maintaining Y height).</param>
        /// <param name="currentQuaternion">The current rotation to preserve pitch and roll.</param>
        /// <returns>A tuple of Vector3 and Quaternion in Unity coordinate system.</returns>
        public static (Vector3 position, Quaternion rotation) FrcToUnity(
            Pose2d targetPose2d,
            Vector3 currentPose,
            Quaternion currentQuaternion
        )
        {
            // Convert position: FRC X→Unity Z, FRC Y→Unity -X
            Vector3 unityPosition = new Vector3(
                (float)-targetPose2d.Translation.Y, // FRC Y → Unity -X
                currentPose.y, // Maintain current height
                (float)targetPose2d.Translation.X // FRC X → Unity Z
            );

            // Convert rotation: preserve current pitch/roll, set yaw from FRC
            Vector3 currentEuler = currentQuaternion.eulerAngles;
            Vector3 newEuler = new Vector3(
                currentEuler.x, // Keep current pitch
                (float)(targetPose2d.Rotation.Radians * Mathf.Rad2Deg), // Set yaw from FRC
                currentEuler.z // Keep current roll
            );
            Quaternion unityRotation = Quaternion.Euler(newEuler);

            return (unityPosition, unityRotation);
        }

        /// <summary>
        /// Converts from Unity coordinate system to FRC coordinate system.
        /// </summary>
        /// <param name="unityPosition">The position in Unity coordinates.</param>
        /// <param name="unityRotation">The rotation in Unity coordinates.</param>
        /// <returns>A Pose2d representing position and rotation in FRC coordinates.</returns>
        public static Pose2d UnityToFrc(Vector3 unityPosition, Quaternion unityRotation)
        {
            return new Pose2d
            {
                Translation = new Translation2d
                {
                    X = unityPosition.z, // Unity Z → FRC X
                    Y = -unityPosition.x, // Unity X → FRC -Y
                },
                Rotation = new Rotation2d { Radians = unityRotation.eulerAngles.y * Mathf.Deg2Rad },
            };
        }
    }
}
