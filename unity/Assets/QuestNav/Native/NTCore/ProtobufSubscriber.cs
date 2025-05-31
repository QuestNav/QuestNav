using System;
using Google.Protobuf;

namespace QuestNav.Native.NTCore
{
    public class ProtobufSubscriber<T>
        where T : IMessage<T>, new()
    {
        private readonly RawSubscriber rawSubscriber;
        private readonly MessageParser<T> parser;

        internal ProtobufSubscriber(RawSubscriber rawSubscriber)
        {
            this.rawSubscriber = rawSubscriber;
            this.parser = new MessageParser<T>(() => new T());
        }

        public T Get(T defaultValue)
        {
            if (defaultValue == null)
            {
                defaultValue = new T();
            }

            byte[] defaultBytes = defaultValue.ToByteArray();
            byte[] data = rawSubscriber.Get(defaultBytes);

            try
            {
                return parser.ParseFrom(data);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
