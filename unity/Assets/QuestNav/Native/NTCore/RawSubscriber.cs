using System;

namespace QuestNav.Native.NTCore {
    public class RawSubscriber {
        private readonly uint handle;

        internal RawSubscriber(uint handle) {
            this.handle = handle;
        }

        public unsafe byte[] Get(byte[] defaultValue) {
            byte* res;
            UIntPtr len = UIntPtr.Zero;
            
            fixed (byte* ptr = defaultValue) {
                res = NtCoreNatives.NT_GetRaw(handle, ptr, (UIntPtr)defaultValue.Length, &len);
            }

            byte[] ret = new byte[(int)len];
            for (int i = 0; i < ret.Length; i++) {
                ret[i] = res[i];
            }

            NtCoreNatives.NT_FreeRaw(res);

            return ret;
        }
    }
}