using System;
using NUnit.Framework;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using UnityEngine;
using FrcQuaternion = QuestNav.QuestNav.Geometry.Quaternion;
using Quaternion = UnityEngine.Quaternion;

namespace QuestNav.Tests
{
    public class ConversionsTests
    {
        private const double PositionTolerance = 1e-5;
        private const double AngleTolerance = 1e-5;

        // Rotates FRC body coordinates into CV camera coordinates, as a PnP solver's camera frame
        private static readonly Rotation3d BodyToCv = new Rotation3d(
            new FrcQuaternion(0.5, 0.5, -0.5, 0.5)
        );

        private static double Radians(double degrees) => degrees * Math.PI / 180.0;

        private static double AngleBetween(Rotation3d a, Rotation3d b)
        {
            double angle = a.Minus(b).Angle;
            return Math.Min(angle, 2 * Math.PI - angle);
        }

        private static Quaternion RandomRotation(System.Random random)
        {
            return Quaternion.Euler(
                (float)(random.NextDouble() * 360 - 180),
                (float)(random.NextDouble() * 360 - 180),
                (float)(random.NextDouble() * 360 - 180)
            );
        }

        private static Vector3 RandomPosition(System.Random random)
        {
            return new Vector3(
                (float)(random.NextDouble() * 20 - 10),
                (float)(random.NextDouble() * 4 - 2),
                (float)(random.NextDouble() * 20 - 10)
            );
        }

        /// <summary>Builds the world-to-camera pose a PnP solver reports for a camera at this FRC pose.</summary>
        private static Pose3d ToCvPose(Pose3d cameraInField)
        {
            Rotation3d worldToCv = (-cameraInField.Rotation).RotateBy(BodyToCv);
            return new Pose3d(-cameraInField.Translation.RotateBy(worldToCv), worldToCv);
        }

        [Test]
        public void UnityToFrc3d_MapsAxes()
        {
            var pose = Conversions.UnityToFrc3d(new Vector3(1f, 2f, 3f), Quaternion.identity);

            Assert.AreEqual(3.0, pose.X, PositionTolerance);
            Assert.AreEqual(-1.0, pose.Y, PositionTolerance);
            Assert.AreEqual(2.0, pose.Z, PositionTolerance);
        }

        [Test]
        public void UnityToFrc3d_TurningRightIsNegativeYaw()
        {
            var pose = Conversions.UnityToFrc3d(Vector3.zero, Quaternion.Euler(0f, 30f, 0f));

            Assert.AreEqual(Radians(-30), pose.Rotation.Z, AngleTolerance);
            Assert.AreEqual(0.0, pose.Rotation.X, AngleTolerance);
            Assert.AreEqual(0.0, pose.Rotation.Y, AngleTolerance);
        }

        [Test]
        public void UnityToFrc3d_LookingDownIsPositivePitch()
        {
            var pose = Conversions.UnityToFrc3d(Vector3.zero, Quaternion.Euler(30f, 0f, 0f));

            Assert.AreEqual(Radians(30), pose.Rotation.Y, AngleTolerance);
        }

        [Test]
        public void UnityToFrc3d_RotationAgreesWithPositionMapping()
        {
            var random = new System.Random(5152);
            for (int i = 0; i < 200; i++)
            {
                Quaternion rotation = RandomRotation(random);
                Vector3 vector = RandomPosition(random);

                // Rotating in Unity then converting must match converting then rotating in FRC
                var converted = Conversions.UnityToFrc3d(vector, rotation);
                var rotatedInUnity = Conversions.UnityToFrc3d(
                    rotation * vector,
                    Quaternion.identity
                );
                var rotatedInFrc = converted.Translation.RotateBy(converted.Rotation);

                Assert.Less(
                    rotatedInUnity.Translation.GetDistance(rotatedInFrc),
                    PositionTolerance
                );
            }
        }

        [Test]
        public void FrcToUnity3d_RoundTripsUnityToFrc3d()
        {
            var random = new System.Random(5152);
            for (int i = 0; i < 200; i++)
            {
                Vector3 position = RandomPosition(random);
                Quaternion rotation = RandomRotation(random);

                var (unityPosition, unityRotation) = Conversions.FrcToUnity3d(
                    Conversions.UnityToFrc3d(position, rotation)
                );

                Assert.Less(Vector3.Distance(position, unityPosition), 1e-5f);
                Assert.Less(Quaternion.Angle(rotation, unityRotation), 1e-3f);
            }
        }

        [Test]
        public void CvToFrc_CameraFacingBlueWall()
        {
            // Rows are the CV camera axes in field coordinates: right = +Y, down = -Z, forward = -X
            var worldToCv = new Rotation3d(
                new double[,]
                {
                    { 0, 1, 0 },
                    { 0, 0, -1 },
                    { -1, 0, 0 },
                }
            );
            var cvPose = new Pose3d(new Translation3d(-3, 1, 2), worldToCv);

            var camera = Conversions.CvToFrc(cvPose);

            Assert.AreEqual(2.0, camera.X, PositionTolerance);
            Assert.AreEqual(3.0, camera.Y, PositionTolerance);
            Assert.AreEqual(1.0, camera.Z, PositionTolerance);
            Assert.AreEqual(Math.PI, Math.Abs(camera.Rotation.Z), AngleTolerance);
            Assert.AreEqual(0.0, camera.Rotation.X, AngleTolerance);
            Assert.AreEqual(0.0, camera.Rotation.Y, AngleTolerance);
        }

        [Test]
        public void CvToFrc_InvertsSolverPose()
        {
            var random = new System.Random(5152);
            for (int i = 0; i < 200; i++)
            {
                var cameraInField = Conversions.UnityToFrc3d(
                    RandomPosition(random),
                    RandomRotation(random)
                );

                var recovered = Conversions.CvToFrc(ToCvPose(cameraInField));

                Assert.Less(
                    recovered.Translation.GetDistance(cameraInField.Translation),
                    PositionTolerance
                );
                Assert.Less(
                    AngleBetween(recovered.Rotation, cameraInField.Rotation),
                    AngleTolerance
                );
            }
        }

        [Test]
        public void LensOffset_RecoversHeadsetPose()
        {
            // Lens offset reported by a Quest 3 left passthrough camera
            var lensPosition = new Vector3(-0.0319f, -0.0172f, 0.0632f);
            var lensRotation = new Quaternion(0.0965f, 0.0050f, -0.0003f, 0.9953f).normalized;
            var headsetToCam = Conversions.UnityToFrc3d(lensPosition, lensRotation);
            var camToHeadset = new Transform3d(headsetToCam, Pose3d.Zero);

            var random = new System.Random(5152);
            for (int i = 0; i < 200; i++)
            {
                Vector3 headPosition = RandomPosition(random);
                Quaternion headRotation = RandomRotation(random);

                // Camera pose as Meta's PassthroughCameraAccess.GetCameraPose computes it
                var cameraInField = Conversions.UnityToFrc3d(
                    headPosition + headRotation * lensPosition,
                    headRotation * lensRotation
                );

                // Same steps AprilTagManager runs on a PoseLib result
                var headset = Conversions
                    .CvToFrc(ToCvPose(cameraInField))
                    .TransformBy(camToHeadset);

                var expected = Conversions.UnityToFrc3d(headPosition, headRotation);
                Assert.Less(headset.Translation.GetDistance(expected.Translation), 1e-4);
                Assert.Less(AngleBetween(headset.Rotation, expected.Rotation), 1e-4);
            }
        }
    }
}
