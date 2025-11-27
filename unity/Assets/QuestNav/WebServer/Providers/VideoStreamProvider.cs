using System;
using System.Buffers.Text;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using Meta.XR;
using UnityEngine;

namespace QuestNav.WebServer
{
    public struct EncodedFrame
    {
        /// <summary>
        /// The frame number. Corresponds with Time.frameCount.
        /// </summary>
        public readonly int frameNumber;

        /// <summary>
        /// A JPEG encoded frame.
        /// </summary>
        public readonly byte[] frameData;

        public EncodedFrame(int frameNumber, byte[] frameData)
        {
            this.frameNumber = frameNumber;
            this.frameData = frameData;
        }
    }

    public class VideoStreamProvider
    {
        public interface IFrameSource
        {
            /// <summary>
            /// Maximum desired framerate for capture/stream pacing
            /// </summary>
            int MaxFrameRate { get; }

            /// <summary>
            /// The current frame
            /// </summary>
            EncodedFrame CurrentFrame { get; }
        }

        #region Fields
        private const string Boundary = "frame";
        private const int InitialBufferSize = 32 * 1024;
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        private static readonly byte[] HeaderStartBytes = DefaultEncoding.GetBytes(
            "\r\n--" + Boundary + "\r\n" + "Content-Type: image/jpeg\r\n" + "Content-Length: "
        );
        private static readonly byte[] HeaderEndBytes = DefaultEncoding.GetBytes("\r\n\r\n");
        private readonly IFrameSource frameSource;
        private int connectedClients;

        #endregion

        #region Properties

        private int MaxFrameRate => frameSource?.MaxFrameRate ?? 15;
        private TimeSpan FrameDelay => TimeSpan.FromSeconds(1.0f / MaxFrameRate);

        #endregion

        public VideoStreamProvider(IFrameSource frameSource)
        {
            this.frameSource = frameSource;
            Debug.Log("[VideoStreamProvider] Created");
        }

        #region Public Methods

        // Frame capture has been extracted to QuestNav.Camera.PassthroughCapture

        public async Task HandleStreamAsync(IHttpContext context)
        {
            if (frameSource is null)
            {
                context.Response.StatusCode = 503;
                context.Response.StatusDescription = "Service unavailable";
                await context.SendStringAsync(
                    "The stream is unavailable",
                    "text/plain",
                    Encoding.UTF8
                );
                return;
            }

            Interlocked.Increment(ref connectedClients);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "multipart/x-mixed-replace; boundary=--" + Boundary;
            context.Response.SendChunked = true;

            Debug.Log("[VideoStreamProvider] Starting mjpeg stream");
            using Stream responseStream = context.OpenResponseStream(preferCompression: false);

            // Create a buffer that we'll use to build the data structure for each frame
            MemoryStream memStream = new(InitialBufferSize);
            int lastFrame = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var frame = frameSource.CurrentFrame;
                if (lastFrame < frame.frameNumber)
                {
                    try
                    {
                        Stream frameStream = memStream;
                        // Reset the content of memStream
                        memStream.SetLength(0);
                        WriteFrame(frameStream, frame.frameData);

                        // Copy the buffer into the response stream
                        memStream.Position = 0;
                        memStream.CopyTo(responseStream);
                        responseStream.Flush();

                        // Don't re-send the same frame
                        lastFrame = frame.frameNumber;
                    }
                    catch
                    {
                        break;
                    }
                }

                Interlocked.Decrement(ref connectedClients);
                await Task.Delay(FrameDelay);
            }

            Debug.Log("[VideoStreamProvider] Done streaming");
        }

        private static void WriteFrame(Stream stream, byte[] jpegData)
        {
            // Use Utf8Formatter to avoid memory allocations each frame for ToString() and GetBytes()
            Span<byte> lengthBuffer = stackalloc byte[9];
            if (!Utf8Formatter.TryFormat(jpegData.Length, lengthBuffer, out int strLen))
            {
                Debug.Log("[VideoStreamProvider] Returned false");
                return;
            }

            stream.Write(HeaderStartBytes);
            // Write the string representation of the ContentLength to the stream
            stream.Write(lengthBuffer[..strLen]);
            stream.Write(HeaderEndBytes);
            stream.Write(jpegData);
            stream.Flush();
        }

        #endregion
    }
}
