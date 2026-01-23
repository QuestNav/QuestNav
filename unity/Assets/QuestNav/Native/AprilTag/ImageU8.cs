using System;
using QuestNav.Camera;
using QuestNav.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace QuestNav.Native.AprilTag
{
    public unsafe class ImageU8 : IDisposable
    {
        internal ImageU8Native* Handle { get; private set; }
        private bool disposed;
        private bool ownsHandle;

        private ImageU8(ImageU8Native* img, bool ownsHandle = true)
        {
            Handle = img;
            this.ownsHandle = ownsHandle;
        }

        private static ImageU8Native* cachedImage;
        private static int cachedWidth;
        private static int cachedHeight;

        /// <summary>
        /// Creates an ImageU8 from PassthroughCameraAccess.GetColors()
        /// </summary>
        public static ImageU8 FromPassthroughCamera(
            NativeArray<Color32> colors,
            int width,
            int height,
            bool flipVertically = true
        )
        {
            if (!colors.IsCreated || colors.Length == 0)
            {
                QueuedLogger.LogError("Colors array is invalid");
                return null;
            }

            // Reuse native image buffer
            if (cachedImage == null || cachedWidth != width || cachedHeight != height)
            {
                if (cachedImage != null)
                {
                    AprilTagNatives.image_u8_destroy(cachedImage);
                }

                cachedImage = AprilTagNatives.image_u8_create(width, height);
                cachedWidth = width;
                cachedHeight = height;

                if (cachedImage == null)
                {
                    QueuedLogger.LogError("Failed to create native ImageU8");
                    return null;
                }
            }

            // Wrap destination buffer
            NativeArray<byte> destData =
                NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                    (void*)cachedImage->buf,
                    height * cachedImage->stride,
                    Allocator.None
                );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(
                ref destData,
                AtomicSafetyHandle.GetTempMemoryHandle()
            );
#endif

            // Run parallel grayscale conversion
            var job = new GrayscaleConversionJob
            {
                Source = colors.Reinterpret<byte>(4),
                Destination = destData,
                Width = width,
                Height = height,
                Stride = cachedImage->stride,
                FlipVertically = flipVertically,
            };

            job.Schedule(height, 32).Complete();

            return new ImageU8(cachedImage, ownsHandle: false);
        }

        public static void DisposeCache()
        {
            if (cachedImage != null)
            {
                AprilTagNatives.image_u8_destroy(cachedImage);
                cachedImage = null;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (Handle != null && ownsHandle)
            {
                AprilTagNatives.image_u8_destroy(Handle);
            }

            Handle = null;
            disposed = true;
        }
    }
}
