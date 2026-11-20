using System.IO;
using System.Linq;
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
    }
}
