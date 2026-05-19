using System;
using UnityEngine;

namespace LibJpegTurboUnity
{
    public class LJTDecompressor : IDisposable
    {
        private readonly object @lock = new object();
        private IntPtr decompressorHandle = IntPtr.Zero;
        private bool isDisposed;

        public LJTDecompressor()
        {
            this.decompressorHandle = LJTImport.TjInitDecompress();
            if (!(this.decompressorHandle == IntPtr.Zero))
            {
                return;
            }

            LJTUtils.GetErrorAndThrow();
        }

        ~LJTDecompressor() => this.Dispose(false);

        public unsafe byte[] Decompress(IntPtr jpegBuf, ulong jpegBufSize, LJTPixelFormat destPixelFormat,
            LJTFlags flags, out int width, out int height, out int stride)
        {
            int bufSize;
            this.GetImageInfo(jpegBuf, jpegBufSize, destPixelFormat, out width, out height, out stride, out bufSize);
            byte[] numArray;
            fixed (byte* outBuf = numArray = new byte[bufSize])
            {
                this.Decompress(jpegBuf, jpegBufSize, (IntPtr) (void*) outBuf, bufSize, destPixelFormat, flags,
                    out width, out height, out stride);
            }

            return numArray;
        }

        public void Decompress(IntPtr jpegBuf, ulong jpegBufSize, IntPtr outBuf, int outBufSize,
            LJTPixelFormat destPixelFormat, LJTFlags flags, out int width, out int height, out int stride)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            if (LJTImport.TjDecompressHeader(this.decompressorHandle, jpegBuf, jpegBufSize, out width, out height,
                    out int _, out int _) == -1)
            {
                LJTUtils.GetErrorAndThrow();
            }

            LJTPixelFormat tjPixelFormat = destPixelFormat;
            stride = width * LJTImport.PixelSizes[tjPixelFormat];
            int num = stride * height;
            if (outBufSize < num)
            {
                throw new ArgumentOutOfRangeException(outBufSize.ToString());
            }

            if (LJTImport.TjDecompress(this.decompressorHandle, jpegBuf, jpegBufSize, outBuf, width, stride, height,
                    (int) tjPixelFormat, (int) flags) != -1)
            {
                return;
            }

            LJTUtils.GetErrorAndThrow();
        }

        public unsafe byte[] Decompress(byte[] jpegBuf, LJTPixelFormat destPixelFormat, LJTFlags flags, out int width,
            out int height, out int stride)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            ulong length = (ulong) jpegBuf.Length;
            fixed (byte* jpegBuf1 = jpegBuf)
            {
                return this.Decompress((IntPtr) (void*) jpegBuf1, length, destPixelFormat, flags, out width, out height,
                    out stride);
            }
        }

        public DecompressedImage Decompress(IntPtr jpegBuf, ulong jpegBufSize, LJTPixelFormat destPixelFormat,
            LJTFlags flags)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            int width;
            int height;
            int stride;
            byte[] data = this.Decompress(jpegBuf, jpegBufSize, destPixelFormat, flags, out width, out height,
                out stride);
            return new DecompressedImage(width, height, stride, data, destPixelFormat);
        }

        public unsafe DecompressedImage Decompress(byte[] jpegBuf, LJTPixelFormat destPixelFormat, LJTFlags flags)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            ulong length = (ulong) jpegBuf.Length;
            fixed (byte* jpegBuf1 = jpegBuf)
            {
                return this.Decompress((IntPtr) (void*) jpegBuf1, length, destPixelFormat, flags);
            }
        }

        public DecompressedImage DecodeJPG(byte[] jpegBuf, TextureFormat format)
        {
            LJTPixelFormat destPixelFormat = LJTPixelFormat.RGB;
            switch (format)
            {
                case TextureFormat.RGB24:
                    destPixelFormat = LJTPixelFormat.RGB;
                    break;
                case TextureFormat.RGBA32:
                    destPixelFormat = LJTPixelFormat.RGBA;
                    break;
                case TextureFormat.ARGB32:
                    destPixelFormat = LJTPixelFormat.ARGB;
                    break;
                case TextureFormat.BGRA32:
                    destPixelFormat = LJTPixelFormat.BGRA;
                    break;
                case TextureFormat.R8:
                    destPixelFormat = LJTPixelFormat.Gray;
                    break;
            }

            return this.Decompress(jpegBuf, destPixelFormat, LJTFlags.BottomUp);
        }

        public DecompressedImage DecodeJPG(byte[] jpegBuf)
        {
            return this.Decompress(jpegBuf, LJTPixelFormat.RGB, LJTFlags.BottomUp);
        }

        public DecompressedImage DecodeJPG(byte[] jpegBuf, LJTPixelFormat _format)
        {
            return this.Decompress(jpegBuf, _format, LJTFlags.BottomUp);
        }

        public void GetImageInfo(IntPtr jpegBuf, ulong jpegBufSize, LJTPixelFormat destPixelFormat, out int width,
            out int height, out int stride, out int bufSize)
        {
            LJTImport.TjDecompressHeader(this.decompressorHandle, jpegBuf, jpegBufSize, out width, out height,
                out int _, out int _);
            stride = width * LJTImport.PixelSizes[destPixelFormat];
            bufSize = stride * height;
        }

        public int GetBufferSize(int height, int width, LJTPixelFormat destPixelFormat)
        {
            return LJTImport.TJPAD(width * LJTImport.PixelSizes[destPixelFormat]) * height;
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.Dispose(true);
                GC.SuppressFinalize((object) this);
            }
        }

        protected virtual void Dispose(bool callFromUserCode)
        {
            if (callFromUserCode)
            {
                this.isDisposed = true;
            }

            if (!(this.decompressorHandle != IntPtr.Zero))
            {
                return;
            }

            LJTImport.TjDestroy(this.decompressorHandle);
            this.decompressorHandle = IntPtr.Zero;
        }
    }
}