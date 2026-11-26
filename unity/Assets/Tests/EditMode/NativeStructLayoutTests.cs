using System.Runtime.InteropServices;
using NUnit.Framework;
using QuestNav.Native.AprilTag;
using QuestNav.Native.NTCore;
using QuestNav.QuestNav.Native.PoseLib;

namespace QuestNav.Tests
{
    /// <summary>
    /// Pins the P/Invoke structs to the 64-bit layouts in ntcore_c.h (v2027.0.0-alpha-8)
    /// </summary>
    public class NativeStructLayoutTests
    {
        [Test]
        public void ConnectionInfoMatchesNative()
        {
            Assert.AreEqual(56, Marshal.SizeOf<NativeConnectionInfo>());
            Assert.AreEqual(16, (int)Marshal.OffsetOf<NativeConnectionInfo>("remoteIp"));
            Assert.AreEqual(32, (int)Marshal.OffsetOf<NativeConnectionInfo>("remotePort"));
            Assert.AreEqual(40, (int)Marshal.OffsetOf<NativeConnectionInfo>("lastUpdate"));
            Assert.AreEqual(48, (int)Marshal.OffsetOf<NativeConnectionInfo>("protocolVersion"));
        }

        [Test]
        public void PubSubOptionsMatchesNative()
        {
            Assert.AreEqual(56, Marshal.SizeOf<NativePubSubOptions>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativePubSubOptions>("periodic"));
            Assert.AreEqual(16, (int)Marshal.OffsetOf<NativePubSubOptions>("excludePublisher"));
            Assert.AreEqual(20, (int)Marshal.OffsetOf<NativePubSubOptions>("sendAll"));
            Assert.AreEqual(52, (int)Marshal.OffsetOf<NativePubSubOptions>("disableSignal"));
            // ntcore ignores trailing fields that structSize does not cover
            Assert.AreEqual(56u, PubSubOptions.AllDefault.ToNative().structSize);
        }

        [Test]
        public void TimeSyncEventDataMatchesNative()
        {
            Assert.AreEqual(24, Marshal.SizeOf<NativeTimeSyncEventData>());
            Assert.AreEqual(16, (int)Marshal.OffsetOf<NativeTimeSyncEventData>("valid"));
        }

        [Test]
        public void WpiStringMatchesNative()
        {
            Assert.AreEqual(16, Marshal.SizeOf<WpiString>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<WpiString>("len"));
        }

        // Arrays of values are walked by this size (RawSubscriber)
        [Test]
        public void NetworkTableValueMatchesNative()
        {
            Assert.AreEqual(40, Marshal.SizeOf<NativeNetworkTableValue>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativeNetworkTableValue>("lastChange"));
            Assert.AreEqual(16, (int)Marshal.OffsetOf<NativeNetworkTableValue>("serverTime"));
            Assert.AreEqual(24, (int)Marshal.OffsetOf<NativeNetworkTableValue>("data"));
        }

        // Arrays of events are walked by this size (PolledLogger), and each union member must fit
        [Test]
        public void NtEventMatchesNative()
        {
            Assert.AreEqual(72, Marshal.SizeOf<NativeNtEvent>());
            Assert.AreEqual(4, (int)Marshal.OffsetOf<NativeNtEvent>("flags"));
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativeNtEvent>("data"));
            Assert.AreEqual(64, Marshal.SizeOf<NativeNtEvent.NtEventUnion>());

            Assert.AreEqual(64, Marshal.SizeOf<NativeTopicInfo>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativeTopicInfo>("name"));
            Assert.AreEqual(24, (int)Marshal.OffsetOf<NativeTopicInfo>("type"));
            Assert.AreEqual(32, (int)Marshal.OffsetOf<NativeTopicInfo>("typeStr"));
            Assert.AreEqual(48, (int)Marshal.OffsetOf<NativeTopicInfo>("properties"));

            Assert.AreEqual(48, Marshal.SizeOf<NativeValueEventData>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativeValueEventData>("value"));

            Assert.AreEqual(48, Marshal.SizeOf<NativeNtLogMessage>());
            Assert.AreEqual(8, (int)Marshal.OffsetOf<NativeNtLogMessage>("filename"));
            Assert.AreEqual(24, (int)Marshal.OffsetOf<NativeNtLogMessage>("line"));
            Assert.AreEqual(32, (int)Marshal.OffsetOf<NativeNtLogMessage>("message"));
        }

        // The AprilTag and PoseLib structs are read through pointers, so these check the
        // in-memory layout (bool is 1 byte, as in C) against the bundled arm64 libraries
        [Test]
        public unsafe void AprilTagDetectorMatchesNative()
        {
            var detector = default(AprilTagDetectorNative);
            byte* start = (byte*)&detector;
            Assert.AreEqual(0x38, (byte*)&detector.tp - start);
            Assert.AreEqual(0x50, (byte*)&detector.tag_families - start);
            Assert.AreEqual(0x58, (byte*)&detector.wp - start);
        }

        [Test]
        public unsafe void AprilTagFamilyMatchesNative()
        {
            var family = default(AprilTagFamilyNative);
            Assert.AreEqual(0x40, (byte*)&family.impl - (byte*)&family);
        }

        [Test]
        public unsafe void AprilTagDetectionMatchesNative()
        {
            var detection = default(AprilTagDetectionNative);
            byte* start = (byte*)&detection;
            Assert.AreEqual(0x18, (byte*)&detection.H - start);
            Assert.AreEqual(0x20, (byte*)&detection.center - start);
            Assert.AreEqual(0x30, (byte*)&detection.corner0 - start);
        }

        [Test]
        public unsafe void ZArrayMatchesNative()
        {
            var array = default(ZArrayNative);
            byte* start = (byte*)&array;
            Assert.AreEqual(0x08, (byte*)&array.size - start);
            Assert.AreEqual(0x10, (byte*)&array.data - start);
        }

        [Test]
        public unsafe void PoseLibCameraPoseMatchesNative()
        {
            Assert.AreEqual(56, sizeof(PoseLibNatives.PoseLibCameraPoseNative));
        }
    }
}
