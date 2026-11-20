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
            Span<byte> scratch = stackalloc byte[Utf8Buffer.StackSize];
            using var utf8 = new Utf8Buffer(defaultValue, scratch);
            WpiString outValue = new WpiString();
            fixed (byte* ptr = utf8.Bytes)
            {
                WpiString defaultWpi = new WpiString { str = ptr, len = (UIntPtr)utf8.Length };
                NtCoreNatives.NT_GetString(handle, &defaultWpi, &outValue);
            }

            // ntcore always returns a fresh copy (even of the default); empty comes back as null
            if (outValue.str == null)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(outValue.str, (int)outValue.len);
            }
            finally
            {
                NtCoreNatives.NT_FreeRaw(outValue.str);
            }
        }
    }
}
