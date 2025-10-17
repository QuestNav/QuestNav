using System;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;
using QuestNav.Utils;

namespace QuestNav.Native.NTCore
{
    /// <summary>
    /// Represents a NetworkTables instance for communication with FRC robots.
    /// Provides methods for creating publishers, subscribers, and managing connections.
    /// </summary>
    public unsafe class NtInstance
    {
        /// <summary>
        /// The native handle for this NetworkTables instance
        /// </summary>
        private readonly uint handle;

        /// <summary>
        /// Creates a new NetworkTables instance with the specified name
        /// </summary>
        /// <param name="instanceName">The name for this NetworkTables instance</param>
        public NtInstance(string instanceName)
        {
            QueuedLogger.Log("Loading NTCore Natives");
            handle = NtCoreNatives.NT_GetDefaultInstance();

            byte[] nameUtf8 = Encoding.UTF8.GetBytes(instanceName);

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                NtCoreNatives.NT_StartClient4(handle, &str);
            }
        }

        /// <summary>
        /// Sets the team number for automatic FRC robot connection
        /// </summary>
        /// <param name="teamNumber">The FRC team number</param>
        /// <param name="port">The NetworkTables port (defaults to standard port)</param>
        public void SetTeamNumber(int teamNumber, int port = NtCoreNatives.NT_DEFAULT_PORT4)
        {
            NtCoreNatives.NT_SetServerTeam(handle, (uint)teamNumber, (uint)port);
        }

        /// <summary>
        /// Sets specific IP addresses and ports for NetworkTables connection
        /// </summary>
        /// <param name="addressesAndPorts">Array of address/port tuples to connect to</param>
        public void SetAddresses((string addr, int port)[] addressesAndPorts)
        {
            WpiString[] addresses = new WpiString[addressesAndPorts.Length];
            uint[] ports = new uint[addressesAndPorts.Length];

            try
            {
                for (int i = 0; i < addressesAndPorts.Length; i++)
                {
                    ports[i] = (uint)addressesAndPorts[i].port;
                    int byteCount = Encoding.UTF8.GetByteCount(addressesAndPorts[i].addr);
                    addresses[i].str = (byte*)Marshal.AllocHGlobal(byteCount);
                    addresses[i].len = (UIntPtr)byteCount;
                    fixed (char* c = addressesAndPorts[i].addr)
                    {
                        Encoding.UTF8.GetBytes(
                            c,
                            addressesAndPorts[i].addr.Length,
                            addresses[i].str,
                            byteCount
                        );
                    }
                }
                fixed (WpiString* addrs = addresses)
                {
                    fixed (uint* ps = ports)
                    {
                        NtCoreNatives.NT_SetServerMulti(
                            handle,
                            (UIntPtr)addressesAndPorts.Length,
                            addrs,
                            ps
                        );
                    }
                }
            }
            finally
            {
                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].str != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)addresses[i].str);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if this NetworkTables instance is currently connected to a server
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        public bool IsConnected()
        {
            return NtCoreNatives.NT_IsConnected(handle) != 0;
        }

        public DoubleSubscriber GetDoubleSubscriber(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("double");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(
                    topicHandle,
                    NtType.NT_DOUBLE,
                    &str,
                    &nOptions
                );
            }
            return new DoubleSubscriber(subHandle);
        }

        public DoublePublisher GetDoublePublisher(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("double");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Publish(
                    topicHandle,
                    NtType.NT_DOUBLE,
                    &str,
                    &nOptions
                );
            }
            return new DoublePublisher(subHandle);
        }

        public IntegerPublisher GetIntegerPublisher(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("int");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Publish(
                    topicHandle,
                    NtType.NT_INTEGER,
                    &str,
                    &nOptions
                );
            }
            return new IntegerPublisher(subHandle);
        }

        public IntegerSubscriber GetIntegerSubscriber(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("int");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(
                    topicHandle,
                    NtType.NT_INTEGER,
                    &str,
                    &nOptions
                );
            }
            return new IntegerSubscriber(subHandle);
        }

        public FloatArrayPublisher GetFloatArrayPublisher(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("float[]");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Publish(
                    topicHandle,
                    NtType.NT_FLOAT_ARRAY,
                    &str,
                    &nOptions
                );
            }
            return new FloatArrayPublisher(subHandle);
        }

        public FloatArraySubscriber GetFloatArraySubscriber(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("float[]");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(
                    topicHandle,
                    NtType.NT_FLOAT_ARRAY,
                    &str,
                    &nOptions
                );
            }
            return new FloatArraySubscriber(subHandle);
        }

        public unsafe RawPublisher GetRawPublisher(
            string name,
            string typeString,
            PubSubOptions options
        )
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes(typeString);

            uint pubHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                pubHandle = NtCoreNatives.NT_Publish(topicHandle, NtType.NT_RAW, &str, &nOptions);
            }
            return new RawPublisher(pubHandle);
        }

        public unsafe RawSubscriber GetRawSubscriber(
            string name,
            string typeString,
            PubSubOptions options
        )
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes(typeString);

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(topicHandle, NtType.NT_RAW, &str, &nOptions);
            }
            return new RawSubscriber(subHandle);
        }

        /// <summary>
        /// Creates a protobuf publisher for the specified topic and message type
        /// </summary>
        /// <typeparam name="T">The protobuf message type</typeparam>
        /// <param name="name">The topic name</param>
        /// <param name="classString">The protobuf class identifier</param>
        /// <param name="options">Publisher options</param>
        /// <returns>A protobuf publisher for the specified type</returns>
        public ProtobufPublisher<T> GetProtobufPublisher<T>(
            string name,
            string classString,
            PubSubOptions options
        )
            where T : IMessage<T>
        {
            var rawPublisher = GetRawPublisher(name, "proto:" + classString, options);
            return new ProtobufPublisher<T>(rawPublisher);
        }

        /// <summary>
        /// Creates a protobuf subscriber for the specified topic and message type
        /// </summary>
        /// <typeparam name="T">The protobuf message type</typeparam>
        /// <param name="name">The topic name</param>
        /// <param name="classString">The protobuf class identifier</param>
        /// <param name="options">Subscriber options</param>
        /// <returns>A protobuf subscriber for the specified type</returns>
        public ProtobufSubscriber<T> GetProtobufSubscriber<T>(
            string name,
            string classString,
            PubSubOptions options
        )
            where T : IMessage<T>, new()
        {
            var rawSubscriber = GetRawSubscriber(name, "proto:" + classString, options);
            return new ProtobufSubscriber<T>(rawSubscriber);
        }

        /// <summary>
        /// Creates a logger for NetworkTables internal messages within the specified level range
        /// </summary>
        /// <param name="minLevel">Minimum log level to capture</param>
        /// <param name="maxLevel">Maximum log level to capture</param>
        /// <returns>A polled logger for NetworkTables messages</returns>
        public PolledLogger CreateLogger(int minLevel, int maxLevel)
        {
            var poller = NtCoreNatives.NT_CreateListenerPoller(handle);
            NtCoreNatives.NT_AddPolledLogger(poller, (uint)minLevel, (uint)maxLevel);
            return new PolledLogger(poller);
        }

        /// <summary>
        /// Returns monotonic current time in 1 us increments.
        /// This is the same time base used for entry and connection timestamps.
        /// This function by default simply wraps WPI_Now(), but if NT_SetNow() is
        /// called, this function instead returns the value passed to NT_SetNow();
        /// this can be used to reduce overhead.
        /// </summary>
        /// <returns></returns>
        public long Now()
        {
            return NtCoreNatives.NT_Now();
        }
    }
}
