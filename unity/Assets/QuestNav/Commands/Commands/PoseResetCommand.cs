using System;
using QuestNav.Core;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using UnityEngine;
using Wpi.Proto;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera pose and the Kalman filter to a specified position and heading.
    /// Only the yaw of the requested rotation is applied; pitch and roll stay with the
    /// gravity-aligned VIO tracking.
    /// </summary>
    public class PoseResetCommand : ICommand
    {
        private const double MIN_QUATERNION_NORM = 1e-6;

        private readonly ICommandContext commandContext;
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;
        private readonly IVioAprilTagPoseEstimator poseEstimator;

        /// <summary>
        /// Initializes a new instance of the PoseResetCommand.
        /// </summary>
        public PoseResetCommand(
            ICommandContext commandContext,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform,
            IVioAprilTagPoseEstimator poseEstimator
        )
        {
            this.commandContext = commandContext;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            this.poseEstimator = poseEstimator;
        }

        /// <summary>
        /// The formatted name for PoseResetCommand.
        /// </summary>
        public string CommandNiceName => "PoseReset";

        /// <summary>
        /// Executes the pose reset by moving the VR camera root so the camera sits at the
        /// target position with the target yaw, and resetting the Kalman filter to match.
        /// </summary>
        public void Execute(ProtobufQuestNavCommand receivedCommand)
        {
            QueuedLogger.Log("Received pose reset request, initiating reset...");

            // Validate the raw protobuf; Pose3d normalizes the quaternion and would hide bad input
            var targetPose = receivedCommand.PoseResetPayload?.TargetPose;
            if (!IsValidPose(targetPose))
            {
                commandContext.SendErrorResponse(
                    receivedCommand.CommandId,
                    "Failed to get valid pose data (invalid)"
                );
                QueuedLogger.LogError("Failed to get valid pose data");
                return;
            }

            Pose3d resetPose = new Pose3d(targetPose);
            double poseX = resetPose.X;
            double poseY = resetPose.Y;
            double poseZ = resetPose.Z;
            double poseQx = resetPose.Rotation.Quaternion.X;
            double poseQy = resetPose.Rotation.Quaternion.Y;
            double poseQz = resetPose.Rotation.Quaternion.Z;
            double poseQW = resetPose.Rotation.Quaternion.W;

            // Additional validation for field boundaries
            bool inBounds =
                poseX >= 0
                && poseX <= QuestNavConstants.Field.FIELD_LENGTH
                && poseY >= 0
                && poseY <= QuestNavConstants.Field.FIELD_WIDTH;

            if (!inBounds)
            {
                QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
            }

            // Solved in FRC coordinates; the camera's local pose is relative to the root
            var cameraLocalPose = Conversions.UnityToFrc3d(
                vrCamera.localPosition,
                vrCamera.localRotation
            );
            var (rootPose, cameraPose) = SolveYawOnlyRootPose(resetPose, cameraLocalPose);

            var (rootPosition, rootRotation) = Conversions.FrcToUnity3d(rootPose);
            vrCameraRoot.rotation = rootRotation;
            vrCameraRoot.position = rootPosition;

            // Reset the Kalman filter so it agrees with the new VIO reference frame.
            // After moving vrCameraRoot, the next VIO reading will be relative to this new
            // origin. The filter must be told so it doesn't interpret the jump as displacement.
            // The camera now sits at cameraPose, which is also what the next VIO frame reports.
            poseEstimator.ResetPosition(cameraPose, Time.timeAsDouble);

            QueuedLogger.Log(
                $"Pose reset applied: X={poseX}, Y={poseY}, Z={poseZ} Rotation W={poseQW}, X={poseQx}, "
                    + $"Y={poseQy}, Z={poseQz}"
            );

            commandContext.SendSuccessResponse(receivedCommand.CommandId);
            QueuedLogger.Log("Pose reset completed successfully");
        }

        /// <summary>
        /// Solves the camera root pose that puts the camera at the target position with the
        /// target yaw, all in FRC coordinates. The root only rotates about the vertical axis,
        /// so the camera keeps the pitch and roll it already has in the gravity-aligned
        /// tracking space.
        /// </summary>
        /// <param name="targetPose">Requested field pose of the camera.</param>
        /// <param name="cameraLocalPose">Camera pose relative to the root.</param>
        /// <returns>The root pose and the resulting field pose of the camera.</returns>
        public static (Pose3d rootPose, Pose3d cameraPose) SolveYawOnlyRootPose(
            Pose3d targetPose,
            Pose3d cameraLocalPose
        )
        {
            // Rz(a) * R has yaw yaw(R) + a, so this root gives the camera the target yaw
            var rootRotation = new Rotation3d(
                0,
                0,
                targetPose.Rotation.Z - cameraLocalPose.Rotation.Z
            );
            var rootTranslation =
                targetPose.Translation - cameraLocalPose.Translation.RotateBy(rootRotation);
            var cameraRotation = cameraLocalPose.Rotation.RotateBy(rootRotation);

            return (
                new Pose3d(rootTranslation, rootRotation),
                new Pose3d(targetPose.Translation, cameraRotation)
            );
        }

        /// <summary>
        /// Checks that the pose is present, finite, and has a non-degenerate quaternion.
        /// </summary>
        private static bool IsValidPose(ProtobufPose3d pose)
        {
            var t = pose?.Translation;
            var q = pose?.Rotation?.Q;
            if (t == null || q == null)
                return false;

            // A NaN or infinite component makes the norm non-finite
            double qNorm = Math.Sqrt(q.W * q.W + q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            return double.IsFinite(t.X)
                && double.IsFinite(t.Y)
                && double.IsFinite(t.Z)
                && double.IsFinite(qNorm)
                && qNorm > MIN_QUATERNION_NORM;
        }
    }
}
