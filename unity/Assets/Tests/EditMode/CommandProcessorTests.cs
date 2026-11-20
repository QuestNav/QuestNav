using NUnit.Framework;
using QuestNav.Commands;

namespace QuestNav.Tests
{
    public class CommandProcessorTests
    {
        [Test]
        public void AgeIsComputedFromNanosecondTimestamps()
        {
            Assert.AreEqual(500, CommandProcessor.GetAgeMs(5_000_000_000, 4_500_000_000));
        }

        [Test]
        public void AgeTruncatesPartialMilliseconds()
        {
            Assert.AreEqual(0, CommandProcessor.GetAgeMs(1_999_999, 1_000_000));
            Assert.AreEqual(1, CommandProcessor.GetAgeMs(2_000_000, 1_000_000));
        }
    }
}
