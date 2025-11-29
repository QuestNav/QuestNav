namespace QuestNav.Native.NTCore
{
    public class StringEntry
    {
        private readonly StringPublisher publisher;
        private readonly StringSubscriber subscriber;

        internal StringEntry(uint handle)
        {
            publisher = new StringPublisher(handle);
            subscriber = new StringSubscriber(handle);
        }

        public string Get(string defaultValue)
        {
            return subscriber.Get(defaultValue);
        }

        public void Set(string value)
        {
            publisher.Set(value);
        }
    }
}
