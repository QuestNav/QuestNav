using System;
using Google.Protobuf;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Publisher for protobuf messages over NetworkTables. Handles serialization of protobuf messages to byte arrays.
    /// </summary>
    /// <typeparam name="T">The protobuf message type</typeparam>
    public class ProtobufPublisher<T>
        where T : IMessage<T>
    {
        /// <summary>
        /// The underlying raw publisher for byte array data
        /// </summary>
        private readonly RawPublisher rawPublisher;

        /// <summary>
        /// Creates a new protobuf publisher wrapping the given raw publisher
        /// </summary>
        /// <param name="rawPublisher">The raw publisher to wrap</param>
        internal ProtobufPublisher(RawPublisher rawPublisher)
        {
            this.rawPublisher = rawPublisher;
        }

        /// <summary>
        /// Publishes a protobuf message by serializing it to bytes and sending over NetworkTables
        /// </summary>
        /// <param name="message">The protobuf message to publish</param>
        /// <returns>True if the message was successfully published, false otherwise</returns>
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
