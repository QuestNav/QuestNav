using System;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;
using QuestNav.Utils;

namespace QuestNav.Native.NTCore
{
    public unsafe class NtInstance
    {
        private readonly uint handle;

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

        public void SetTeamNumber(int teamNumber, int port = NtCoreNatives.NT_DEFAULT_PORT4)
        {
            NtCoreNatives.NT_SetServerTeam(handle, (uint)teamNumber, (uint)port);
        }

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

        public unsafe RawPublisher GetRawPublisher(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("raw");

            uint pubHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                pubHandle = NtCoreNatives.NT_Publish(topicHandle, NtType.NT_RAW, &str, &nOptions);
            }
            return new RawPublisher(pubHandle);
        }

        public unsafe RawSubscriber GetRawSubscriber(string name, PubSubOptions options)
        {
            byte[] nameUtf8 = Encoding.UTF8.GetBytes(name);

            uint topicHandle;

            fixed (byte* ptr = nameUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)nameUtf8.Length };

                topicHandle = NtCoreNatives.NT_GetTopic(handle, &str);
            }

            byte[] typeStr = Encoding.UTF8.GetBytes("raw");

            uint subHandle;
            fixed (byte* ptr = typeStr)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)typeStr.Length };
                NativePubSubOptions nOptions = options.ToNative();
                subHandle = NtCoreNatives.NT_Subscribe(topicHandle, NtType.NT_RAW, &str, &nOptions);
            }
            return new RawSubscriber(subHandle);
        }

        public ProtobufPublisher<T> GetProtobufPublisher<T>(string name, PubSubOptions options)
            where T : IMessage<T>
        {
            var rawPublisher = GetRawPublisher(name, options);
            return new ProtobufPublisher<T>(rawPublisher);
        }

        public ProtobufSubscriber<T> GetProtobufSubscriber<T>(string name, PubSubOptions options)
            where T : IMessage<T>, new()
        {
            var rawSubscriber = GetRawSubscriber(name, options);
            return new ProtobufSubscriber<T>(rawSubscriber);
        }

        public PolledLogger CreateLogger(int minLevel, int maxLevel)
        {
            var poller = NtCoreNatives.NT_CreateListenerPoller(handle);
            NtCoreNatives.NT_AddPolledLogger(poller, (uint)minLevel, (uint)maxLevel);
            return new PolledLogger(poller);
        }
    }
}
