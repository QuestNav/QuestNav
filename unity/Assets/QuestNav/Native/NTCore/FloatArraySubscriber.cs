using System;

namespace QuestNav.Native.NTCore
{
    public class FloatArraySubscriber
    {
        private readonly uint handle;

        internal FloatArraySubscriber(uint handle)
        {
            this.handle = handle;
        }

        public unsafe float[] Get(float[] defaultValue)
        {
            float* res;
            UIntPtr len = UIntPtr.Zero;
            fixed (float* ptr = defaultValue)
            {
                res = NtCoreNatives.NT_GetFloatArray(
                    handle,
                    ptr,
                    (UIntPtr)defaultValue.Length,
                    &len
                );
            }

            float[] ret = new float[(int)len];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = res[i];
            }

            NtCoreNatives.NT_FreeFloatArray(res);

            return ret;
        }
    }
}
