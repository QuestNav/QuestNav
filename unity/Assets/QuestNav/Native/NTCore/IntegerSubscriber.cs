namespace QuestNav.Native.NTCore {
    public class IntegerSubscriber {
        private readonly uint handle;

        internal IntegerSubscriber(uint handle) {
            this.handle = handle;
        }

        public long Get(long defaultValue) {
            return NtCoreNatives.NT_GetInteger(handle, defaultValue);
        }
    }
}
