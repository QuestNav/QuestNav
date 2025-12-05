using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace QuestNav.Native.NTCore
{
    public unsafe class StringArrayPublisher
    {
        private readonly uint handle;
        private string[] currentValue;

        internal StringArrayPublisher(uint handle)
        {
            this.handle = handle;
            this.currentValue = Array.Empty<string>();
        }

        private bool HasValueChanged(string[] newValue)
        {
            if (currentValue.Length != newValue.Length)
            {
                return true;
            }

            for (int i = 0; i < newValue.Length; i++)
            {
                if (!currentValue[i].Equals(newValue[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Set(string[] values)
        {
            values ??= Array.Empty<string>();
            // Avoid unnecessary marshaling & allocation for unchanged values
            if (!HasValueChanged(values))
            {
                return false;
            }

            bool result;
            WpiString[] wpiStrings = new WpiString[values.Length];

            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int byteCount = Encoding.UTF8.GetByteCount(values[i]);
                    wpiStrings[i].str = (byte*)Marshal.AllocHGlobal(byteCount);
                    wpiStrings[i].len = (UIntPtr)byteCount;
                    fixed (char* c = values[i])
                    {
                        Encoding.UTF8.GetBytes(c, values[i].Length, wpiStrings[i].str, byteCount);
                    }
                }
                fixed (WpiString* ptr = wpiStrings)
                {
                    result =
                        NtCoreNatives.NT_SetStringArray(handle, 0, ptr, (UIntPtr)values.Length)
                        != 0;
                }
            }
            finally
            {
                for (int i = 0; i < wpiStrings.Length; i++)
                {
                    if (wpiStrings[i].str != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)wpiStrings[i].str);
                    }
                }
            }

            if (result)
            {
                currentValue = (string[])values.Clone();
            }

            return result;
        }
    }
}
