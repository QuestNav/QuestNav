using System.Collections.Generic;
using NUnit.Framework;
using QuestNav.Native.PoseLib;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;

namespace QuestNav.Tests
{
    public class PoseLibSupportingTagsTests
    {
        private static readonly double[] Intrinsics = { 500.0, 500.0, 640.0, 480.0 };
        private static readonly Translation3d CameraPosition = new Translation3d(0.0, 0.0, 1.0);
        private const double TagHalfSize = 0.08;
        private const double BadCornerPx = 40.0;

        // Rotates FRC body coordinates into CV camera coordinates, as a PnP solver's camera frame
        private static readonly Rotation3d BodyToCv = new Rotation3d(
            new Quaternion(0.5, 0.5, -0.5, 0.5)
        );

        /// <summary>The world-to-camera pose PoseLib reports for a camera facing +X from CameraPosition.</summary>
        private static Pose3d WorldToCamera()
        {
            return new Pose3d(-CameraPosition.RotateBy(BodyToCv), BodyToCv);
        }

        private readonly List<int> ids = new List<int>();
        private readonly List<double> corners2d = new List<double>();
        private readonly List<double> corners3d = new List<double>();

        // NUnit reuses one fixture instance for every test, so start each with no tags
        [SetUp]
        public void ClearTags()
        {
            ids.Clear();
            corners2d.Clear();
            corners3d.Clear();
        }

        /// <summary>Adds a tag 3 m ahead, projected by hand (CV x = -Y, y = -Z, z = X).</summary>
        private void AddTag(int id, double centerY, params int[] badCorners)
        {
            double[,] offsets =
            {
                { -TagHalfSize, -TagHalfSize },
                { TagHalfSize, -TagHalfSize },
                { TagHalfSize, TagHalfSize },
                { -TagHalfSize, TagHalfSize },
            };
            ids.Add(id);
            for (int c = 0; c < 4; c++)
            {
                var corner = new Translation3d(
                    3.0,
                    centerY + offsets[c, 0],
                    CameraPosition.Z + offsets[c, 1]
                );
                var rel = corner - CameraPosition;
                double u = Intrinsics[2] + Intrinsics[0] * -rel.Y / rel.X;
                double v = Intrinsics[3] + Intrinsics[1] * -rel.Z / rel.X;
                if (System.Array.IndexOf(badCorners, c) >= 0)
                    u += BadCornerPx;

                corners2d.Add(u);
                corners2d.Add(v);
                corners3d.Add(corner.X);
                corners3d.Add(corner.Y);
                corners3d.Add(corner.Z);
            }
        }

        private int Count()
        {
            return PoseLibSolver.CountSupportingTags(
                ids,
                corners2d,
                corners3d,
                WorldToCamera(),
                Intrinsics,
                PoseLibSolver.MAX_REPROJECTION_ERROR_PX
            );
        }

        [Test]
        public void WorldToCamera_MatchesConversions()
        {
            var camera = Conversions.CvToFrc(WorldToCamera());
            Assert.AreEqual(0.0, camera.Translation.GetDistance(CameraPosition), 1e-9);
        }

        [Test]
        public void TwoFittingTags_CountTwo()
        {
            AddTag(1, 0.5);
            AddTag(2, -0.5);
            Assert.AreEqual(2, Count());
        }

        [Test]
        public void TagWithOneBadCorner_StillCounts()
        {
            AddTag(1, 0.5);
            AddTag(2, -0.5, 3);
            Assert.AreEqual(2, Count());
        }

        // 5 of 8 corners fit, which clears a 0.6 inlier ratio, but only one tag supports the solve
        [Test]
        public void TagWithThreeBadCorners_DoesNotCount()
        {
            AddTag(1, 0.5);
            AddTag(2, -0.5, 0, 1, 2);
            Assert.AreEqual(1, Count());
        }

        [Test]
        public void DuplicateId_CountsOnce()
        {
            AddTag(1, 0.5);
            AddTag(1, 0.5);
            Assert.AreEqual(1, Count());
        }
    }
}
