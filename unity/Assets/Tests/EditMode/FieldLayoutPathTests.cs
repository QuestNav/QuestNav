using System.IO;
using NUnit.Framework;
using QuestNav.Utils;

namespace QuestNav.Tests
{
    public class FieldLayoutPathTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "questnav-layouts-test");
        }

        [TestCase("2026-rebuilt-welded.json")]
        [TestCase("team_1234.practice.json")]
        [TestCase("a.json")]
        public void AcceptsPlainJsonNames(string name)
        {
            Assert.IsTrue(FileManager.TryResolveFieldLayoutPath(directory, name, out string path));
            Assert.AreEqual(Path.Combine(Path.GetFullPath(directory), name), path);
        }

        [Test]
        public void AcceptsTrailingSeparatorOnDirectory()
        {
            Assert.IsTrue(
                FileManager.TryResolveFieldLayoutPath(
                    directory + Path.DirectorySeparatorChar,
                    "field.json",
                    out string path
                )
            );
            Assert.AreEqual(Path.Combine(Path.GetFullPath(directory), "field.json"), path);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("../config.db")]
        [TestCase("../../config.json")]
        [TestCase("..json")]
        [TestCase("a..b.json")]
        [TestCase("sub/field.json")]
        [TestCase("sub\\field.json")]
        [TestCase("/etc/field.json")]
        [TestCase("C:\\field.json")]
        [TestCase(".hidden.json")]
        [TestCase(".json")]
        [TestCase("field")]
        [TestCase("field.txt")]
        [TestCase("field.JSON")]
        [TestCase("field.json.bak")]
        [TestCase("field .json")]
        [TestCase("%2e%2e%2fconfig.json")]
        public void RejectsUnsafeNames(string name)
        {
            Assert.IsFalse(FileManager.TryResolveFieldLayoutPath(directory, name, out string path));
            Assert.IsNull(path);
        }

        [Test]
        public void EnforcesStemLengthCap()
        {
            string longest = new string('a', 64) + ".json";
            string tooLong = new string('a', 65) + ".json";

            Assert.IsTrue(FileManager.TryResolveFieldLayoutPath(directory, longest, out _));
            Assert.IsFalse(FileManager.TryResolveFieldLayoutPath(directory, tooLong, out _));
        }
    }
}
