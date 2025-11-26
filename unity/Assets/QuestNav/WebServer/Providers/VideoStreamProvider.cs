using System;
using System.Buffers.Text;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using Meta.XR;
using UnityEngine;

namespace QuestNav.WebServer
{
    public class VideoStreamProvider : MonoBehaviour
    {
        #region Fields

        private const int MaxFrameRate = 30;
        private static readonly TimeSpan FrameDelay = TimeSpan.FromSeconds(1.0f / MaxFrameRate);
        private const string Boundary = "frame";
        private const int InitialBufferSize = 32 * 1024;
        private static readonly Encoding DefaultEncoding = Encoding.ASCII;

        private static readonly byte[] HeaderStartBytes = DefaultEncoding.GetBytes(
            "\r\n--" + Boundary + "\r\n" + "Content-Type: image/jpeg\r\n" + "Content-Length: "
        );
        private static readonly byte[] HeaderEndBytes = DefaultEncoding.GetBytes("\r\n\r\n");

        [SerializeField]
        private PassthroughCameraAccess cameraAccess;

        private int frameNumber = -1;
        private byte[] jpegData;
        private int connectedClients;
        #endregion

        #region Static Instance
        private static VideoStreamProvider instance;

        public static VideoStreamProvider Instance
        {
            get
            {
                if (instance is null)
                {
                    Debug.LogError("Instance is null - attempting to create it");
                    var go = new GameObject(nameof(VideoStreamProvider));
                    instance = go.AddComponent<VideoStreamProvider>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Initializes the status provider and ensures singleton pattern.
        /// </summary>
        private void Awake()
        {
            Debug.Log("[VideoStreamProvider] Awake");
            if (instance != null && instance != this)
            {
                Debug.Log(
                    "[VideoStreamProvider] Instance already exists - requesting Destroy of this gameObject"
                );
                Destroy(gameObject);
                return;
            }

            Debug.Log("[VideoStreamProvider] Setting instance");
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (cameraAccess is not null)
            {
                Debug.Log("[VideoStreamProvider] Using existing cameraAccess");
                return;
            }

            Debug.Log("[VideoStreamProvider] Constructing cameraAccess");
            cameraAccess = gameObject.AddComponent<PassthroughCameraAccess>();
            cameraAccess.CameraPosition = PassthroughCameraAccess.CameraPositionType.Left;
            cameraAccess.RequestedResolution = new Vector2Int(1280, 960);
        }

        /// <summary>
        /// Sends the current frame to any connected clients
        /// </summary>
        void Update()
        {
            if (connectedClients == 0)
            {
                return;
            }

            if (this.cameraAccess is not { enabled: true } cameraAccess)
            {
                Debug.LogError("[VideoStreamProvider] CameraAccess is unset");
                return;
            }

            try
            {
                Texture texture = cameraAccess.GetTexture();
                if (texture is not Texture2D texture2D)
                {
                    Debug.Log(
                        $"[VideoStreamProvider] GetTexture returned an incompatible object ({texture.GetType().Name})"
                    );
                    return;
                }

                jpegData = texture2D.EncodeToJPG();
                frameNumber = frameNumber + 1;
            }
            catch (NullReferenceException)
            {
                // This probably means the app hasn't been given permission to access the headset camera.
                // If we inject StatusProvider we can add an error on the dashboard for this.
            }
        }
        #endregion

        #region Public Methods

        public async Task HandleMjpegAsync(IHttpContext context)
        {
            Interlocked.Increment(ref connectedClients);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "multipart/x-mixed-replace; boundary=--" + Boundary;
            context.Response.SendChunked = true;

            Debug.Log("[VideoStreamProvider] Starting mjpeg stream");
            using Stream responseStream = context.OpenResponseStream(preferCompression: false);
            MemoryStream memStream = new(InitialBufferSize);
            int lastFrame = -1;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var (availableFrame, frameData) = (frameNumber, jpegData);
                if (lastFrame < availableFrame)
                {
                    try
                    {
                        Stream frameStream = memStream;
                        // Reset the content of memStream
                        memStream.SetLength(0);
                        Debug.Log(
                            $"[VideoStreamProvider] Sending frame {availableFrame} ({frameData.Length} bytes)"
                        );
                        WriteFrame(frameStream, frameData);

                        // Copy the buffer into the response stream
                        memStream.Position = 0;
                        memStream.CopyTo(responseStream);
                        responseStream.Flush();

                        // Don't re-send the same frame
                        lastFrame = availableFrame;
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
            /*
    header.clear();
    oss << "\r\n--" BOUNDARY "\r\n" << "Content-Type: image/jpeg\r\n";
    wpi::print(oss, "Content-Length: {}\r\n", size);
    wpi::print(oss, "X-Timestamp: {}\r\n", timestamp);
    oss << "\r\n";
    os << oss.str();
    if (addDHT) {
      // Insert DHT data immediately before SOF
      os << std::string_view(data, locSOF);
      os << JpegGetDHT();
      os << std::string_view(data + locSOF, image->size() - locSOF);
    } else {
      os << std::string_view(data, size);
    }
    // os.flush();
             */
        }

        #endregion
    }
}
