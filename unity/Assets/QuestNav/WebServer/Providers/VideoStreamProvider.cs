using System;
using System.Buffers.Text;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// A JPEG-encoded video frame with frame number.
    /// </summary>
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

        /// <summary>
        /// Creates a new encoded frame.
        /// </summary>
        /// <param name="frameNumber">Unity frame count.</param>
        /// <param name="frameData">JPEG encoded image data.</param>
        public EncodedFrame(int frameNumber, byte[] frameData)
        {
            this.frameNumber = frameNumber;
            this.frameData = frameData;
        }
    }

    /// <summary>
    /// Provides MJPEG video streaming over HTTP.
    /// </summary>
    public class VideoStreamProvider
    {
        /// <summary>
        /// Interface for frame sources that provide encoded video frames.
        /// </summary>
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

            /// <summary>
            /// Gets the available video modes.
            /// </summary>
            /// <returns>Array of available video modes, or null if not initialized</returns>
            Network.VideoMode[] GetAvailableModes();

            /// <summary>
            /// Called when stream starts with requested video parameters from query string.
            /// </summary>
            /// <param name="width">Requested width, or null if not specified</param>
            /// <param name="height">Requested height, or null if not specified</param>
            /// <param name="fps">Requested FPS, or null if not specified</param>
            /// <param name="compression">Requested compression quality (1-100), or null if not specified</param>
            void SetModeAndCompression(int? width, int? height, int? fps, int? compression);
        }

        #region Fields

        /// <summary>
        /// MIME boundary string for multipart responses.
        /// </summary>
        private const string Boundary = "frame";

        /// <summary>
        /// Initial buffer size for frame data.
        /// </summary>
        private const int InitialBufferSize = 32 * 1024;

        /// <summary>
        /// UTF-8 encoding for header strings.
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Pre-encoded header start bytes for MJPEG frames.
        /// </summary>
        private static readonly byte[] HeaderStartBytes = DefaultEncoding.GetBytes(
            "\r\n--" + Boundary + "\r\n" + "Content-Type: image/jpeg\r\n" + "Content-Length: "
        );

        /// <summary>
        /// Pre-encoded header end bytes.
        /// </summary>
        private static readonly byte[] HeaderEndBytes = DefaultEncoding.GetBytes("\r\n\r\n");

        /// <summary>
        /// Source providing encoded video frames.
        /// </summary>
        private readonly IFrameSource frameSource;

        /// <summary>
        /// Count of currently connected streaming clients.
        /// </summary>
        private int connectedClients;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the frame source for accessing video properties.
        /// </summary>
        public IFrameSource FrameSource => frameSource;

        /// <summary>
        /// Maximum framerate from the frame source, defaults to 15 fps.
        /// </summary>
        private int MaxFrameRate => frameSource?.MaxFrameRate ?? 15;

        /// <summary>
        /// Delay between frames based on max framerate.
        /// </summary>
        private TimeSpan FrameDelay => TimeSpan.FromSeconds(1.0f / MaxFrameRate);

        #endregion

        /// <summary>
        /// Creates a new video stream provider.
        /// </summary>
        /// <param name="frameSource">Source providing encoded frames.</param>
        public VideoStreamProvider(IFrameSource frameSource)
        {
            this.frameSource = frameSource;
            Debug.Log("[VideoStreamProvider] Created");
        }

        #region Public Methods

        /// <summary>
        /// Handles an HTTP request by streaming MJPEG frames.
        /// </summary>
        /// <param name="context">The HTTP context to stream to.</param>
        /// <returns>A task that completes when streaming ends.</returns>
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

            // Parse compression quality from query string
            int? compression = null;
            if (int.TryParse(context.Request.QueryString["compression"], out int parsed))
            {
                compression = Math.Clamp(parsed, 1, 100);
            }

            // Parse resolution and FPS from query string
            int? width = null;
            int? height = null;

            // Parse resolution in format "320x240"
            if (context.Request.QueryString["resolution"] != null)
            {
                string resolutionStr = context.Request.QueryString["resolution"];
                string[] parts = resolutionStr.Split('x', 'X');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int parsedWidth))
                    {
                        width = parsedWidth;
                    }
                    if (int.TryParse(parts[1], out int parsedHeight))
                    {
                        height = parsedHeight;
                    }
                }
            }

            int? fps = null;
            if (int.TryParse(context.Request.QueryString["fps"], out int parsedFps))
            {
                fps = parsedFps;
            }

            // Notify frame source of stream start with requested parameters
            frameSource.SetModeAndCompression(width, height, fps, compression);

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

        /// <summary>
        /// Writes a single MJPEG frame to the stream.
        /// </summary>
        /// <param name="stream">Output stream.</param>
        /// <param name="jpegData">JPEG encoded image data.</param>
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
