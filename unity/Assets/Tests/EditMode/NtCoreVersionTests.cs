using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using QuestNav.Native.NTCore;

namespace QuestNav.Tests
{
    public class NtCoreVersionTests
    {
        private const string TagKey = "allwpilib-tag:";

        [Test]
        public void TagMatchesBundledLibraries()
        {
            // EditMode tests run with the project folder as the working directory
            var tagLine = File.ReadAllLines(Path.Combine("Assets", "Plugins", "ntcore", "VERSION"))
                .Single(line => line.StartsWith(TagKey));

            Assert.AreEqual(NtCoreNatives.ALLWPILIB_TAG, tagLine.Substring(TagKey.Length).Trim());
        }

        // The tag above only means something if the binaries are the ones VERSION lists
        [TestCase("android-sha256:", "Android/arm64-v8a", 4)]
        [TestCase("windows-sha256:", "Windows/x64", 4)]
        public void BundledLibrariesMatchVersionFile(string section, string folder, int count)
        {
            var pluginDir = Path.Combine("Assets", "Plugins", "ntcore");
            var hashes = File.ReadAllLines(Path.Combine(pluginDir, "VERSION"))
                .SkipWhile(line => line.Trim() != section)
                .Skip(1)
                .TakeWhile(line => line.StartsWith("  "))
                .Select(line => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                .ToList();
            Assert.AreEqual(count, hashes.Count, $"{section} lists the wrong number of files");

            using var sha256 = SHA256.Create();
            foreach (var entry in hashes)
            {
                var file = Path.Combine(pluginDir, folder, entry[1]);
                var actual = BitConverter
                    .ToString(sha256.ComputeHash(File.ReadAllBytes(file)))
                    .Replace("-", "")
                    .ToLowerInvariant();
                Assert.AreEqual(entry[0], actual, $"{file} is not the build VERSION describes");
            }
        }

        [TestCase(0x0400u, "4.0")]
        [TestCase(0x0401u, "4.1")]
        public void FormatsProtocolVersion(uint version, string expected)
        {
            Assert.AreEqual(expected, new NtConnectionInfo("", 0, version).ProtocolVersionString);
        }
    }
}
