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
            ManagedWpiString str = new ManagedWpiString(value);
            return NtCoreNatives.NT_SetString(handle, 0, str) != 0;
        }
    }
}
