using System;
using System.Runtime.InteropServices;

namespace LibJpegTurboUnity
{
    internal static class LJTUtils
    {
        public static void GetErrorAndThrow()
        {
        }

        public static string GetPlatformName()
        {
            return IntPtr.Size != 4 ? "x64" : "x86";
        }

        public static IntPtr StructArrayToIntPtr<T>(T[] structArray)
        {
            var num = Marshal.SizeOf(typeof(T));
            var intPtr = Marshal.AllocHGlobal(structArray.Length * num);
            var int64 = intPtr.ToInt64();
            foreach (var structure in structArray)
            {
                Marshal.StructureToPtr(structure ?? throw new InvalidOperationException(), new IntPtr(int64), false);
                int64 += num;
            }

            return intPtr;
        }

        public static IntPtr CopyDataToPointer(byte[] data, bool useComAllocation = false)
        {
            var destination =
                useComAllocation ? Marshal.AllocCoTaskMem(data.Length) : Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, destination, data.Length);
            return destination;
        }

        public static void FreePtr(IntPtr ptr, bool isComAllocated = false)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            if (isComAllocated)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
            else
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}