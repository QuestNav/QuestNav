using System;

namespace QuestNav.Native.NTCore
{
    public class RawPublisher
    {
        private readonly uint handle;

        internal RawPublisher(uint handle)
        {
            this.handle = handle;
        }

        public unsafe bool Set(byte[] value)
        {
            fixed (byte* ptr = value)
            {
                return NtCoreNatives.NT_SetRaw(handle, 0, ptr, (UIntPtr)value.Length) != 0;
            }
        }
    }
}
