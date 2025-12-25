// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
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
        public static (Vector3 position, Quaternion rotation) FrcToUnity3d(
            ProtobufPose3d targetPose3d
        )
        {
            // Convert 3D field position to 3D Unity position
            // FRC field is measured in meters with origin at blue alliance wall
            Vector3 unityPosition = new Vector3(
                (float)-targetPose3d.Translation.Y, // FRC Y (left) → Unity -X (convert left to right-handed)
                (float)targetPose3d.Translation.Z, // FRC Z (up) → Unity Y (up)
                (float)targetPose3d.Translation.X // FRC X (forward) → Unity Z (forward)
            );

            // Convert rotation
            Quaternion unityRotation = new Quaternion(
                (float)targetPose3d.Rotation.Q.Y, // FRC Y → Unity X
                (float)-targetPose3d.Rotation.Q.Z, // FRC Z → Unity -Y (convert left to right-handed)
                (float)-targetPose3d.Rotation.Q.X, // FRC X → Unity -Z (convert left to right-handed)
                (float)targetPose3d.Rotation.Q.W // FRC W → Unity W
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
        /// <returns>2D pose in FRC field coordinates (meters, radians)</returns>
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
                        X = -unityRotation.z, // Unity Z → FRC -X (convert right to left-handed)
                        Y = unityRotation.x, // Unity X → FRC Y
                        Z = -unityRotation.y, // Unity Y → FRC -Z (convert right to left-handed)
                        W = unityRotation.w, // Unity W → FRC W
                    },
                },
            };
        }

        /// <summary>
        /// Converts a protobuf Pose3d to Unity Vector3 and Quaternion types.
        /// This is a utility method for extracting Unity-native types from the protobuf message structure.
        /// </summary>
        /// <param name="pose">Protobuf pose containing translation and rotation</param>
        /// <returns>Tuple of Unity position and rotation</returns>
        public static (Vector3 position, Quaternion rotation) ProtobufPose3dToUnity(
            ProtobufPose3d pose
        )
        {
            Vector3 position = new Vector3(
                (float)pose.Translation.X,
                (float)pose.Translation.Y,
                (float)pose.Translation.Z
            );

            Quaternion rotation = new Quaternion(
                (float)pose.Rotation.Q.X,
                (float)pose.Rotation.Q.Y,
                (float)pose.Rotation.Q.Z,
                (float)pose.Rotation.Q.W
            );

            return (position, rotation);
        }
    }
}
