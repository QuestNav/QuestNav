namespace QuestNav.Native.NTCore
{
    public class DoubleSubscriber
    {
        private readonly uint handle;

        internal DoubleSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public double Get(double defaultValue)
        {
            return NtCoreNatives.NT_GetDouble(handle, defaultValue);
        }
    }
}
