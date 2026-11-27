using NUnit.Framework;
using QuestNav.Commands;
using QuestNav.Core;
using QuestNav.Native.NTCore;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Estimation;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using UnityEngine;

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

        [Test]
        public void PoseResetFromTheFutureIsExecuted()
        {
            // NT measures the clock offset once per connection, so drift over a long
            // connection puts commands well into the future; the reset must still apply
            var root = new GameObject("root").transform;
            var camera = new GameObject("camera").transform;
            camera.SetParent(root, false);
            try
            {
                var config = new FakeConfigManager();
                var futureProcessor = new CommandProcessor(
                    connection,
                    new VioAprilTagPoseEstimator(),
                    camera,
                    root,
                    null,
                    config
                );
                config.RaiseAllowedPoseResetTimeoutMs(TtlMs);
                var target = new Pose3d(4.0, 2.0, 0.0, new Rotation3d(0, 0, 0.5));
                var command = PoseReset(7, -5000);
                command.Value.PoseResetPayload = new ProtobufQuestNavPoseResetPayload
                {
                    TargetPose = target.ToProtobuf(),
                };
                connection.CommandRequests = new[] { command };

                futureProcessor.ProcessCommands();

                Assert.IsEmpty(connection.ErrorResponses);
                CollectionAssert.AreEqual(new[] { 7u }, connection.SuccessResponses);
                var cameraPose = Conversions.UnityToFrc3d(camera.position, camera.rotation);
                Assert.AreEqual(0.0, cameraPose.Translation.GetDistance(target.Translation), 1e-5);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void PoseResetWithinClockSkewIsExecuted()
        {
            connection.CommandRequests = new[]
            {
                PoseReset(7, -QuestNavConstants.Network.MAX_COMMAND_CLOCK_SKEW_MS),
            };

            processor.ProcessCommands();

            Assert.AreEqual(1, connection.ErrorResponses.Count);
            Assert.AreEqual(
                "Failed to get valid pose data (invalid)",
                connection.ErrorResponses[0].Message
            );
        }

        [Test]
        public void UnknownCommandTypeGetsErrorResponse()
        {
            // A command type from a newer robot library that this app's protos don't define
            connection.CommandRequests = new[] { Command((QuestNavCommandType)42, 9) };

            processor.ProcessCommands();

            Assert.AreEqual(1, connection.ErrorResponses.Count);
            Assert.AreEqual(9u, connection.ErrorResponses[0].CommandId);
            StringAssert.StartsWith(
                "Unknown command type 42",
                connection.ErrorResponses[0].Message
            );
            Assert.IsEmpty(connection.SuccessResponses);
        }

        [Test]
        public void UnspecifiedCommandTypeGetsErrorResponse()
        {
            connection.CommandRequests = new[]
            {
                Command(QuestNavCommandType.CommandTypeUnspecified, 11),
            };

            processor.ProcessCommands();

            Assert.AreEqual(1, connection.ErrorResponses.Count);
            Assert.AreEqual(11u, connection.ErrorResponses[0].CommandId);
            Assert.AreEqual("Command type not set", connection.ErrorResponses[0].Message);
        }

        [Test]
        public void UnknownCommandDoesNotSupersedePoseReset()
        {
            connection.CommandRequests = new[]
            {
                PoseReset(7, TtlMs),
                Command((QuestNavCommandType)42, 8),
            };

            processor.ProcessCommands();

            // The pose reset reaches the age check rather than being skipped as superseded
            Assert.AreEqual(2, connection.ErrorResponses.Count);
            Assert.AreEqual(7u, connection.ErrorResponses[0].CommandId);
            StringAssert.StartsWith(
                "Pose Reset Command too old",
                connection.ErrorResponses[0].Message
            );
            Assert.AreEqual(8u, connection.ErrorResponses[1].CommandId);
        }

        private static TimestampedValue<ProtobufQuestNavCommand> Command(
            QuestNavCommandType type,
            uint commandId
        ) =>
            new TimestampedValue<ProtobufQuestNavCommand>(
                new ProtobufQuestNavCommand { Type = type, CommandId = commandId },
                0,
                NowNs
            );

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
