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
        private readonly Field2d field;

        /// <summary>
        /// Initializes a new instance of the PoseResetCommand.
        /// </summary>
        /// <param name="field">
        /// Dimensions of the loaded field layout, used only to warn about out-of-bounds resets;
        /// null skips the check.
        /// </param>
        public PoseResetCommand(
            ICommandContext commandContext,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform,
            IVioAprilTagPoseEstimator poseEstimator,
            Field2d field = null
        )
        {
            this.commandContext = commandContext;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            this.poseEstimator = poseEstimator;
            this.field = field;
        }

        /// <summary>
        /// The formatted name for PoseResetCommand.
        /// </summary>
        public string CommandNiceName => "PoseReset";

        /// <summary>
        /// Executes the pose reset with no latency compensation.
        /// </summary>
        public void Execute(ProtobufQuestNavCommand receivedCommand) => Execute(receivedCommand, 0);

        /// <summary>
        /// Executes the pose reset by moving the VR camera root so the camera sits at the
        /// target position with the target yaw, and resetting the Kalman filter to match.
        /// </summary>
        /// <param name="receivedCommand">The command data received from the robot</param>
        /// <param name="ageMs">
        /// How long ago the robot sent the command; 0 or less when unknown. The target is the
        /// pose at that time, so the VIO motion since is added to it.
        /// </param>
        public void Execute(ProtobufQuestNavCommand receivedCommand, long ageMs)
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

            if (IsOutsideField(poseX, poseY, field))
            {
                QueuedLogger.LogWarning($"Pose out of field bounds: ({poseX}, {poseY})");
            }

            // Solved in FRC coordinates; the camera's local pose is relative to the root
            var cameraLocalPose = Conversions.UnityToFrc3d(
                vrCamera.localPosition,
                vrCamera.localRotation
            );
            var (rootPose, cameraPose) = SolveYawOnlyRootPose(
                CompensateForAge(resetPose, ageMs),
                cameraLocalPose
            );

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
        /// Moves the target by the VIO motion since the command was sent. The age is limited
        /// to the estimator's snapshot buffer.
        /// </summary>
        private Pose3d CompensateForAge(Pose3d targetPose, long ageMs)
        {
            if (
                ageMs <= 0
                || !poseEstimator.TryGetVioMotionSince(
                    Time.timeAsDouble - ageMs / 1000.0,
                    out var vioAtSend,
                    out var vioNow
                )
            )
                return targetPose;

            var compensated = ApplyMotionSince(targetPose, vioAtSend, vioNow);
            QueuedLogger.Log(
                $"Pose reset compensated for {ageMs} ms of motion: moved "
                    + $"{compensated.Translation.GetDistance(targetPose.Translation):F3} m"
            );
            return compensated;
        }

        /// <summary>
        /// Moves a target pose by the VIO motion between two VIO poses. The VIO frame maps
        /// onto the field by the yaw between the target and <paramref name="vioThen"/>, so
        /// the displacement is rotated by that, and the yaw change is added to the target.
        /// </summary>
        /// <param name="targetPose">Field pose of the camera at the time of <paramref name="vioThen"/>.</param>
        /// <param name="vioThen">Raw VIO pose of the camera when the target applied.</param>
        /// <param name="vioNow">Latest raw VIO pose of the camera.</param>
        /// <returns>The field pose of the camera now.</returns>
        public static Pose3d ApplyMotionSince(Pose3d targetPose, Pose3d vioThen, Pose3d vioNow)
        {
            var vioToField = new Rotation3d(0, 0, targetPose.Rotation.Z - vioThen.Rotation.Z);
            var displacement = (vioNow.Translation - vioThen.Translation).RotateBy(vioToField);
            var yawChange = new Rotation3d(0, 0, vioNow.Rotation.Z - vioThen.Rotation.Z);
            return new Pose3d(
                targetPose.Translation + displacement,
                targetPose.Rotation.RotateBy(yawChange)
            );
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
        /// Whether a position is clearly outside the field, allowing
        /// <see cref="QuestNavConstants.Field.BOUNDS_MARGIN_METERS"/> for measurement error.
        /// Returns false when the field size is unknown.
        /// </summary>
        public static bool IsOutsideField(double x, double y, Field2d field)
        {
            if (field == null || !(field.Length > 0) || !(field.Width > 0))
                return false;

            const double margin = QuestNavConstants.Field.BOUNDS_MARGIN_METERS;
            return x < -margin
                || x > field.Length + margin
                || y < -margin
                || y > field.Width + margin;
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
