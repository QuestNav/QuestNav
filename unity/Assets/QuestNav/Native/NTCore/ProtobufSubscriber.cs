using System;
using Google.Protobuf;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Subscriber for protobuf messages over NetworkTables. Handles deserialization of byte arrays to protobuf messages.
    /// </summary>
    /// <typeparam name="T">The protobuf message type</typeparam>
    public class ProtobufSubscriber<T>
        where T : IMessage<T>, new()
    {
        /// <summary>
        /// The underlying raw subscriber for byte array data
        /// </summary>
        private readonly RawSubscriber rawSubscriber;

        /// <summary>
        /// Parser for deserializing byte arrays to protobuf messages
        /// </summary>
        private readonly MessageParser<T> parser;

        /// <summary>
        /// Creates a new protobuf subscriber wrapping the given raw subscriber
        /// </summary>
        /// <param name="rawSubscriber">The raw subscriber to wrap</param>
        internal ProtobufSubscriber(RawSubscriber rawSubscriber)
        {
            this.rawSubscriber = rawSubscriber;
            this.parser = new MessageParser<T>(() => new T());
        }

        /// <summary>
        /// Gets the latest protobuf message from NetworkTables, or returns the default value if none available
        /// </summary>
        /// <param name="defaultValue">The default value to return if no message is available</param>
        /// <returns>The latest protobuf message or the default value</returns>
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
