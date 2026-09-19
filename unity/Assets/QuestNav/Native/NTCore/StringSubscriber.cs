using System.Text;

namespace QuestNav.Native.NTCore
{
    public class StringSubscriber
    {
        private readonly uint handle;

        internal StringSubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe string Get(string defaultValue)
        {
            string result = null;
            ManagedWpiString defaultWpi = new ManagedWpiString(defaultValue);
            WpiString outValue = new WpiString();
            NtCoreNatives.NT_GetString(handle, defaultWpi, &outValue);

            if (outValue.str == defaultWpi)
            {
                // GetString returned our default value - no need to free.
                result = defaultValue;
            }
            else if (outValue.str != null)
            {
                try
                {
                    // Marshal string back to managed memory
                    result = Encoding.UTF8.GetString(outValue.str, (int)outValue.len);
                }
                finally
                {
                    NtCoreNatives.NT_FreeRaw(outValue.str);
                }
            }

            return result;
        }
    }
}
