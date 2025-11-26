using QuestNav.WebServer;

namespace QuestNav.Core
{
    /// <summary>
    /// Provides access to settings for video streaming
    /// </summary>
    public class PassthroughOptions : IPassthroughOptions
    {
        public bool Enable => WebServerConstants.enablePassThrough;
        public int MaxFrameRate => WebServerConstants.maxVideoFrameRate;
    }
}