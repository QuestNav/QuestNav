using System.Linq;
using NUnit.Framework;
using QuestNav.Core;
using QuestNav.WebServer.Server;

namespace QuestNav.Tests
{
    public class RequestValidationTests
    {
        [TestCase(null, "10.12.34.5:5801")]
        [TestCase("http://10.12.34.5:5801", "10.12.34.5:5801")]
        [TestCase("HTTP://Quest.local:5801", "quest.local:5801")]
        [TestCase("http://quest", "quest:80")]
        [TestCase("http://[::1]:5801", "[::1]:5801")]
        public void AllowsMissingOrMatchingOrigin(string origin, string host)
        {
            Assert.IsTrue(RequestValidation.IsAllowedOrigin(origin, host));
        }

        [TestCase("http://evil.example", "10.12.34.5:5801")]
        [TestCase("http://10.12.34.5:5173", "10.12.34.5:5801")]
        [TestCase("https://10.12.34.5:5801", "10.12.34.5:5802")]
        [TestCase("null", "10.12.34.5:5801")]
        [TestCase("", "10.12.34.5:5801")]
        [TestCase("file:///tmp/page.html", "10.12.34.5:5801")]
        [TestCase("http://10.12.34.5:5801", null)]
        [TestCase("http://10.12.34.5:5801", "")]
        public void RejectsOtherOrigins(string origin, string host)
        {
            Assert.IsFalse(RequestValidation.IsAllowedOrigin(origin, host));
        }

        [TestCase("cross-site")]
        [TestCase(" Cross-Site ")]
        public void DetectsCrossSiteRequest(string secFetchSite)
        {
            Assert.IsTrue(RequestValidation.IsCrossSiteRequest(secFetchSite));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("same-origin")]
        [TestCase("same-site")]
        [TestCase("none")]
        public void AllowsOtherFetchSites(string secFetchSite)
        {
            Assert.IsFalse(RequestValidation.IsCrossSiteRequest(secFetchSite));
        }

        [TestCase("application/json")]
        [TestCase("application/json; charset=utf-8")]
        [TestCase(" APPLICATION/JSON ")]
        public void AcceptsJsonMediaType(string contentType)
        {
            Assert.IsTrue(RequestValidation.HasMediaType(contentType, "application/json"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("text/plain")]
        [TestCase("application/x-www-form-urlencoded")]
        [TestCase("multipart/form-data; boundary=x")]
        [TestCase("application/jsonp")]
        public void RejectsOtherMediaTypes(string contentType)
        {
            Assert.IsFalse(RequestValidation.HasMediaType(contentType, "application/json"));
        }

        [Test]
        public void AcceptsAnyListedMediaType()
        {
            Assert.IsTrue(
                RequestValidation.HasMediaType(
                    "application/vnd.sqlite3",
                    "application/octet-stream",
                    "application/vnd.sqlite3"
                )
            );
        }

        [Test]
        public void AcceptsValidIgnoredIds()
        {
            Assert.IsTrue(RequestValidation.TryValidateIgnoredIds(null, out _));
            Assert.IsTrue(RequestValidation.TryValidateIgnoredIds(new int[0], out _));
            Assert.IsTrue(
                RequestValidation.TryValidateIgnoredIds(
                    new[] { 0, QuestNavConstants.AprilTag.MAX_TAG36H11_ID },
                    out string error
                )
            );
            Assert.IsNull(error);

            int[] longest = Enumerable
                .Range(0, QuestNavConstants.AprilTag.MAX_IGNORED_IDS)
                .ToArray();
            Assert.IsTrue(RequestValidation.TryValidateIgnoredIds(longest, out _));
        }

        [Test]
        public void RejectsOutOfRangeIgnoredIds()
        {
            Assert.IsFalse(RequestValidation.TryValidateIgnoredIds(new[] { -1 }, out string error));
            Assert.IsNotNull(error);
            Assert.IsFalse(
                RequestValidation.TryValidateIgnoredIds(
                    new[] { QuestNavConstants.AprilTag.MAX_TAG36H11_ID + 1 },
                    out _
                )
            );
        }

        [Test]
        public void RejectsTooManyIgnoredIds()
        {
            int[] tooMany = Enumerable
                .Range(0, QuestNavConstants.AprilTag.MAX_IGNORED_IDS + 1)
                .ToArray();
            Assert.IsFalse(RequestValidation.TryValidateIgnoredIds(tooMany, out string error));
            Assert.IsNotNull(error);
        }
    }
}
