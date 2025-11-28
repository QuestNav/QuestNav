namespace QuestNav.Native.NTCore
{
    public class BooleanSubscriber
    {   
        private readonly uint handle;

        internal BooleanSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public bool Get(bool defaultValue)
        {
            return NtCoreNatives.NT_GetBoolean(handle, defaultValue) != 0;
        }
    }
}