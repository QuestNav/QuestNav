using System;
using QuestNav.Core;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using UnityEngine;
using Wpi.Proto;
using Quaternion = UnityEngine.Quaternion;

namespace QuestNav.Commands.Commands
{
    /// <summary>
    /// Resets the VR camera pose and the Kalman filter to a specified position.
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
        /// Executes the pose reset by applying the target pose to the VR camera system
        /// and resetting the Kalman filter to match.
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

            // Step 1: Convert FRC field coordinates to Unity coordinates
            var (targetCameraPosition, targetCameraRotation) = Conversions.FrcToUnity3d(resetPose);

            // Step 2: Calculate rotation difference between current camera and target
            Quaternion newRotation =
                targetCameraRotation * Quaternion.Inverse(vrCamera.localRotation);

            // Step 3: Apply rotation to root
            vrCameraRoot.rotation = newRotation;

            // Step 4: Recalculate position after rotation
            Vector3 newRootPosition = targetCameraPosition - (newRotation * vrCamera.localPosition);

            // Step 5: Apply the new position to vrCameraRoot
            vrCameraRoot.position = newRootPosition;

            // Step 6: Reset the Kalman filter so it agrees with the new VIO reference frame.
            // After moving vrCameraRoot, the next VIO reading will be relative to this new
            // origin. The filter must be told so it doesn't interpret the jump as displacement.
            // The camera now sits at resetPose, which is also what the next VIO frame reports.
            poseEstimator.ResetPosition(resetPose, Time.timeAsDouble);

            QueuedLogger.Log(
                $"Pose reset applied: X={poseX}, Y={poseY}, Z={poseZ} Rotation W={poseQW}, X={poseQx}, "
                    + $"Y={poseQy}, Z={poseQz}"
            );

            commandContext.SendSuccessResponse(receivedCommand.CommandId);
            QueuedLogger.Log("Pose reset completed successfully");
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
