using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuestNav.Native.NTCore
{
    public class RawSubscriber
    {
        private readonly uint handle;

        internal RawSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe byte[] Get(byte[] defaultValue)
        {
            byte* res;
            UIntPtr len = UIntPtr.Zero;

            fixed (byte* ptr = defaultValue)
            {
                res = NtCoreNatives.NT_GetRaw(handle, ptr, (UIntPtr)defaultValue.Length, &len);
            }

            byte[] ret = new byte[(int)len];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = res[i];
            }

            NtCoreNatives.NT_FreeRaw(res);

            return ret;
        }

        /// <summary>
        /// Reads the raw value queue for this subscriber.
        /// </summary>
        /// <returns>An array of TimestampedValue&lt;byte[]&gt; representing the raw values with timestamps in the queue</returns>
        public unsafe TimestampedValue<byte[]>[] ReadQueue()
        {
            UIntPtr count;
            IntPtr valuesIntPtr = NtCoreNatives.NT_ReadQueueValue(handle, out count);
            try
            {
                int valueCount = (int)count.ToUInt32();

                if (valuesIntPtr == IntPtr.Zero || valueCount == 0)
                {
                    // No results, fast return
                    return Array.Empty<TimestampedValue<byte[]>>();
                }

                var results = new List<TimestampedValue<byte[]>>(valueCount);
                long valuesPtr = valuesIntPtr.ToInt64();
                int structSize = sizeof(NativeNetworkTableValue);

                for (int i = 0; i < valueCount; i++)
                {
                    IntPtr elementPtr = (IntPtr)(valuesPtr + i * structSize);
                    NativeNetworkTableValue value = Marshal.PtrToStructure<NativeNetworkTableValue>(
                        elementPtr
                    );
                    var raw = value.data.valueRaw;
                    int arrLen = (int)raw.size;
                    byte[] arr = new byte[arrLen];
                    if (arrLen > 0)
                    {
                        Marshal.Copy((IntPtr)raw.data, arr, 0, arrLen);
                    }

                    // Create TimestampedValue with the byte array and timestamp information
                    var timestampedValue = new TimestampedValue<byte[]>(
                        arr,
                        value.serverTime,
                        value.lastChange
                    );
                    results.Add(timestampedValue);
                }
                return results.ToArray();
            }
            finally
            {
                if (valuesIntPtr != IntPtr.Zero && count.ToUInt32() > 0)
                {
                    NtCoreNatives.NT_DisposeValueArray(valuesIntPtr, count);
                }
            }
        }
    }
}
