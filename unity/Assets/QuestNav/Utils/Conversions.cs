using UnityEngine;
using Wpi.Proto;

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
        /// <returns>A tuple of Vector3 and Quaternion in Unity coordinate system.</returns>
        public static (Vector3 position, Quaternion rotation) FrcToUnity3d(
            ProtobufPose3d targetPose3d
        )
        {
            // Convert position
            Vector3 unityPosition = new Vector3(
                (float)-targetPose3d.Translation.Y, // FRC Y → Unity -X
                (float)targetPose3d.Translation.Z, // FRC Z → Unity Y
                (float)targetPose3d.Translation.X // FRC X → Unity Z
            );

            // Convert rotation
            Quaternion unityRotation = new Quaternion(
                (float)targetPose3d.Rotation.Q.Y, // FRC Y → Unity X
                (float)-targetPose3d.Rotation.Q.Z, // FRC Z → Unity -Y
                (float)-targetPose3d.Rotation.Q.X, // FRC X → Unity -Z
                (float)targetPose3d.Rotation.Q.W // FRC W → Unity W
            );

            return (unityPosition, unityRotation);
        }

        /// <summary>
        /// Converts from Unity coordinate system to FRC coordinate system.
        /// </summary>
        /// <param name="unityPosition">The position in Unity coordinates.</param>
        /// <param name="unityRotation">The rotation in Unity coordinates.</param>
        /// <returns>A Pose3d representing position and rotation in FRC coordinates.</returns>
        public static ProtobufPose3d UnityToFrc3d(Vector3 unityPosition, Quaternion unityRotation)
        {
            return new ProtobufPose3d
            {
                Translation = new ProtobufTranslation3d
                {
                    X = unityPosition.z, // Unity Z → FRC X
                    Y = -unityPosition.x, // Unity X → FRC -Y
                    Z = unityPosition.y, // Unity Y → FRC Z
                },
                Rotation = new ProtobufRotation3d
                {
                    Q = new ProtobufQuaternion
                    {
                        X = -unityRotation.z, // Unity Z → FRC -X
                        Y = unityRotation.x, // Unity X → FRC Y
                        Z = -unityRotation.y, // Unity Y → FRC -Z
                        W = unityRotation.w, // Unity W → FRC W
                    },
                },
            };
        }
    }
}
