using System;
using NUnit.Framework;
using QuestNav.Commands.Commands;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

namespace QuestNav.Tests
{
    public class PoseResetCommandTests
    {
        private const double Tolerance = 1e-9;
        private const double FloatTolerance = 1e-5;

        // A headset tilted the way a real mount is, at some spot in its tracking space
        private static readonly Pose3d TiltedLocalPose = new Pose3d(
            0.2,
            -0.1,
            1.1,
            new Rotation3d(0.05, -0.12, 0.4)
        );

        private static double AngleDiff(double a, double b)
        {
            return Math.Abs(Math.IEEERemainder(a - b, 2 * Math.PI));
        }

        private static double AngleBetween(Rotation3d a, Rotation3d b)
        {
            double angle = a.Minus(b).Angle;
            return Math.Min(angle, 2 * Math.PI - angle);
        }

        [Test]
        public void LevelTarget_KeepsHeadsetPitchAndRoll()
        {
            var target = new Pose3d(5.0, 3.0, 0.5, new Rotation3d(0, 0, 1.3));

            var (rootPose, cameraPose) = PoseResetCommand.SolveYawOnlyRootPose(
                target,
                TiltedLocalPose
            );

            Assert.AreEqual(0.0, cameraPose.Translation.GetDistance(target.Translation), Tolerance);
            Assert.AreEqual(0.0, AngleDiff(1.3, cameraPose.Rotation.Z), Tolerance);
            Assert.AreEqual(TiltedLocalPose.Rotation.X, cameraPose.Rotation.X, Tolerance);
            Assert.AreEqual(TiltedLocalPose.Rotation.Y, cameraPose.Rotation.Y, Tolerance);

            // The root only turns about vertical
            Assert.AreEqual(0.0, rootPose.Rotation.X, Tolerance);
            Assert.AreEqual(0.0, rootPose.Rotation.Y, Tolerance);
        }

        [Test]
        public void TargetPitchAndRollAreIgnored()
        {
            var level = new Pose3d(2.0, 1.0, 0.4, new Rotation3d(0, 0, -2.0));
            var tilted = new Pose3d(2.0, 1.0, 0.4, new Rotation3d(0.3, 0.2, -2.0));

            var fromLevel = PoseResetCommand.SolveYawOnlyRootPose(level, TiltedLocalPose);
            var fromTilted = PoseResetCommand.SolveYawOnlyRootPose(tilted, TiltedLocalPose);

            Assert.AreEqual(
                0.0,
                AngleBetween(fromLevel.rootPose.Rotation, fromTilted.rootPose.Rotation),
                Tolerance
            );
            Assert.AreEqual(
                0.0,
                fromLevel.rootPose.Translation.GetDistance(fromTilted.rootPose.Translation),
                Tolerance
            );
        }

        [Test]
        public void RootComposedWithLocalPose_GivesCameraPose()
        {
            var target = new Pose3d(12.0, 6.5, 0.6, new Rotation3d(0, 0, 3.0));
            var local = new Pose3d(-1.0, 2.0, 1.2, new Rotation3d(-0.04, 0.1, -3.0));

            var (rootPose, cameraPose) = PoseResetCommand.SolveYawOnlyRootPose(target, local);

            var composedTranslation =
                rootPose.Translation + local.Translation.RotateBy(rootPose.Rotation);
            var composedRotation = local.Rotation.RotateBy(rootPose.Rotation);
            Assert.AreEqual(
                0.0,
                composedTranslation.GetDistance(cameraPose.Translation),
                Tolerance
            );
            Assert.AreEqual(0.0, AngleBetween(composedRotation, cameraPose.Rotation), Tolerance);
            Assert.AreEqual(0.0, AngleDiff(3.0, cameraPose.Rotation.Z), Tolerance);
        }

        [Test]
        public void MatchingTilt_SameRootAsFullRotationSolve()
        {
            var target = new Pose3d(3.0, 4.0, 0.5, new Rotation3d(0.05, -0.12, 2.2));

            var (rootPose, _) = PoseResetCommand.SolveYawOnlyRootPose(target, TiltedLocalPose);

            // The full solve: root = target * inverse(local)
            var fullRoot = new Rotation3d(
                target.Rotation.Quaternion * TiltedLocalPose.Rotation.Quaternion.Inverse()
            );
            Assert.AreEqual(0.0, AngleBetween(fullRoot, rootPose.Rotation), Tolerance);
        }

        [Test]
        public void UnityRootTransform_PutsCameraAtSolvedPose()
        {
            var target = new Pose3d(7.0, 2.0, 0.55, new Rotation3d(0, 0, -0.7));
            var (localPosition, localRotation) = Conversions.FrcToUnity3d(TiltedLocalPose);
            var cameraLocal = Conversions.UnityToFrc3d(localPosition, localRotation);

            var (rootPose, cameraPose) = PoseResetCommand.SolveYawOnlyRootPose(target, cameraLocal);

            // What Unity does with the root transform and the camera's local pose
            var (rootPosition, rootRotation) = Conversions.FrcToUnity3d(rootPose);
            Vector3 worldPosition = rootPosition + rootRotation * localPosition;
            Quaternion worldRotation = rootRotation * localRotation;
            var world = Conversions.UnityToFrc3d(worldPosition, worldRotation);

            Assert.AreEqual(
                0.0,
                world.Translation.GetDistance(cameraPose.Translation),
                FloatTolerance
            );
            Assert.AreEqual(0.0, AngleBetween(world.Rotation, cameraPose.Rotation), FloatTolerance);
            Assert.AreEqual(0.0, AngleDiff(-0.7, world.Rotation.Z), FloatTolerance);
        }
    }
}
