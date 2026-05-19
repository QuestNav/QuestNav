using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibJpegTurboUnity
{
    internal static class LJTImport
    {
        private const string LibraryName = "turbojpeg";

        public static readonly Dictionary<LJTPixelFormat, int> PixelSizes = new()
        {
            {
                LJTPixelFormat.RGB, 3
            },
            {
                LJTPixelFormat.BGR, 3
            },
            {
                LJTPixelFormat.RGBX, 4
            },
            {
                LJTPixelFormat.BGRX, 4
            },
            {
                LJTPixelFormat.XBGR, 4
            },
            {
                LJTPixelFormat.XRGB, 4
            },
            {
                LJTPixelFormat.Gray, 1
            },
            {
                LJTPixelFormat.RGBA, 4
            },
            {
                LJTPixelFormat.BGRA, 4
            },
            {
                LJTPixelFormat.ABGR, 4
            },
            {
                LJTPixelFormat.ARGB, 4
            },
            {
                LJTPixelFormat.CMYK, 4
            }
        };

        public static readonly Dictionary<LJTSubsamplingOption, LJTSize> MCUSizes = new()
        {
            {
                LJTSubsamplingOption.Gray, new LJTSize(8, 8)
            },
            {
                LJTSubsamplingOption.Chrominance444, new LJTSize(8, 8)
            },
            {
                LJTSubsamplingOption.Chrominance422, new LJTSize(16, 8)
            },
            {
                LJTSubsamplingOption.Chrominance420, new LJTSize(16, 16)
            },
            {
                LJTSubsamplingOption.Chrominance440, new LJTSize(8, 16)
            },
            {
                LJTSubsamplingOption.Chrominance411, new LJTSize(32, 8)
            }
        };

        public static bool LibraryFound { get; set; } = true;

        public static int TJPAD(int width)
        {
            return (width + 3) & -4;
        }

        public static int TJSCALED(int dimension, LJTScalingFactor ljtScalingFactor)
        {
            return (dimension * ljtScalingFactor.Num + ljtScalingFactor.Denom - 1) / ljtScalingFactor.Denom;
        }

        [DllImport("turbojpeg", EntryPoint = "tjInitCompress")]
        public static extern IntPtr TjInitCompress();

        [DllImport("turbojpeg", EntryPoint = "tjCompress2")]
        public static extern int TjCompress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height,
            int pixelFormat, ref IntPtr jpegBuf, ref ulong jpegSize, int jpegSubsamp, int jpegQual, int flags);

        [DllImport("turbojpeg", EntryPoint = "tjBufSize")]
        public static extern long TjBufSize(int width, int height, int jpegSubsamp);

        [DllImport("turbojpeg", EntryPoint = "tjInitDecompress")]
        public static extern IntPtr TjInitDecompress();

        public static int TjDecompressHeader(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width,
            out int height,
            out int jpegSubsamp, out int jpegColorspace)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return TjDecompressHeader3_x86(handle, jpegBuf, (uint) jpegSize, out width,
                        out height, out jpegSubsamp, out jpegColorspace);
                case 8:
                    return TjDecompressHeader3_x64(handle, jpegBuf, jpegSize, out width, out height,
                        out jpegSubsamp, out jpegColorspace);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }

        [DllImport("turbojpeg", EntryPoint = "tjGetScalingFactors")]
        public static extern IntPtr TjGetScalingFactors(out int numscalingfactors);

        public static int TjDecompress(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf, int width,
            int pitch,
            int height, int pixelFormat, int flags)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return TjDecompress2_x86(handle, jpegBuf, (uint) jpegSize, dstBuf, width, pitch,
                        height, pixelFormat, flags);
                case 8:
                    return TjDecompress2_x64(handle, jpegBuf, jpegSize, dstBuf, width, pitch, height,
                        pixelFormat, flags);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }

        [DllImport("turbojpeg", EntryPoint = "tjAlloc")]
        public static extern IntPtr TjAlloc(int bytes);

        [DllImport("turbojpeg", EntryPoint = "tjFree")]
        public static extern void TjFree(IntPtr buffer);

        [DllImport("turbojpeg", EntryPoint = "tjInitTransform")]
        public static extern IntPtr TjInitTransform();

        public static int TjTransform(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, int n, IntPtr[] dstBufs,
            ulong[] dstSizes, IntPtr transforms, int flags)
        {
            var dstSizes1 = new uint[dstSizes.Length];
            for (var index = 0; index < dstSizes.Length; ++index)
            {
                dstSizes1[index] = (uint) dstSizes[index];
            }

            int num;
            switch (IntPtr.Size)
            {
                case 4:
                    num = TjTransform_x86(handle, jpegBuf, (uint) jpegSize, n, dstBufs, dstSizes1,
                        transforms, flags);
                    break;
                case 8:
                    num = TjTransform_x64(handle, jpegBuf, jpegSize, n, dstBufs, dstSizes1, transforms,
                        flags);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }

            for (var index = 0; index < dstSizes.Length; ++index)
            {
                dstSizes[index] = dstSizes1[index];
            }

            return num;
        }

        [DllImport("turbojpeg", EntryPoint = "tjDestroy")]
        public static extern int TjDestroy(IntPtr handle);

        [DllImport("turbojpeg", EntryPoint = "tjGetErrorStr", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string TjGetErrorStr();

        [DllImport("turbojpeg", EntryPoint = "tjDecompressHeader3")]
        private static extern int TjDecompressHeader3_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, out int width,
            out int height, out int jpegSubsamp, out int jpegColorspace);

        [DllImport("turbojpeg", EntryPoint = "tjDecompressHeader3")]
        private static extern int TjDecompressHeader3_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width,
            out int height, out int jpegSubsamp, out int jpegColorspace);

        [DllImport("turbojpeg", EntryPoint = "tjDecompress2")]
        private static extern int TjDecompress2_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, IntPtr dstBuf,
            int width,
            int pitch, int height, int pixelFormat, int flags);

        [DllImport("turbojpeg", EntryPoint = "tjDecompress2")]
        private static extern int TjDecompress2_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf,
            int width,
            int pitch, int height, int pixelFormat, int flags);

        [DllImport("turbojpeg", EntryPoint = "tjTransform")]
        private static extern int TjTransform_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, int n, IntPtr[] dstBufs,
            uint[] dstSizes, IntPtr transforms, int flags);

        [DllImport("turbojpeg", EntryPoint = "tjTransform")]
        private static extern int TjTransform_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, int n,
            IntPtr[] dstBufs,
            uint[] dstSizes, IntPtr transforms, int flags);
    }
}