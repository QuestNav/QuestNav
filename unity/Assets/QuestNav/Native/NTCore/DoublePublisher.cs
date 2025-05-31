namespace QuestNav.Native.NTCore
{
    public class DoublePublisher
    {
        private readonly uint handle;

        internal DoublePublisher(uint handle)
        {
            this.handle = handle;
        }

        public bool Set(double value)
        {
            return NtCoreNatives.NT_SetDouble(handle, 0, value) != 0;
        }
    }
}
