using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LibJpegTurboUnity
{
    public class LJTCompressor : IDisposable
    {
        private readonly object @lock = new();
        private IntPtr compressorHandle;
        private bool isDisposed;

        public LJTCompressor()
        {
            compressorHandle = LJTImport.TjInitCompress();
            if (!(compressorHandle == IntPtr.Zero))
            {
                return;
            }

            LJTUtils.GetErrorAndThrow();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;
            lock (@lock)
            {
                if (isDisposed)
                {
                    return;
                }

                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        ~LJTCompressor()
        {
            Dispose(false);
        }

        public byte[] Compress(IntPtr srcPtr, int stride, int width, int height, LJTPixelFormat ljtPixelFormat,
            LJTSubsamplingOption subSamp, int quality, LJTFlags ljtFlags)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            CheckOptionsCompatibilityAndThrow(subSamp, ljtPixelFormat);
            var zero = IntPtr.Zero;
            ulong jpegSize = 0;
            try
            {
                if (LJTImport.TjCompress2(compressorHandle, srcPtr, width, stride, height,
                        (int) ljtPixelFormat,
                        ref zero, ref jpegSize, (int) subSamp, quality, (int) ljtFlags) == -1)
                {
                    LJTUtils.GetErrorAndThrow();
                }

                var destination = new byte[jpegSize];
                Marshal.Copy(zero, destination, 0, (int) jpegSize);
                return destination;
            }
            finally
            {
                LJTImport.TjFree(zero);
            }
        }

        public unsafe byte[] Compress(byte[] srcBuf, int stride, int width, int height, LJTPixelFormat ljtPixelFormat,
            LJTSubsamplingOption subSamp, int quality, LJTFlags ljtFlags)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("this");
            }

            CheckOptionsCompatibilityAndThrow(subSamp, ljtPixelFormat);
            var zero = IntPtr.Zero;
            ulong jpegSize = 0;
            try
            {
                fixed (byte* srcBuf1 = srcBuf)
                {
                    if (LJTImport.TjCompress2(compressorHandle, (IntPtr) srcBuf1, width, stride, height,
                            (int) ljtPixelFormat, ref zero, ref jpegSize, (int) subSamp, quality, (int) ljtFlags) == -1)
                    {
                        LJTUtils.GetErrorAndThrow();
                    }
                }

                var destination = new byte[jpegSize];
                Marshal.Copy(zero, destination, 0, (int) jpegSize);
                return destination;
            }
            finally
            {
                LJTImport.TjFree(zero);
            }
        }

        public byte[] EncodeJPG(Texture2D texture, int Quality,
            LJTSubsamplingOption ljtSubsampling = LJTSubsamplingOption.Chrominance420)
        {
            var ljtPixelFormat = LJTPixelFormat.RGB;
            var stride = texture.width * 3;
            switch (texture.format)
            {
                case TextureFormat.RGB24:
                    ljtPixelFormat = LJTPixelFormat.RGB;
                    stride = texture.width * 3;
                    break;
                case TextureFormat.RGBA32:
                    ljtPixelFormat = LJTPixelFormat.RGBA;
                    stride = texture.width * 4;
                    break;
                case TextureFormat.ARGB32:
                    ljtPixelFormat = LJTPixelFormat.ARGB;
                    stride = texture.width * 4;
                    break;
                case TextureFormat.BGRA32:
                    ljtPixelFormat = LJTPixelFormat.BGRA;
                    stride = texture.width * 4;
                    break;
            }

            return Compress(texture.GetRawTextureData(), stride, texture.width, texture.height, ljtPixelFormat,
                ljtSubsampling, Quality, LJTFlags.BottomUp);
        }

        public byte[] EncodeJPG(byte[] _RawTextureData, int _width, int _height, LJTPixelFormat _format, int Quality,
            LJTSubsamplingOption ljtSubsampling = LJTSubsamplingOption.Chrominance420)
        {
            var stride = _width * 3;
            switch (_format)
            {
                case LJTPixelFormat.RGB:
                    stride = _width * 3;
                    break;
                case LJTPixelFormat.RGBA:
                    stride = _width * 4;
                    break;
                case LJTPixelFormat.BGRA:
                    stride = _width * 4;
                    break;
                case LJTPixelFormat.ARGB:
                    stride = _width * 4;
                    break;
            }

            return Compress(_RawTextureData, stride, _width, _height, _format, ljtSubsampling, Quality,
                LJTFlags.BottomUp);
        }

        public byte[] EncodeJPG(byte[] _RawTextureData, int _width, int _height, int Quality,
            LJTSubsamplingOption ljtSubsampling = LJTSubsamplingOption.Chrominance420)
        {
            return Compress(_RawTextureData, _width * 3, _width, _height, LJTPixelFormat.RGB, ljtSubsampling, Quality,
                LJTFlags.BottomUp);
        }

        protected virtual void Dispose(bool callFromUserCode)
        {
            if (callFromUserCode)
            {
                isDisposed = true;
            }

            if (!(compressorHandle != IntPtr.Zero))
            {
                return;
            }

            LJTImport.TjDestroy(compressorHandle);
            compressorHandle = IntPtr.Zero;
        }

        private static void CheckOptionsCompatibilityAndThrow(LJTSubsamplingOption subSamp, LJTPixelFormat srcFormat)
        {
            if (srcFormat == LJTPixelFormat.Gray && subSamp != LJTSubsamplingOption.Gray)
            {
                throw new NotSupportedException(string.Format(
                    "Subsampling differ from {0} for pixel format {1} is not supported", LJTSubsamplingOption.Gray,
                    LJTPixelFormat.Gray));
            }
        }
    }
}