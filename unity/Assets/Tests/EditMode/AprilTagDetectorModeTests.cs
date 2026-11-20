using NUnit.Framework;
using QuestNav.Core;
using QuestNav.QuestNav.Estimation;
using static QuestNav.Config.Config;

namespace QuestNav.Tests
{
    public class AprilTagDetectorModeTests
    {
        private static AprilTagDetectorMode WithMinimumTags(int minimumTags)
        {
            return new AprilTagDetectorMode(
                AprilTagDetectorMode.DetectionMode.TRADITIONAL,
                1280,
                960,
                30,
                new int[0],
                4.0,
                minimumTags
            );
        }

        // A stored value of 1 from an older build is read as the two-tag floor
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void MinimumTags_BelowFloorIsRaised(int stored)
        {
            Assert.AreEqual(
                VioAprilTagPoseEstimatorConstants.MIN_TAGS_FOR_POSITION_UPDATE,
                WithMinimumTags(stored).MinimumNumberOfTags
            );
        }

        [Test]
        public void MinimumTags_SupportedValuesAreKept()
        {
            foreach (int n in QuestNavConstants.AprilTag.MINIMUM_TAGS_OPTIONS)
                Assert.AreEqual(n, WithMinimumTags(n).MinimumNumberOfTags);
        }

        [Test]
        public void MinimumTags_OptionsStartAtFloor()
        {
            CollectionAssert.DoesNotContain(QuestNavConstants.AprilTag.MINIMUM_TAGS_OPTIONS, 1);
            Assert.AreEqual(
                VioAprilTagPoseEstimatorConstants.MIN_TAGS_FOR_POSITION_UPDATE,
                QuestNavConstants.AprilTag.MINIMUM_TAGS_OPTIONS[0]
            );
        }
    }
}
