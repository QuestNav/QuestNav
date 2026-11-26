using System;
using Google.Protobuf;
using NUnit.Framework;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.Tests
{
    public class FrameDataGoldenTests
    {
        /// <summary>
        /// Same bytes as questnav-lib/src/test/resources/gg/questnav/questnav/protos/wpilib/frame_data_golden.bin,
        /// which the robot library's FrameDataProtoTest decodes. Change both together.
        /// </summary>
        private const string GoldenHex =
            "0892211177BE9F1A2FDD5E401A450A1B09000000000000F83F1100000000000002C019000000000000C03F"
            + "12260A2409000000000000E03F11000000000000E03F19000000000000E03F21000000000000E03F"
            + "200128CB89EC8FF723";

        [Test]
        public void FrameDataEncodesToGoldenBytes()
        {
            var frameData = new ProtobufQuestNavFrameData
            {
                FrameCount = 4242,
                Timestamp = 123.456,
                ServerTimestamp = 1_234_567_890_123,
                Pose3D = new Pose3d(
                    new Translation3d(1.5, -2.25, 0.125),
                    new Rotation3d(new Quaternion(0.5, 0.5, 0.5, 0.5))
                ).ToProtobuf(),
                IsTracking = true,
            };

            Assert.AreEqual(
                GoldenHex,
                BitConverter.ToString(frameData.ToByteArray()).Replace("-", "")
            );
        }
    }
}
