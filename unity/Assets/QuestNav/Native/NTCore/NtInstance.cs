using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
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

            using (var nameStr = new ManagedWpiString(instanceName))
            {
                NtCoreNatives.NT_StartClient(handle, nameStr);
            }
        }

        /// <summary>
        /// Sets server addresses and port for client(without restarting client).
        /// Attempts connections to the following addresses in parallel:
        /// - 10.TE.AM.2
        /// - 172.26.0.1 on Windows, or 172.27.0.1 on other platforms(USB)
        /// -172.30.0.1 (WiFi)
        /// It also connects using matching Systemcore mDNS announcements.
        /// The team - specific 10.TE.AM.2 address is only added if the team string
        /// parses as an integer in the range 0 to 25599 inclusive.
        /// @param inst instance handle
        /// </summary>
        /// <param name="teamNumber">The FRC team number string</param>
        /// <param name="port">The NetworkTables port (defaults to standard port)</param>
        public void SetTeamNumber(string teamNumber, int port = NtCoreNatives.NT_DEFAULT_PORT4)
        {
            using (ManagedWpiString teamNumberStr = new ManagedWpiString(teamNumber))
            {
                NtCoreNatives.NT_SetServerTeam(handle, teamNumberStr, (uint)port);
            }
        }

        /// <summary>
        /// Sets specific IP addresses and ports for NetworkTables connection
        /// </summary>
        /// <param name="addressesAndPorts">Array of address/port tuples to connect to</param>
        public void SetAddresses((string addr, int port)[] addressesAndPorts)
        {
            var managedAddresses = new ManagedWpiString[addressesAndPorts.Length];
            uint[] ports = new uint[addressesAndPorts.Length];

            try
            {
                for (int i = 0; i < addressesAndPorts.Length; i++)
                {
                    ports[i] = (uint)addressesAndPorts[i].port;
                    managedAddresses[i] = new ManagedWpiString(addressesAndPorts[i].addr);
                }

                // Marshal the managed wrappers to native pointers
                WpiString[] nativeAddresses = new WpiString[addressesAndPorts.Length];
                for (int i = 0; i < addressesAndPorts.Length; i++)
                {
                    nativeAddresses[i] = *managedAddresses[i].NativePointer;
                }

                fixed (WpiString* addrs = nativeAddresses)
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
                for (int i = 0; i < managedAddresses.Length; i++)
                {
                    managedAddresses[i]?.Dispose();
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

        /// <summary>
        ///  Get the time offset between server time and local time. Add this value to
        ///  local time to get the estimated equivalent server time. This returns the time
        ///  offset only if the client and server are connected and have exchanged
        ///  synchronization messages. Note the time offset may change over time as it is
        ///  periodically updated.
        /// </summary>
        /// <returns>Time offset in nanoseconds, or zero if not available</returns>
        public long GetServerTimeOffset()
        {
            int valid = 0;
            long offset = NtCoreNatives.NT_GetServerTimeOffset(handle, &valid);
            if (valid == 0)
            {
                return 0;
            }
            return offset;
        }

        public BooleanPublisher GetBooleanPublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_BOOLEAN,
                GetTypeString(NtType.NT_BOOLEAN),
                options
            );
            return new BooleanPublisher(pubHandle);
        }

        public BooleanSubscriber GetBooleanSubscriber(string name, PubSubOptions options)
        {
            var subHandle = Subscribe(
                name,
                NtType.NT_BOOLEAN,
                GetTypeString(NtType.NT_BOOLEAN),
                options
            );
            return new BooleanSubscriber(subHandle);
        }

        public DoubleSubscriber GetDoubleSubscriber(string name, PubSubOptions options)
        {
            var subHandle = Subscribe(
                name,
                NtType.NT_DOUBLE,
                GetTypeString(NtType.NT_DOUBLE),
                options
            );
            return new DoubleSubscriber(subHandle);
        }

        public DoublePublisher GetDoublePublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_DOUBLE,
                GetTypeString(NtType.NT_DOUBLE),
                options
            );
            return new DoublePublisher(pubHandle);
        }

        public IntegerPublisher GetIntegerPublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_INTEGER,
                GetTypeString(NtType.NT_INTEGER),
                options
            );
            return new IntegerPublisher(pubHandle);
        }

        public IntegerSubscriber GetIntegerSubscriber(string name, PubSubOptions options)
        {
            var subHandle = Subscribe(
                name,
                NtType.NT_INTEGER,
                GetTypeString(NtType.NT_INTEGER),
                options
            );
            return new IntegerSubscriber(subHandle);
        }

        public FloatArrayPublisher GetFloatArrayPublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_FLOAT_ARRAY,
                GetTypeString(NtType.NT_FLOAT_ARRAY),
                options
            );
            return new FloatArrayPublisher(pubHandle);
        }

        public FloatArraySubscriber GetFloatArraySubscriber(string name, PubSubOptions options)
        {
            var subHandle = Subscribe(
                name,
                NtType.NT_FLOAT_ARRAY,
                GetTypeString(NtType.NT_FLOAT_ARRAY),
                options
            );
            return new FloatArraySubscriber(subHandle);
        }

        public StringPublisher GetStringPublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_STRING,
                GetTypeString(NtType.NT_STRING),
                options
            );
            return new StringPublisher(pubHandle);
        }

        public StringSubscriber GetStringSubscriber(string name, PubSubOptions options)
        {
            var subHandle = Subscribe(
                name,
                NtType.NT_STRING,
                GetTypeString(NtType.NT_STRING),
                options
            );
            return new StringSubscriber(subHandle);
        }

        public StringEntry GetStringEntry(string name, PubSubOptions options)
        {
            var subHandle = GetEntry(
                name,
                NtType.NT_STRING,
                GetTypeString(NtType.NT_STRING),
                options
            );
            return new StringEntry(subHandle);
        }

        public StringArrayPublisher GetStringArrayPublisher(string name, PubSubOptions options)
        {
            var pubHandle = Publish(
                name,
                NtType.NT_STRING_ARRAY,
                GetTypeString(NtType.NT_STRING_ARRAY),
                options
            );
            return new StringArrayPublisher(pubHandle);
        }

        public RawPublisher GetRawPublisher(string name, string typeString, PubSubOptions options)
        {
            var pubHandle = Publish(name, NtType.NT_RAW, typeString, options);
            return new RawPublisher(pubHandle);
        }

        public RawSubscriber GetRawSubscriber(string name, string typeString, PubSubOptions options)
        {
            var subHandle = Subscribe(name, NtType.NT_RAW, typeString, options);
            return new RawSubscriber(subHandle);
        }

        /// <summary>
        /// Creates a protobuf publisher for the specified topic and message type
        /// </summary>
        /// <typeparam name="T">The protobuf message type</typeparam>
        /// <param name="name">The topic name</param>
        /// <param name="messageDescriptor">The protobuf message descriptor for the message type</param>
        /// <param name="options">Publisher options</param>
        /// <returns>A protobuf publisher for the specified type</returns>
        public ProtobufPublisher<T> GetProtobufPublisher<T>(
            string name,
            MessageDescriptor messageDescriptor,
            PubSubOptions options
        )
            where T : IMessage<T>
        {
            AddProtobufSchema(messageDescriptor);
            var rawPublisher = GetRawPublisher(
                name,
                "proto:" + messageDescriptor.FullName,
                options
            );
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
        /// Registers a data schema.  Data schemas provide information for how a certain data type string can be
        /// decoded.  This is used to enable rich client features like automatic decoding and display of protobuf
        /// messages in tools that support NetworkTables schemas, like AdvantageScope and OutlineViewer. This will
        /// publish the whole file descriptor for the protobuf message. The schema is published with the name that
        /// of the protobuf file, and the type is "proto:FileDescriptorProto".
        /// </summary>
        /// <param name="descriptor">Protobuf MessageDescriptor whose schema to publish</param>
        private unsafe void AddProtobufSchema(MessageDescriptor descriptor)
        {
            // Get the file descriptor for the message type
            var file = descriptor.File;

            // Get the schema as a byte array, this is what will be published
            var schema = file.ToProto().ToByteArray();

            using (var nameStr = new ManagedWpiString("proto:" + file.Name))
            using (var typeStr = new ManagedWpiString("proto:FileDescriptorProto"))
            {
                fixed (byte* schemaPtr = schema)
                {
                    NtCoreNatives.NT_AddSchema(
                        handle,
                        nameStr,
                        typeStr,
                        schemaPtr,
                        (UIntPtr)schema.Length
                    );
                }
            }
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
        /// Returns monotonic current time in 1 ns increments.
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

        /// <summary>
        /// Maps an NtType to its NetworkTables type string representation (e.g., "boolean", "string[]").
        /// </summary>
        /// <param name="type">The NetworkTables type.</param>
        /// <returns>The string representation for the specified type.</returns>
        private static string GetTypeString(NtType type)
        {
            return type switch
            {
                NtType.NT_BOOLEAN => "boolean",
                NtType.NT_DOUBLE => "double",
                NtType.NT_STRING => "string",
                NtType.NT_BOOLEAN_ARRAY => "boolean[]",
                NtType.NT_DOUBLE_ARRAY => "double[]",
                NtType.NT_STRING_ARRAY => "string[]",
                NtType.NT_RPC => "rpc",
                NtType.NT_INTEGER => "int",
                NtType.NT_FLOAT => "float",
                NtType.NT_INTEGER_ARRAY => "int[]",
                NtType.NT_FLOAT_ARRAY => "float[]",
                _ => "raw",
            };
        }

        /// <summary>
        /// Creates a native publisher handle for the given topic and type.
        /// </summary>
        /// <param name="name">Topic name.</param>
        /// <param name="type">NetworkTables value type.</param>
        /// <param name="typeString">NetworkTables type string (e.g., "double", "int[]").</param>
        /// <param name="options">Publisher options.</param>
        private uint Publish(string name, NtType type, string typeString, PubSubOptions options)
        {
            uint topicHandle = GetTopic(name);
            uint pubHandle;
            using (var typeStr = new ManagedWpiString(typeString))
            {
                NativePubSubOptions nOptions = options.ToNative();
                pubHandle = NtCoreNatives.NT_Publish(topicHandle, type, typeStr, &nOptions);
            }
            return pubHandle;
        }

        /// <summary>
        /// Creates a native subscriber handle for the given topic and type.
        /// </summary>
        /// <param name="name">Topic name.</param>
        /// <param name="type">NetworkTables value type.</param>
        /// <param name="typeString">NetworkTables type string (e.g., "double", "int[]").</param>
        /// <param name="options">Subscriber options.</param>
        private uint Subscribe(string name, NtType type, string typeString, PubSubOptions options)
        {
            uint topicHandle = GetTopic(name);
            uint subHandle;
            using (var typeStr = new ManagedWpiString(typeString))
            {
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(topicHandle, type, typeStr, &nOptions);
            }
            return subHandle;
        }

        /// <summary>
        /// Retrieves the entry handle for a specified entry in the system, identified by its name, type, and options.
        /// </summary>
        /// <param name="name">Topic name.</param>
        /// <param name="type">NetworkTables value type.</param>
        /// <param name="typeString">NetworkTables type string (e.g., "double", "int[]").</param>
        /// <param name="options">Subscriber options.</param>
        private uint GetEntry(string name, NtType type, string typeString, PubSubOptions options)
        {
            uint topicHandle = GetTopic(name);
            uint subHandle;
            using (var typeStr = new ManagedWpiString(typeString))
            {
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_GetEntryEx(topicHandle, type, typeStr, &nOptions);
            }
            return subHandle;
        }

        private uint GetTopic(string name)
        {
            using (var nameStr = new ManagedWpiString(name))
            {
                return NtCoreNatives.NT_GetTopic(handle, nameStr);
            }
        }
    }
}
