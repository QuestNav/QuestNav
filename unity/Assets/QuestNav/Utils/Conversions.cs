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
        ///
        /// COORDINATE SYSTEM DIFFERENCES:
        /// FRC Field Coordinates:          Unity World Coordinates:
        /// - Origin: Blue alliance wall    - Origin: Arbitrary (set by VR tracking)
        /// - X-axis: Points toward red     - X-axis: Points right
        /// - Y-axis: Points left           - Y-axis: Points up
        /// - Z-axis: Points up             - Z-axis: Points forward
        /// - Rotation: Counter-clockwise   - Rotation: Clockwise (left-handed)
        ///
        /// CONVERSION MAPPING:
        /// FRC X (forward) → Unity Z (forward)
        /// FRC Y (left) → Unity -X (right becomes left)
        /// FRC Z (up) → Unity Y (up)
        /// FRC θ (CCW) → Unity -Y rotation (CW)
        ///
        /// HEIGHT PRESERVATION:
        /// The Y coordinate (height) is preserved from the current VR position since
        /// FRC operates in 2D while VR tracking provides 3D data. This allows users
        /// to crouch, stand, etc. without affecting the robot's 2D field position.
        /// </summary>
        /// <param name="targetPose2d">Target position in FRC coordinates (meters, radians)</param>
        /// <param name="currentPose">Current VR headset position (for preserving Y height)</param>
        /// <param name="currentQuaternion">Current VR headset rotation (for preserving pitch/roll)</param>
        /// <returns>Position and rotation in Unity coordinate system</returns>
        public static (Vector3 position, Quaternion rotation) FrcToUnity(
            ProtobufPose2d targetPose2d,
            Vector3 currentPose,
            Quaternion currentQuaternion
        )
        {
            // Convert 2D field position to 3D Unity position
            // FRC field is measured in meters with origin at blue alliance wall
            Vector3 unityPosition = new Vector3(
                (float)-targetPose2d.Translation.Y, // FRC Y (left) → Unity -X (convert left to right-handed)
                currentPose.y, // Preserve current VR height (allows crouching/standing)
                (float)targetPose2d.Translation.X // FRC X (forward) → Unity Z (forward)
            );

            // Convert 2D field rotation to 3D Unity rotation, preserving VR pitch/roll
            // FRC uses counter-clockwise radians, Unity uses clockwise degrees
            Vector3 currentEuler = currentQuaternion.eulerAngles;
            Vector3 newEuler = new Vector3(
                currentEuler.x, // Preserve pitch (looking up/down)
                (float)(-targetPose2d.Rotation.Value * Mathf.Rad2Deg), // Convert FRC yaw: CCW radians → CW degrees
                currentEuler.z // Preserve roll (head tilt)
            );
            Quaternion unityRotation = Quaternion.Euler(newEuler);

            return (unityPosition, unityRotation);
        }

        /// <summary>
        /// Converts from Unity coordinate system to FRC coordinate system.
        ///
        /// This is the inverse of FrcToUnity(), used to send VR headset position
        /// to the robot in the coordinate system it expects.
        ///
        /// CONVERSION MAPPING (REVERSE):
        /// Unity X (right) → FRC -Y (left, accounting for handedness)
        /// Unity Y (up) → Ignored (FRC is 2D, robot doesn't need height)
        /// Unity Z (forward) → FRC X (forward)
        /// Unity Y rotation (CW degrees) → FRC θ (CCW radians)
        ///
        /// USAGE:
        /// Called 100 times per second to stream headset position to robot.
        /// The robot uses this data for autonomous navigation, driver assistance,
        /// or any other functionality that needs to know where the driver is looking.
        /// </summary>
        /// <param name="unityPosition">VR headset position in Unity world coordinates</param>
        /// <param name="unityRotation">VR headset orientation in Unity world coordinates</param>
        /// <returns>2D pose in FRC field coordinates (meters, radians)</returns>
        public static ProtobufPose2d UnityToFrc(Vector3 unityPosition, Quaternion unityRotation)
        {
            return new ProtobufPose2d
            {
                Translation = new ProtobufTranslation2d
                {
                    // Convert 3D Unity position to 2D FRC field position
                    X = unityPosition.z, // Unity Z (forward) → FRC X (forward toward red alliance)
                    Y = -unityPosition.x, // Unity X (right) → FRC -Y (convert to left-positive system)
                },
                Rotation = new ProtobufRotation2d
                {
                    // Convert Unity Y rotation (yaw) from clockwise degrees to counter-clockwise radians
                    // Only yaw matters for 2D field positioning; pitch/roll are VR-specific
                    Value = -unityRotation.eulerAngles.y * Mathf.Deg2Rad,
                },
            };
        }
    }
}
