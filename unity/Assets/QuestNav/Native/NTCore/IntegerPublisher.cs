namespace QuestNav.Native.NTCore
{
    public class IntegerPublisher
    {
        private readonly uint handle;

        internal IntegerPublisher(uint handle)
        {
            this.handle = handle;
        }

        public bool Set(long value)
        {
            return NtCoreNatives.NT_SetInteger(handle, 0, value) != 0;
        }
    }
}
