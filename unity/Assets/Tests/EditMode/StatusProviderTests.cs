using NUnit.Framework;
using QuestNav.WebServer;

namespace QuestNav.Tests
{
    public class StatusProviderTests
    {
        private static object StatusField(StatusProvider provider, string name)
        {
            var status = provider.GetStatus();
            var property = status.GetType().GetProperty(name);
            Assert.IsNotNull(property, $"/api/status has no {name}");
            return property.GetValue(status);
        }

        [Test]
        public void ReportsRunningStreamFramerateAndQuality()
        {
            var provider = new StatusProvider();

            provider.UpdateStreamStatus(15, 40);
            Assert.AreEqual(15, StatusField(provider, "passthroughStreamFramerate"));
            Assert.AreEqual(40, StatusField(provider, "passthroughStreamQuality"));

            provider.UpdateStreamStatus(null, null);
            Assert.IsNull(StatusField(provider, "passthroughStreamFramerate"));
            Assert.IsNull(StatusField(provider, "passthroughStreamQuality"));
        }
    }
}
