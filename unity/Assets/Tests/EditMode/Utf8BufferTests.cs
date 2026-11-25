using System;
using System.Text;
using NUnit.Framework;
using QuestNav.Native.NTCore;

namespace QuestNav.Tests
{
    public class Utf8BufferTests
    {
        private static void AssertRoundTrip(string value, string expected)
        {
            Span<byte> scratch = stackalloc byte[Utf8Buffer.StackSize];
            using var utf8 = new Utf8Buffer(value, scratch);

            Assert.AreEqual(Encoding.UTF8.GetByteCount(expected), utf8.Length);
            Assert.AreEqual(utf8.Length + 1, utf8.Bytes.Length);
            Assert.AreEqual(0, utf8.Bytes[utf8.Length], "missing NUL terminator");
            Assert.AreEqual(expected, Encoding.UTF8.GetString(utf8.Bytes.Slice(0, utf8.Length)));
        }

        [Test]
        public void Ascii() => AssertRoundTrip("/QuestNav/frameData", "/QuestNav/frameData");

        [Test]
        public void Multibyte() => AssertRoundTrip("héllo 世界 🚀", "héllo 世界 🚀");

        [Test]
        public void Empty() => AssertRoundTrip("", "");

        [Test]
        public void NullEncodesAsEmpty() => AssertRoundTrip(null, "");

        [Test]
        public void LongerThanScratchUsesPooledBuffer()
        {
            string value = new string('é', Utf8Buffer.StackSize);
            AssertRoundTrip(value, value);
        }

        [Test]
        public void ExactlyFillsScratch()
        {
            // StackSize - 1 bytes plus the terminator fills the scratch span exactly
            string value = new string('a', Utf8Buffer.StackSize - 1);
            AssertRoundTrip(value, value);
        }
    }
}
