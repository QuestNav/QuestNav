using System;

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
            Span<byte> scratch = stackalloc byte[Utf8Buffer.StackSize];
            using var utf8 = new Utf8Buffer(value, scratch);
            fixed (byte* ptr = utf8.Bytes)
            {
                WpiString str = new WpiString { str = ptr, len = (UIntPtr)utf8.Length };
                return NtCoreNatives.NT_SetString(handle, 0, &str) != 0;
            }
        }
    }
}
