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
    /// <summary>
    /// Provides access to settings for video streaming
    /// </summary>
    public interface IPassthroughOptions
    {
        /// <summary>
        /// If true, enable streaming the passthrough camera
        /// </summary>
        bool Enable { get; }

        /// <summary>
        /// The maximum frame rate for the passthrough camera
        /// </summary>
        int MaxFrameRate { get; }
    }

    internal class EncodedFrame
    {
        public readonly int FrameNumber;
        public readonly byte[] FrameData;

        public EncodedFrame(int frameNumber, byte[] frameData)
        {
            FrameNumber = frameNumber;
            FrameData = frameData;
        }
    }

    public class VideoStreamProvider
    {
        #region Fields
        private const string Boundary = "frame";
        private const int InitialBufferSize = 32 * 1024;
        private static readonly Encoding DefaultEncoding = Encoding.ASCII;

        private static readonly byte[] HeaderStartBytes = DefaultEncoding.GetBytes(
            "\r\n--" + Boundary + "\r\n" + "Content-Type: image/jpeg\r\n" + "Content-Length: "
        );
        private static readonly byte[] HeaderEndBytes = DefaultEncoding.GetBytes("\r\n\r\n");

        private readonly PassthroughCameraAccess cameraAccess;
        private readonly IPassthroughOptions options;
        private EncodedFrame currentFrame;
        private int connectedClients;

        public VideoStreamProvider(
            PassthroughCameraAccess cameraAccess,
            IPassthroughOptions options
        )
        {
            this.cameraAccess = cameraAccess;
            this.options = options;
            Debug.Log("[VideoStreamProvider] Created");
        }

        #endregion

        #region Properties

        private float FrameDelaySeconds => 1.0f / options.MaxFrameRate;
        private TimeSpan FrameDelay => TimeSpan.FromSeconds(FrameDelaySeconds);

        #endregion

        #region Public Methods

        public IEnumerator FrameCaptureCoroutine()
        {
            if (cameraAccess is null)
            {
                Debug.Log("[VideoStreamProvider] Disabled - cameraAccess is unset");
                yield break;
            }

            Debug.Log("[VideoStreamProvider] Initialized");

            while (true)
            {
                if (!options.Enable)
                {
                    Debug.Log("[VideoStreamProvider] Disabled");
                    yield return new WaitUntil(() => options.Enable);
                    Debug.Log("[VideoStreamProvider] Enabled");
                }

                if (!cameraAccess.enabled)
                {
                    yield return new WaitForSeconds(FrameDelaySeconds);
                    continue;
                }

                try
                {
                    var texture = cameraAccess.GetTexture();
                    if (texture is not Texture2D texture2D)
                    {
                        Debug.LogError(
                            $"[VideoStreamProvider] GetTexture returned an incompatible object ({texture.GetType().Name})"
                        );
                        yield break;
                    }

                    currentFrame = new EncodedFrame(Time.frameCount, texture2D.EncodeToJPG());
                }
                catch (NullReferenceException ex)
                {
                    // This probably means the app hasn't been given permission to access the headset camera.
                    // If we inject StatusProvider we can add an error on the dashboard for this.
                    Debug.LogError(
                        $"[VideoStreamProvider] Error capturing frame - verify 'Headset Cameras' app permission is enabled. {ex.Message}"
                    );
                    yield break;
                }

                yield return new WaitForSeconds(FrameDelaySeconds);
            }
        }

        public async Task HandleStreamAsync(IHttpContext context)
        {
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
                var frame = currentFrame;
                if (lastFrame < frame.FrameNumber)
                {
                    try
                    {
                        Stream frameStream = memStream;
                        // Reset the content of memStream
                        memStream.SetLength(0);
                        WriteFrame(frameStream, frame.FrameData);

                        // Copy the buffer into the response stream
                        memStream.Position = 0;
                        memStream.CopyTo(responseStream);
                        responseStream.Flush();

                        // Don't re-send the same frame
                        lastFrame = frame.FrameNumber;
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
