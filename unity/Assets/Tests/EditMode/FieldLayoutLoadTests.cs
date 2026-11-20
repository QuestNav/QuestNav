using System.IO;
using NUnit.Framework;
using QuestNav.Core;
using QuestNav.QuestNav.AprilTag;

namespace QuestNav.Tests
{
    public class FieldLayoutLoadTests
    {
        private static readonly string[] BundledLayouts = QuestNavConstants
            .AprilTag
            .BUNDLED_FIELD_LAYOUTS;

        [TestCaseSource(nameof(BundledLayouts))]
        public void BundledLayoutLoadsWithFieldSize(string fileName)
        {
            // EditMode tests run with the project folder as the working directory
            var path = Path.Combine(
                "Assets",
                "StreamingAssets",
                "apriltag",
                "fieldlayouts",
                fileName
            );
            var layout = new AprilTagFieldLayout();

            Assert.IsTrue(layout.TryLoadFrom(path));
            Assert.IsNotNull(layout.Field);
            Assert.Greater(layout.Field.Length, 15.0);
            Assert.Greater(layout.Field.Width, 7.0);
            Assert.Greater(layout.Field.Length, layout.Field.Width);
        }
    }
}
