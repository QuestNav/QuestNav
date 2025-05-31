using System;
using Google.Protobuf;

namespace QuestNav.Native.NTCore
{
    public class ProtobufPublisher<T>
        where T : IMessage<T>
    {
        private readonly RawPublisher rawPublisher;

        internal ProtobufPublisher(RawPublisher rawPublisher)
        {
            this.rawPublisher = rawPublisher;
        }

        public bool Set(T message)
        {
            if (message == null)
            {
                return false;
            }

            try
            {
                byte[] data = message.ToByteArray();
                return rawPublisher.Set(data);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
