using NUnit.Framework;
using QuestNav.Commands;
using QuestNav.Native.NTCore;
using QuestNav.Protos.Generated;

namespace QuestNav.Tests
{
    public class CommandProcessorTests
    {
        private const long NowNs = 100_000_000_000;
        private const int TtlMs = 500;

        private FakeNetworkTableConnection connection;
        private CommandProcessor processor;

        [SetUp]
        public void SetUp()
        {
            connection = new FakeNetworkTableConnection { NtNow = NowNs };
            var config = new FakeConfigManager();
            // Transforms and the estimator are only touched for a valid pose payload
            processor = new CommandProcessor(connection, null, null, null, null, config);
            config.RaiseAllowedPoseResetTimeoutMs(TtlMs);
        }

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

        [TestCase(TtlMs)]
        [TestCase(TtlMs + 100)]
        public void StalePoseResetIsRejected(int ageMs)
        {
            connection.CommandRequests = new[] { PoseReset(7, ageMs) };

            processor.ProcessCommands();

            Assert.AreEqual(1, connection.ErrorResponses.Count);
            Assert.AreEqual(7u, connection.ErrorResponses[0].CommandId);
            StringAssert.StartsWith(
                $"Pose Reset Command too old. Age: {ageMs} ms",
                connection.ErrorResponses[0].Message
            );
        }

        [TestCase(0)]
        [TestCase(TtlMs - 1)]
        public void FreshPoseResetIsExecuted(int ageMs)
        {
            // Without a payload Execute rejects the pose, which shows it got past the age check
            connection.CommandRequests = new[] { PoseReset(7, ageMs) };

            processor.ProcessCommands();

            Assert.AreEqual(1, connection.ErrorResponses.Count);
            Assert.AreEqual(
                "Failed to get valid pose data (invalid)",
                connection.ErrorResponses[0].Message
            );
        }

        private static TimestampedValue<ProtobufQuestNavCommand> PoseReset(
            uint commandId,
            long ageMs
        ) =>
            new TimestampedValue<ProtobufQuestNavCommand>(
                new ProtobufQuestNavCommand
                {
                    Type = QuestNavCommandType.PoseReset,
                    CommandId = commandId,
                },
                0,
                NowNs - ageMs * 1_000_000
            );
    }
}
