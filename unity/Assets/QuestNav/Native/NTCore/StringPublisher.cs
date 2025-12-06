using System;
using System.Text;

namespace QuestNav.Native.NTCore
{
    public unsafe class StringPublisher
    {
        private readonly uint handle;

        internal StringPublisher(uint handle)
        {
            this.handle = handle;
        }

        public unsafe bool Set(string value)
        {
            byte[] valueUtf8 = value is not null
                ? Encoding.UTF8.GetBytes(value)
                : Array.Empty<byte>();

            fixed (byte* ptr = valueUtf8)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)valueUtf8.Length };

                return NtCoreNatives.NT_SetString(handle, 0, &str) != 0;
            }
        }
    }
}
