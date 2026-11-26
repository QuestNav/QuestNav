using NUnit.Framework;
using QuestNav.Commands.Commands;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.Tests
{
    public class PoseResetBoundsTests
    {
        private static readonly Field2d Field2026 = new Field2d(16.541, 8.069);

        [TestCase(0.0, 0.0)]
        [TestCase(16.5, 8.0)]
        [TestCase(-0.2, 8.2)]
        public void PoseInsideFieldIsNotFlagged(double x, double y)
        {
            Assert.IsFalse(PoseResetCommand.IsOutsideField(x, y, Field2026));
        }

        [TestCase(-1.0, 4.0)]
        [TestCase(17.5, 4.0)]
        [TestCase(8.0, -1.0)]
        [TestCase(8.0, 9.0)]
        public void PoseOutsideFieldIsFlagged(double x, double y)
        {
            Assert.IsTrue(PoseResetCommand.IsOutsideField(x, y, Field2026));
        }

        [Test]
        public void UnknownFieldSizeSkipsBoundsCheck()
        {
            Assert.IsFalse(PoseResetCommand.IsOutsideField(100, 100, null));
            Assert.IsFalse(PoseResetCommand.IsOutsideField(100, 100, new Field2d(0, 0)));
            Assert.IsFalse(PoseResetCommand.IsOutsideField(100, 100, new Field2d(16.541, -1)));
        }
    }
}
