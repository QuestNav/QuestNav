using System;
using System.Text;

namespace QuestNav.Native.NTCore
{
    public class StringSubscriber
    {
        private readonly uint handle;

        internal StringSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe string Get(string defaultValue)
        {
            byte[] valueUtf8 = defaultValue is not null
                ? Encoding.UTF8.GetBytes(defaultValue)
                : Array.Empty<byte>();

            string result = null;
            fixed (byte* ptr = valueUtf8)
            {
                WpiString defaultWpi = new WpiString { str = ptr, len = (UIntPtr)valueUtf8.Length };
                WpiString outValue = new WpiString();
                NtCoreNatives.NT_GetString(handle, &defaultWpi, &outValue);

                if (outValue.str == defaultWpi.str)
                {
                    // GetString returned our default value - no need to free.
                    result = defaultValue;
                }
                else if (outValue.str != null)
                {
                    try
                    {
                        // Marshal string back to managed memory
                        result = Encoding.UTF8.GetString(outValue.str, (int)outValue.len);
                    }
                    finally
                    {
                        NtCoreNatives.NT_FreeRaw(outValue.str);
                    }
                }
            }

            return result;
        }
    }
}
