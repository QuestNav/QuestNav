using QuestNav.QuestNav.Geometry;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

namespace QuestNav.Utils
{
    /// <summary>
    /// Provides utility methods for converting between FRC and Unity coordinate systems.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Maps FRC body-frame coordinates (X-Fwd, Y-Left, Z-Up) to CV camera coordinates
        /// (X-Right, Y-Down, Z-Fwd): v_cv = R * v_body.
        /// </summary>
        /// <remarks>
        /// Corresponds to the rotation matrix:
        /// <code>
        /// [0 -1  0]
        /// [0  0 -1]
        /// [1  0  0]
        /// </code>
        /// </remarks>
        private static readonly Rotation3d BodyToCv = new Rotation3d(
            new QuestNav.Geometry.Quaternion(0.5, 0.5, -0.5, 0.5)
        );

        /// <summary>
        /// Converts from FRC coordinate system to Unity coordinate system.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///     <listheader>
        ///         <term>FRC Field Coordinates</term>
        ///         <description>Unity World Coordinates</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Origin: Blue alliance wall</term>
        ///         <description>Origin: Arbitrary (set by VR tracking)</description>
        ///     </item>
        ///     <item>
        ///         <term>X-axis: Points toward red</term>
        ///         <description>X-axis: Points right</description>
        ///     </item>
        ///     <item>
        ///         <term>Y-axis: Points left</term>
        ///         <description>Y-axis: Points up</description>
        ///     </item>
        ///     <item>
        ///         <term>Z-axis: Points up</term>
        ///         <description>Z-axis: Points forward</description>
        ///     </item>
        ///     <item>
        ///         <term>Rotation: Counter-clockwise</term>
        ///         <description>Rotation: Clockwise (left-handed)</description>
        ///     </item>
        /// </list>
        ///
        /// <para><b>TRANSLATION MAPPING:</b><br/>
        /// FRC X (forward) → Unity Z (forward)<br/>
        /// FRC Y (left) → Unity -X (right becomes left)<br/>
        /// FRC Z (up) → Unity Y (up)<br/>
        /// FRC θ (CCW) → Unity -Y rotation (CW)
        /// </para>
        ///
        /// <para><b>ROTATION MAPPING:</b></para>
        /// <para>
        /// Converting rotation requires remapping the axes and accounting for the change in "handedness".
        /// A robot's primary rotation in FRC is yaw, which is a counter-clockwise rotation around the FRC Z-axis (up).
        /// In Unity, the equivalent rotation is yaw around Unity's Y-axis (up). However, Unity's left-handed system
        /// means positive rotation is clockwise. Therefore, the angle must be inverted.
        /// </para>
        /// <para>
        /// This transformation is accomplished by shuffling and negating the quaternion components:
        /// <code>
        /// Unity.x =  FRC.y
        /// Unity.y = -FRC.z
        /// Unity.z = -FRC.x
        /// Unity.w =  FRC.w
        /// </code>
        /// This correctly maps the rotation from FRC's right-handed, Z-up frame to Unity's left-handed, Y-up frame.
        /// </para>
        /// </remarks>
        /// <param name="targetPose3d">Target position in FRC coordinates (meters, radians)</param>
        /// <returns>Position and rotation in Unity coordinate system</returns>
        public static (Vector3 position, Quaternion rotation) FrcToUnity3d(Pose3d targetPose3d)
        {
            // Convert 3D field position to 3D Unity position
            // FRC field is measured in meters with origin at blue alliance wall
            Vector3 unityPosition = new Vector3(
                (float)-targetPose3d.Y, // FRC Y (left) → Unity -X (convert left to right-handed)
                (float)targetPose3d.Z, // FRC Z (up) → Unity Y (up)
                (float)targetPose3d.X // FRC X (forward) → Unity Z (forward)
            );

            // Convert rotation
            var q = targetPose3d.Rotation.Quaternion;
            Quaternion unityRotation = new Quaternion(
                (float)q.Y, // FRC Y → Unity X
                (float)-q.Z, // FRC Z → Unity -Y (convert left to right-handed)
                (float)-q.X, // FRC X → Unity -Z (convert left to right-handed)
                (float)q.W // FRC W → Unity W
            );

            return (unityPosition, unityRotation);
        }

        /// <summary>
        /// Converts a pose from the Unity world coordinate system to the FRC field coordinate system.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs the inverse of <c>FrcToUnity3d</c>. It is primarily used to send data,
        /// such as a VR headset's pose, back to the robot in the coordinate system it understands.
        /// </para>
        /// <para><b>COORDINATE SYSTEM DIFFERENCES:</b></para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Unity World Coordinates (Left-Handed, Y-up)</term>
        ///         <description>FRC Field Coordinates (Right-Handed, Z-up)</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Origin: Arbitrary (set by VR tracking)</term>
        ///         <description>Origin: Blue alliance wall</description>
        ///     </item>
        ///     <item>
        ///         <term>X-axis: Points right</term>
        ///         <description>X-axis: Points toward red alliance (forward)</description>
        ///     </item>
        ///     <item>
        ///         <term>Y-axis: Points up</term>
        ///         <description>Y-axis: Points left</description>
        ///     </item>
        ///     <item>
        ///         <term>Z-axis: Points forward</term>
        ///         <description>Z-axis: Points up</description>
        ///     </item>
        ///     <item>
        ///         <term>Rotation: Clockwise (CW) positive</term>
        ///         <description>Rotation: Counter-clockwise (CCW) positive</description>
        ///     </item>
        /// </list>
        ///
        /// <para><b>TRANSLATION MAPPING:</b><br/>
        /// Unity Z (forward) → FRC X (forward)<br/>
        /// Unity X (right)   → FRC -Y (left)<br/>
        /// Unity Y (up)      → FRC Z (up)
        /// </para>
        ///
        /// <para><b>ROTATION MAPPING:</b></para>
        /// <para>
        /// To convert rotation from Unity back to FRC, the axes are remapped and the change in "handedness" is reversed.
        /// Unity's primary yaw rotation occurs around its Y-axis (up) in a clockwise direction. This is converted
        /// back to the FRC standard of yaw around its Z-axis (up) in a counter-clockwise direction.
        /// </para>
        /// <para>
        /// This inverse transformation is accomplished by the reverse quaternion component shuffle:
        /// <code>
        /// FRC.x = -Unity.z
        /// FRC.y =  Unity.x
        /// FRC.z = -Unity.y
        /// FRC.w =  Unity.w
        /// </code>
        /// This correctly maps the rotation from Unity's left-handed, Y-up frame back to FRC's right-handed, Z-up frame.
        /// </para>
        /// <para>
        /// USAGE:
        /// Called 100 times per second to stream headset position to robot.
        /// The robot uses this data for autonomous navigation, driver assistance,
        /// or any other functionality that needs to know where the driver is looking.
        /// </para>
        /// </remarks>
        /// <param name="unityPosition">VR headset position in Unity world coordinates</param>
        /// <param name="unityRotation">VR headset orientation in Unity world coordinates</param>
        /// <returns>3D pose in FRC field coordinates (meters, radians)</returns>
        public static Pose3d UnityToFrc3d(Vector3 unityPosition, Quaternion unityRotation)
        {
            return new Pose3d(
                new Translation3d(
                    unityPosition.z, // Unity Z → FRC X
                    -unityPosition.x, // Unity X → FRC -Y
                    unityPosition.y // Unity Y → FRC Z
                ),
                new Rotation3d(
                    new QuestNav.Geometry.Quaternion(
                        unityRotation.w, // Unity W → FRC W
                        -unityRotation.z, // Unity Z → FRC -X (convert right to left-handed)
                        unityRotation.x, // Unity X → FRC Y
                        -unityRotation.y // Unity Y → FRC -Z (convert right to left-handed)
                    )
                )
            );
        }

        /// <summary>
        /// Converts a Computer Vision (CV) pose (World-to-Camera) to an FRC field pose (Camera-in-World).
        /// </summary>
        /// <param name="rawPose">
        /// The pose directly from the definition libraries: translation t and rotation R in
        /// P_cv = R * P_world + t.
        /// </param>
        /// <returns>The pose of the camera in FRC field coordinates.</returns>
        public static Pose3d CvToFrc(Pose3d rawPose)
        {
            // 1. Invert the World-to-Camera transform to get Camera-in-World
            Rotation3d cvToWorld = -rawPose.Rotation;
            Translation3d cameraPosition = -rawPose.Translation.RotateBy(cvToWorld);

            // 2. Express the orientation in FRC body axes (X-Fwd, Y-Left, Z-Up) instead of CV axes
            Rotation3d cameraRotation = BodyToCv.RotateBy(cvToWorld);

            return new Pose3d(cameraPosition, cameraRotation);
        }
    }
}
