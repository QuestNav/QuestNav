namespace QuestNav.Native.NTCore
{
    public class BooleanPublisher
    {
        private readonly uint handle;

        internal BooleanPublisher(uint handle)
        {
            this.handle = handle;
        }

        public bool Set(bool value)
        {
            return NtCoreNatives.NT_SetBoolean(handle, 0, value) != 0;
        }
    }
}
