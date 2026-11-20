using System;
using System.Collections.Generic;
using QuestNav.Native.NTCore;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.Tests
{
    /// <summary>
    /// INetworkTableConnection with a settable NT clock that hands out queued commands and records
    /// command responses
    /// </summary>
    public class FakeNetworkTableConnection : INetworkTableConnection
    {
        public long NtNow { get; set; }

        public TimestampedValue<ProtobufQuestNavCommand>[] CommandRequests { get; set; } =
            Array.Empty<TimestampedValue<ProtobufQuestNavCommand>>();

        public List<uint> SuccessResponses { get; } = new List<uint>();

        public List<(uint CommandId, string Message)> ErrorResponses { get; } =
            new List<(uint CommandId, string Message)>();

        public event Action OnConnect
        {
            add { }
            remove { }
        }

        public event Action OnDisconnect
        {
            add { }
            remove { }
        }

        public event Action<string> OnIpAddressChanged
        {
            add { }
            remove { }
        }

        public bool IsConnected => true;

        public bool IsReadyToConnect => true;

        public string RobotIpAddress => "127.0.0.1";

        public string IpAddress => "127.0.0.1";

        public bool TryGetServerTimeOffset(out long offsetNs)
        {
            offsetNs = 0;
            return true;
        }

        public TimestampedValue<ProtobufQuestNavCommand>[] GetCommandRequests() => CommandRequests;

        public void SendCommandSuccessResponse(uint commandId) => SuccessResponses.Add(commandId);

        public void SendCommandErrorResponse(uint commandId, string errorMessage) =>
            ErrorResponses.Add((commandId, errorMessage));

        public void PublishFrameData(
            int frameCount,
            double timeStamp,
            long serverTimestamp,
            Pose3d pose,
            bool isTracking,
            AlignmentState alignmentState,
            uint resetEpoch
        ) => throw new NotSupportedException();

        public void PublishDeviceData(int trackingLostCounter, int batteryPercent) =>
            throw new NotSupportedException();

        public INtCameraSource CreateCameraSource(string name) => throw new NotSupportedException();

        public void Periodic() => throw new NotSupportedException();

        public void Initialize() => throw new NotSupportedException();
    }
}
