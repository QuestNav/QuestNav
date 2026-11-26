using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using NUnit.Framework;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.Tests
{
    public class VioAprilTagPoseEstimatorTests
    {
        private const double Dt = 1.0 / 120.0;
        private const double Tolerance = 1e-6;

        // One VIO step of motion is dropped when re-basing after a recenter
        private const double RecenterTolerance = 0.02;

        private const int GoodTags = 3;
        private const double GoodInlierRatio = 0.9;

        private static readonly Matrix<double> TagStdDevs = StdDevs(0.05);

        private static Matrix<double> StdDevs(double sigma)
        {
            return DenseMatrix.OfArray(
                new[,]
                {
                    { sigma },
                    { sigma },
                    { sigma },
                }
            );
        }

        private static double AngleDiff(double a, double b)
        {
            return Math.Abs(Math.IEEERemainder(a - b, 2 * Math.PI));
        }

        /// <summary>
        /// Synthetic headset: moves through the field, and reports its pose in a VIO frame
        /// that is rotated by <see cref="VioYaw"/> and shifted by <see cref="VioOffset"/>.
        /// </summary>
        private class Sim
        {
            public readonly VioAprilTagPoseEstimator Estimator = new VioAprilTagPoseEstimator();
            public double VioYaw = 0.6;
            public Translation3d VioOffset = new Translation3d(-1.5, 2.0, 0.1);
            public Pose3d Field = new Pose3d(4.0, 3.0, 0.5, new Rotation3d(0.02, -0.05, 0.3));
            public double Time;

            public Pose3d Vio
            {
                get
                {
                    var frame = new Rotation3d(0, 0, VioYaw);
                    return new Pose3d(
                        Field.Translation.RotateBy(frame) + VioOffset,
                        Field.Rotation.RotateBy(frame)
                    );
                }
            }

            public void Step(Translation3d fieldVelocity, int steps = 1, double dt = Dt)
            {
                for (int i = 0; i < steps; i++)
                {
                    Time += dt;
                    Field = new Pose3d(Field.Translation + fieldVelocity * dt, Field.Rotation);
                    Estimator.AddVioObservation(Vio, Time);
                }
            }

            /// <summary>Recenters the VIO frame: rotates it about vertical and moves its origin.</summary>
            public void Recenter(double yaw, Translation3d shift)
            {
                var rotation = new Rotation3d(0, 0, yaw);
                VioYaw += yaw;
                VioOffset = VioOffset.RotateBy(rotation) + shift;
                Estimator.HandleRecenter();
            }

            /// <summary>Loses tracking while the robot drives; no VIO reaches the estimator.</summary>
            public void Outage(Translation3d fieldVelocity, double seconds)
            {
                Estimator.HandleTrackingLost();
                Time += seconds;
                Field = new Pose3d(Field.Translation + fieldVelocity * seconds, Field.Rotation);
            }

            public void Observe(Pose3d measured, double captureTime, int tags = GoodTags)
            {
                Estimator.AddAprilTagObservation(
                    measured.Translation,
                    measured.Rotation,
                    captureTime,
                    TagStdDevs,
                    tags,
                    GoodInlierRatio
                );
            }

            public void ObserveTruth()
            {
                Observe(Field, Time);
            }

            /// <summary>Starts the stream and aligns on a perfect observation.</summary>
            public void Align()
            {
                Estimator.AddVioObservation(Vio, Time);
                Step(Translation3d.Zero, 5);
                ObserveTruth();
            }

            public double PositionError =>
                Estimator.EstimatedPose.Translation.GetDistance(Field.Translation);

            /// <summary>
            /// Applies a correction 0.3 m off the truth, inside the gate's minimum distance,
            /// and returns the fraction of it the estimate moved: the Kalman gain.
            /// </summary>
            public double CorrectionGain()
            {
                var before = Estimator.EstimatedPose.Translation;
                Observe(new Pose3d(Field.Translation + GainProbeOffset, Field.Rotation), Time);
                return (Estimator.EstimatedPose.Translation - before).X / GainProbeOffset.X;
            }
        }

        private static readonly Translation3d GainProbeOffset = new Translation3d(0.3, 0.0, 0.0);

        [Test]
        public void FirstVioObservation_InitializesAtVioPose()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, 0.0);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(
                0.0,
                sim.Estimator.EstimatedPose.Translation.GetDistance(sim.Vio.Translation),
                Tolerance
            );
            Assert.AreEqual(sim.Vio.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z, Tolerance);
        }

        [Test]
        public void Phase1_AlignsPositionAndYawWithLatency()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, sim.Time);
            var velocity = new Translation3d(1.0, 0.5, 0.0);
            sim.Step(velocity, 20);

            // Measurement captured 10 VIO steps ago, delivered now
            var captured = sim.Field;
            double captureTime = sim.Time;
            sim.Step(velocity, 10);
            sim.Observe(captured, captureTime);

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
            Assert.AreEqual(
                0.0,
                AngleDiff(sim.Field.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );

            // Later VIO motion is rotated into the field frame
            sim.Step(velocity, 60);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void Phase1_RejectsLowQualityObservation()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, sim.Time);
            sim.Step(Translation3d.Zero, 5);

            sim.Observe(sim.Field, sim.Time, tags: 1);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
        }

        [Test]
        public void Phase2_CorrectsVioDrift()
        {
            var sim = new Sim();
            sim.Align();
            sim.Step(Translation3d.Zero, 20);

            // VIO drift: the VIO frame slips 0.3 m, which the estimator integrates as motion
            sim.VioOffset += new Translation3d(0.3, 0.0, 0.0);
            sim.Step(Translation3d.Zero, 1);
            double errorBefore = sim.PositionError;
            Assert.AreEqual(0.3, errorBefore, 1e-9);

            for (int i = 0; i < 5; i++)
            {
                sim.ObserveTruth();
                sim.Step(Translation3d.Zero, 4);
            }

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.Less(sim.PositionError, 0.05);
        }

        [Test]
        public void Phase2_GatesAgainstCaptureTimeEstimate()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(4.5, 0.0, 0.0);

            // 0.3 s latency at 4.5 m/s: the measurement is 1.35 m behind the present estimate
            for (int i = 0; i < VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT; i++)
            {
                var captured = sim.Field;
                double captureTime = sim.Time;
                sim.Step(velocity, 36);
                sim.Observe(captured, captureTime);
            }

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, 1e-3);
        }

        [Test]
        public void Phase2_RejectsSingleOutlier()
        {
            var sim = new Sim();
            sim.Align();
            sim.Step(Translation3d.Zero, 10);
            var before = sim.Estimator.EstimatedPose;

            // Within the 2 m absolute cap, but far outside the covariance
            sim.Observe(
                new Pose3d(
                    sim.Field.Translation + new Translation3d(1.5, 0.0, 0.0),
                    sim.Field.Rotation
                ),
                sim.Time
            );

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(
                0.0,
                sim.Estimator.EstimatedPose.Translation.GetDistance(before.Translation),
                Tolerance
            );
        }

        [Test]
        public void Phase2_AcceptsSmallBiasBeyondTightCovariance()
        {
            var sim = new Sim();
            sim.Align();
            var tightStdDevs = StdDevs(0.006);
            for (int i = 0; i < 10; i++)
            {
                sim.Step(Translation3d.Zero, 2);
                sim.Estimator.AddAprilTagObservation(
                    sim.Field.Translation,
                    sim.Field.Rotation,
                    sim.Time,
                    tightStdDevs,
                    GoodTags,
                    GoodInlierRatio
                );
            }

            // A 0.3 m bias is far outside S = P + R here, but within the gate's minimum distance
            var biased = new Translation3d(0.3, 0.0, 0.0);
            for (int i = 0; i < 2 * VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT; i++)
            {
                sim.Step(Translation3d.Zero, 2);
                sim.Estimator.AddAprilTagObservation(
                    sim.Field.Translation + biased,
                    sim.Field.Rotation,
                    sim.Time,
                    tightStdDevs,
                    GoodTags,
                    GoodInlierRatio
                );

                Assert.IsTrue(sim.Estimator.HasInitialAlignment);
                if (i == 0)
                    Assert.Greater(sim.PositionError, 0.2);
            }

            Assert.AreEqual(
                0.0,
                sim.Estimator.EstimatedPose.Translation.GetDistance(sim.Field.Translation + biased),
                0.01
            );
        }

        [Test]
        public void ProcessNoise_ScalesWithElapsedTime()
        {
            var fast = new Sim();
            fast.Align();
            fast.Step(Translation3d.Zero, 30);

            var slow = new Sim();
            slow.Align();
            slow.Step(Translation3d.Zero, 15, 2 * Dt);

            double fastGain = fast.CorrectionGain();
            Assert.Greater(fastGain, 0.0);
            Assert.AreEqual(fastGain, slow.CorrectionGain(), 1e-9);
        }

        [Test]
        public void ProcessNoise_ClampsLongSteps()
        {
            var hitch = new Sim();
            hitch.Align();
            hitch.Step(Translation3d.Zero, 1, 2.0);

            var capped = new Sim();
            capped.Align();
            capped.Step(
                Translation3d.Zero,
                1,
                VioAprilTagPoseEstimatorConstants.VIO_NOISE_MAX_DT_SECONDS
            );

            Assert.AreEqual(capped.CorrectionGain(), hitch.CorrectionGain(), 1e-9);
        }

        [Test]
        public void Phase2_LatentCorrectionMatchesImmediateCorrection()
        {
            var velocity = new Translation3d(1.0, -0.5, 0.0);
            var offset = new Translation3d(0.2, 0.1, 0.0);

            // Applied when captured
            var immediate = new Sim();
            immediate.Align();
            immediate.Step(velocity, 20);
            immediate.Observe(
                new Pose3d(immediate.Field.Translation + offset, immediate.Field.Rotation),
                immediate.Time
            );
            immediate.Step(velocity, 20);

            // Same capture, delivered 20 VIO steps later
            var latent = new Sim();
            latent.Align();
            latent.Step(velocity, 20);
            var captured = new Pose3d(latent.Field.Translation + offset, latent.Field.Rotation);
            double captureTime = latent.Time;
            latent.Step(velocity, 20);
            latent.Observe(captured, captureTime);

            Assert.AreEqual(
                0.0,
                latent.Estimator.EstimatedPose.Translation.GetDistance(
                    immediate.Estimator.EstimatedPose.Translation
                ),
                1e-9
            );

            // Replay adds the process noise since capture once, so the covariance agrees too
            Assert.AreEqual(immediate.CorrectionGain(), latent.CorrectionGain(), 1e-9);
        }

        [Test]
        public void AprilTagObservation_OlderThanBufferIsIgnored()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, sim.Time);
            var captured = sim.Field;
            double captureTime = sim.Time - Dt;
            sim.Step(Translation3d.Zero, 5);

            sim.Observe(captured, captureTime);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
        }

        [Test]
        public void AprilTagObservation_PastVioGapIsIgnored()
        {
            var sim = new Sim();
            sim.Align();
            var before = sim.Estimator.EstimatedPose;

            // No VIO since the last snapshot to compensate a capture this far past it
            sim.Observe(
                new Pose3d(
                    sim.Field.Translation + new Translation3d(0.3, 0.0, 0.0),
                    sim.Field.Rotation
                ),
                sim.Time + 2 * VioAprilTagPoseEstimatorConstants.MAX_CAPTURE_VIO_GAP_SECONDS
            );

            Assert.AreEqual(before, sim.Estimator.EstimatedPose);
        }

        [Test]
        public void Phase2_ConsistentRejectionsDropBackToPhase1()
        {
            var sim = new Sim();
            sim.Align();

            // The estimate is now 1 m off (e.g. a relocalization jump); the tags agree on the truth
            sim.VioOffset += new Translation3d(0.0, 1.0, 0.0);
            sim.Step(Translation3d.Zero, 1);

            for (int i = 0; i < VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT - 1; i++)
            {
                sim.ObserveTruth();
                sim.Step(Translation3d.Zero, 4);
            }
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);

            sim.ObserveTruth();
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            sim.Step(Translation3d.Zero, 4);
            sim.ObserveTruth();
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void Phase2_RealignNeedsPresetQuality()
        {
            var sim = new Sim();
            sim.Align();
            sim.VioOffset += new Translation3d(0.0, 1.0, 0.0);
            sim.Step(Translation3d.Zero, 1);
            for (int i = 0; i < VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT; i++)
            {
                sim.ObserveTruth();
                sim.Step(Translation3d.Zero, 4);
            }
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            // Enough for the initial alignment, but below the Balanced preset
            sim.Observe(
                sim.Field,
                sim.Time,
                tags: VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_TAGS
            );
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                sim.Field.Rotation,
                sim.Time,
                TagStdDevs,
                GoodTags,
                VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_INLIER_RATIO
            );
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            sim.ObserveTruth();
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);

            // Later losses of alignment start from the initial bar again
            sim.Estimator.ResetPosition(sim.Vio, sim.Time);
            sim.Step(Translation3d.Zero, 1);
            sim.Observe(
                sim.Field,
                sim.Time,
                tags: VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_TAGS
            );
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
        }

        [Test]
        public void Phase2_InconsistentRejectionsKeepAlignment()
        {
            var sim = new Sim();
            sim.Align();

            for (int i = 0; i < 4 * VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT; i++)
            {
                double side = i % 2 == 0 ? 1.0 : -1.0;
                sim.Observe(
                    new Pose3d(
                        sim.Field.Translation + new Translation3d(side, 0.0, 0.0),
                        sim.Field.Rotation
                    ),
                    sim.Time
                );
                sim.Step(Translation3d.Zero, 4);
            }

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void Phase2_SingleTagRejectionsDoNotDropBack()
        {
            var sim = new Sim();
            sim.Estimator.SetConfidencePreset(ConfidencePreset.Debug);
            sim.Estimator.SetMinimumTags(1);
            sim.Align();

            var offset = new Pose3d(
                sim.Field.Translation + new Translation3d(1.0, 0.0, 0.0),
                sim.Field.Rotation
            );
            for (int i = 0; i < VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT; i++)
            {
                sim.Observe(offset, sim.Time, tags: 1);
                sim.Step(Translation3d.Zero, 1);
            }

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
        }

        [Test]
        public void Recenter_KeepsHeadingAndTravelDirection()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(1.0, 0.0, 0.0);
            sim.Step(velocity, 30);
            double headingBefore = sim.Estimator.EstimatedPose.Rotation.Z;

            sim.Recenter(1.2, new Translation3d(3.0, -2.0, 0.0));
            sim.Step(velocity, 1);

            Assert.AreEqual(
                0.0,
                AngleDiff(headingBefore, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );
            Assert.Less(sim.PositionError, RecenterTolerance);

            var start = sim.Estimator.EstimatedPose.Translation;
            sim.Step(velocity, 120);
            var travel = sim.Estimator.EstimatedPose.Translation - start;

            Assert.AreEqual(1.0, travel.X, Tolerance);
            Assert.AreEqual(0.0, travel.Y, Tolerance);
            Assert.AreEqual(0.0, travel.Z, Tolerance);
            Assert.AreEqual(
                0.0,
                AngleDiff(sim.Field.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );
        }

        [Test]
        public void Recenter_LatentObservationFromBeforeRecenterIsConsistent()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(0.0, 1.0, 0.0);
            sim.Step(velocity, 20);

            var captured = sim.Field;
            double captureTime = sim.Time;
            sim.Step(velocity, 5);
            sim.Recenter(-2.0, new Translation3d(-1.0, 4.0, 0.0));
            sim.Step(velocity, 10);

            // Replay from the pre-recenter snapshot must not integrate the recenter jump
            sim.Observe(captured, captureTime);

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.Less(sim.PositionError, RecenterTolerance);
        }

        [Test]
        public void Recenter_BeforeAlignmentAlignsNormally()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, sim.Time);
            sim.Step(Translation3d.Zero, 5);
            sim.Recenter(0.8, new Translation3d(1.0, 1.0, 0.0));
            sim.Step(Translation3d.Zero, 5);

            sim.ObserveTruth();
            sim.Step(new Translation3d(0.5, 0.5, 0.0), 60);

            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
            Assert.AreEqual(
                0.0,
                AngleDiff(sim.Field.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );
        }

        [Test]
        public void TrackingOutage_PlausibleJumpIsIntegrated()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(3.0, -1.0, 0.0);
            sim.Step(velocity, 10);

            sim.Outage(velocity, 0.8);
            sim.Step(velocity, 1);

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void TrackingOutage_IntegratedJumpErrorIsCorrected()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(1.2, 0.5, 0.0);
            sim.Step(velocity, 10);

            // The VIO comes back 0.8 m off, beyond the gate's minimum distance but within
            // what the robot could have driven over the outage
            sim.Outage(velocity, 3.0);
            sim.VioOffset += new Translation3d(0.8, 0.0, 0.0);
            sim.Step(velocity, 1);
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.8, sim.PositionError, 1e-9);

            sim.ObserveTruth();

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.Less(sim.PositionError, 0.05);
        }

        [Test]
        public void TrackingOutage_LatentCorrectionKeepsOutageNoise()
        {
            var velocity = new Translation3d(1.2, 0.5, 0.0);

            var immediate = new Sim();
            immediate.Align();
            immediate.Step(velocity, 10);
            immediate.ObserveTruth();
            immediate.Outage(velocity, 3.0);
            immediate.Step(velocity, 1);

            // Captured just before the outage, delivered after tracking is back
            var latent = new Sim();
            latent.Align();
            latent.Step(velocity, 10);
            var captured = latent.Field;
            double captureTime = latent.Time;
            latent.Outage(velocity, 3.0);
            latent.Step(velocity, 1);
            latent.Observe(captured, captureTime);

            Assert.AreEqual(0.0, latent.PositionError, Tolerance);
            Assert.AreEqual(immediate.CorrectionGain(), latent.CorrectionGain(), 1e-9);
        }

        [Test]
        public void TrackingOutage_RelocalizationJumpIsNotIntegrated()
        {
            var sim = new Sim();
            sim.Align();
            sim.Step(Translation3d.Zero, 10);
            var before = sim.Estimator.EstimatedPose.Translation;

            // Tracking comes back with the VIO frame shifted further than the robot can drive
            sim.Outage(Translation3d.Zero, 0.2);
            sim.VioOffset += new Translation3d(2.0, -1.0, 0.0);
            sim.Step(Translation3d.Zero, 1);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(
                0.0,
                sim.Estimator.EstimatedPose.Translation.GetDistance(before),
                Tolerance
            );

            // Later motion is integrated from the new baseline, and tags re-anchor it
            var velocity = new Translation3d(1.0, 0.0, 0.0);
            sim.Step(velocity, 12);
            Assert.AreEqual(0.1, (sim.Estimator.EstimatedPose.Translation - before).X, Tolerance);

            sim.ObserveTruth();
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void TrackingOutage_RealignNeedsPresetQuality()
        {
            var sim = new Sim();
            sim.Align();
            sim.Outage(Translation3d.Zero, 0.1);
            sim.VioOffset += new Translation3d(0.0, 3.0, 0.0);
            sim.Step(Translation3d.Zero, 1);

            sim.Observe(
                sim.Field,
                sim.Time,
                tags: VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_TAGS
            );
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            sim.ObserveTruth();
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
        }

        [Test]
        public void TrackingOutage_RecenterDuringOutageRealigns()
        {
            var sim = new Sim();
            sim.Align();

            // The robot turns 90 deg while tracking is lost, then the VIO comes back recentered
            sim.Outage(Translation3d.Zero, 1.0);
            sim.Field = new Pose3d(
                sim.Field.Translation,
                sim.Field.Rotation.RotateBy(new Rotation3d(0, 0, Math.PI / 2))
            );
            sim.Recenter(0.7, new Translation3d(1.0, -0.5, 0.0));
            sim.Step(Translation3d.Zero, 1);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            sim.ObserveTruth();
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(
                0.0,
                AngleDiff(sim.Field.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void TrackingOutage_IgnoresAprilTagsUntilReacquired()
        {
            var sim = new Sim();
            sim.Align();
            var before = sim.Estimator.EstimatedPose;

            sim.Estimator.HandleTrackingLost();
            sim.Observe(
                new Pose3d(
                    sim.Field.Translation + new Translation3d(0.3, 0.0, 0.0),
                    sim.Field.Rotation
                ),
                sim.Time
            );

            Assert.AreEqual(before, sim.Estimator.EstimatedPose);
        }

        [Test]
        public void VioMotionSince_ReturnsPoseThenAndLatest()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(2.0, 1.0, 0.0);
            sim.Step(velocity, 20);
            var vioThen = sim.Vio;
            double then = sim.Time;
            sim.Step(velocity, 10);

            Assert.IsTrue(
                sim.Estimator.TryGetVioMotionSince(then + Dt / 2, out var atThen, out var latest)
            );
            Assert.AreEqual(0.0, atThen.Translation.GetDistance(vioThen.Translation), Tolerance);
            Assert.AreEqual(0.0, AngleDiff(vioThen.Rotation.Z, atThen.Rotation.Z), Tolerance);
            Assert.AreEqual(0.0, latest.Translation.GetDistance(sim.Vio.Translation), Tolerance);
        }

        [Test]
        public void VioMotionSince_ClampsToBuffer()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(1.0, 0.0, 0.0);
            sim.Step(velocity, 240);

            Assert.IsTrue(
                sim.Estimator.TryGetVioMotionSince(sim.Time - 10.0, out var oldest, out var latest)
            );
            Assert.AreEqual(
                VioAprilTagPoseEstimatorConstants.BUFFER_DURATION_SECONDS,
                latest.Translation.GetDistance(oldest.Translation),
                Dt
            );
        }

        [Test]
        public void VioMotionSince_UnavailableWithoutConsistentHistory()
        {
            var sim = new Sim();
            Assert.IsFalse(sim.Estimator.TryGetVioMotionSince(sim.Time, out _, out _));

            sim.Align();
            Assert.IsTrue(sim.Estimator.TryGetVioMotionSince(sim.Time, out _, out _));

            sim.Estimator.HandleTrackingLost();
            Assert.IsFalse(sim.Estimator.TryGetVioMotionSince(sim.Time, out _, out _));
            sim.Step(Translation3d.Zero, 1);

            sim.Estimator.HandleRecenter();
            Assert.IsFalse(sim.Estimator.TryGetVioMotionSince(sim.Time, out _, out _));
        }

        [Test]
        public void NonFiniteVioObservation_IsIgnored()
        {
            var sim = new Sim();
            sim.Align();
            var velocity = new Translation3d(1.0, 0.0, 0.0);
            sim.Step(velocity, 10);

            sim.Estimator.AddVioObservation(
                new Pose3d(double.NaN, 0.0, 0.0, Rotation3d.Zero),
                sim.Time + Dt / 2
            );
            sim.Estimator.AddVioObservation(
                new Pose3d(
                    sim.Vio.Translation,
                    new Rotation3d(new Quaternion(double.NaN, 0, 0, 0))
                ),
                sim.Time + Dt / 2
            );
            sim.Estimator.AddVioObservation(sim.Vio, double.PositiveInfinity);

            sim.Step(velocity, 10);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void NonFiniteAprilTagObservation_IsIgnored()
        {
            var sim = new Sim();
            sim.Estimator.AddVioObservation(sim.Vio, sim.Time);
            sim.Step(Translation3d.Zero, 5);

            var nanPosition = new Translation3d(double.NaN, 1.0, 1.0);
            sim.Estimator.AddAprilTagObservation(
                nanPosition,
                sim.Field.Rotation,
                sim.Time,
                TagStdDevs,
                GoodTags,
                GoodInlierRatio
            );
            Assert.IsFalse(sim.Estimator.HasInitialAlignment);

            sim.Align();
            var before = sim.Estimator.EstimatedPose;

            var badRotation = new Rotation3d(new Quaternion(double.NaN, 0, 0, 1));
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                badRotation,
                sim.Time,
                TagStdDevs,
                GoodTags,
                GoodInlierRatio
            );
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                sim.Field.Rotation,
                sim.Time,
                StdDevs(double.PositiveInfinity),
                GoodTags,
                GoodInlierRatio
            );
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                sim.Field.Rotation,
                sim.Time,
                StdDevs(0.0),
                GoodTags,
                GoodInlierRatio
            );
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                sim.Field.Rotation,
                double.NaN,
                TagStdDevs,
                GoodTags,
                GoodInlierRatio
            );
            sim.Estimator.AddAprilTagObservation(
                sim.Field.Translation,
                sim.Field.Rotation,
                sim.Time,
                TagStdDevs,
                GoodTags,
                double.NaN
            );
            sim.Estimator.AddAprilTagObservation(
                nanPosition,
                sim.Field.Rotation,
                sim.Time,
                TagStdDevs,
                GoodTags,
                GoodInlierRatio
            );

            var after = sim.Estimator.EstimatedPose;
            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(before, after);
        }

        [Test]
        public void ResetPosition_RestartsAtPoseInPhase1()
        {
            var sim = new Sim();
            sim.Align();
            sim.Step(new Translation3d(1.0, 1.0, 0.0), 30);

            // As after setPose: VIO now reports field coordinates directly
            sim.VioYaw = 0.0;
            sim.VioOffset = Translation3d.Zero;
            sim.Estimator.ResetPosition(sim.Vio, sim.Time);

            Assert.IsFalse(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
            Assert.AreEqual(
                0.0,
                AngleDiff(sim.Field.Rotation.Z, sim.Estimator.EstimatedPose.Rotation.Z),
                Tolerance
            );

            sim.Step(new Translation3d(-0.5, 2.0, 0.0), 30);
            Assert.AreEqual(0.0, sim.PositionError, Tolerance);
        }

        [Test]
        public void ResetPosition_IgnoresNonFinitePose()
        {
            var sim = new Sim();
            sim.Align();
            var before = sim.Estimator.EstimatedPose;

            sim.Estimator.ResetPosition(
                new Pose3d(0.0, double.NaN, 0.0, Rotation3d.Zero),
                sim.Time
            );

            Assert.IsTrue(sim.Estimator.HasInitialAlignment);
            Assert.AreEqual(before, sim.Estimator.EstimatedPose);
        }
    }
}
