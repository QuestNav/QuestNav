using System.Runtime.InteropServices;
using NUnit.Framework;
using QuestNav.Native.NTCore;

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
    }
}
