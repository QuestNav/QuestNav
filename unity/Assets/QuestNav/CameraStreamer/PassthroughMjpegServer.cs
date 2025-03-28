using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using QuestNav.Passthrough;
using QuestNav.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace QuestNav.CameraStreamer
{
    /// <summary>
    /// Serves the passthrough camera stream as an MJPEG stream over HTTP
    /// </summary>
    public class PassthroughMjpegServer : MonoBehaviour
    {
        [SerializeField] 
        [Tooltip("Reference to the WebCamTextureManager component that provides the camera texture")]
        private WebCamTextureManager webCamTextureManager;
        
        [SerializeField]
        [Tooltip("The HTTP port to use for the MJPEG server")]
        private int port = 5809;
        
        [SerializeField]
        [Tooltip("JPEG compression quality (1-100), higher values produce better quality but larger files")]
        [Range(1, 100)]
        private int jpegQuality = 75;
        
        [SerializeField]
        [Tooltip("Target frame rate for the MJPEG stream")]
        [Range(1, 60)]
        private int frameRate = 30;
        
        [SerializeField]
        [Tooltip("Whether to automatically start the server when the component awakens")]
        private bool startServerOnAwake = true;
        
        [SerializeField] 
        [Tooltip("Stream resolution (width, height) - set to (0,0) for highest possible resolution")]
        private Vector2 streamResolution = new Vector2(640, 480);
        
        [SerializeField]
        [Tooltip("Dynamic framerate reduction under heavy load (0 = disabled, 1-3 = increasing reduction)")]
        [Range(0, 3)]
        private int performanceMode = 0;
        
        [SerializeField] 
        [Tooltip("Time to wait between client connection attempts in milliseconds")]
        [Range(10, 1000)]
        private int clientConnectionAttemptIntervalMs = 100;
        
        private HttpListener httpListener;
        private Thread serverThread;
        private Thread encodingThread;
        private readonly List<HttpListenerContext> clients = new List<HttpListenerContext>();
        private readonly object clientsLock = new object();
        private byte[] currentFrameData;
        private DateTime lastFrameTime;
        private readonly object frameDataLock = new object();
        private int frameInterval;
        private bool isEncodingThreadRunning = false;
        
        // Improved texture handling
        private RenderTexture renderTexture;
        private Texture2D readTexture;
        private int frameCounter = 0;
        private int currentFrameSkip = 0;
        
        void Awake()
        {
            if (startServerOnAwake)
            {
                StartServer();
            }
            
            // Calculate frame interval in milliseconds
            frameInterval = 1000 / frameRate;
        }
        
        void OnDestroy()
        {
            StopServer();
        }
        
        void Update()
        {
            if (!IsRunning || !webCamTextureManager || !webCamTextureManager.WebCamTexture) return;
            
            // Only capture frames if there are clients
            if (clients.Count == 0)
                return;
                
            // Only capture frames at the specified frame rate
            if ((DateTime.Now - lastFrameTime).TotalMilliseconds < frameInterval)
                return;
            
            // Determine frame skip based on performance mode
            if (performanceMode > 0)
            {
                frameCounter++;
                currentFrameSkip = performanceMode;
                if (frameCounter % (currentFrameSkip + 1) != 0)
                    return;
            }
            
            lastFrameTime = DateTime.Now;
            
            // Capture frame from WebCamTexture
            CaptureFrame();
        }
        
        // We'll encode directly on the main thread now, and use a separate thread just for sending frames
        private void StartEncodingThread()
        {
            isEncodingThreadRunning = true;
            encodingThread = new Thread(() => 
            {
                while (isEncodingThreadRunning)
                {
                    // This thread only handles sending data to clients, not encoding
                    // Sleep to maintain frame rate without hogging CPU
                    Thread.Sleep(Math.Max(1, frameInterval - 5));
                }
            });
            encodingThread.IsBackground = true;
            encodingThread.Start();
        }
        
        private void CaptureFrame()
        {
            var webCamTexture = webCamTextureManager.WebCamTexture;
            if (!webCamTexture || !webCamTexture.isPlaying || webCamTexture.width <= 16)
                return;
                
            // Calculate scaling based on configured resolution
            // If Vector2 is (0,0), use the full resolution of the webcam texture
            int captureWidth = (int)streamResolution.x;
            int captureHeight = (int)streamResolution.y;
            
            if (captureWidth <= 0 || captureHeight <= 0)
            {
                captureWidth = webCamTexture.width;
                captureHeight = webCamTexture.height;
            }
            
            // Create or resize render texture if needed
            if (renderTexture == null || renderTexture.width != captureWidth || renderTexture.height != captureHeight)
            {
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                    
                renderTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 1;
            }
            
            // Create or resize read texture if needed
            if (readTexture == null || readTexture.width != captureWidth || readTexture.height != captureHeight)
            {
                if (readTexture != null)
                    Destroy(readTexture);
                    
                readTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }
            
            // Blit (copy) the WebCamTexture to our scaled RenderTexture
            Graphics.Blit(webCamTexture, renderTexture);
            
            // Read pixels from the render texture
            RenderTexture.active = renderTexture;
            readTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            readTexture.Apply();
            RenderTexture.active = null;
            
            // Encode JPEG directly on the main thread
            byte[] jpegBytes = readTexture.EncodeToJPG(jpegQuality);
            
            // Update current frame data
            lock (frameDataLock)
            {
                currentFrameData = jpegBytes;
            }
        }
        
        /// <summary>
        /// Starts the MJPEG HTTP server
        /// </summary>
        public void StartServer()
        {
            if (IsRunning) return;
            
            try
            {
                // Start encoding thread
                if (encodingThread == null || !encodingThread.IsAlive)
                {
                    StartEncodingThread();
                }
                
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{port}/");
                httpListener.Start();
                
                IsRunning = true;
                QueuedLogger.Log($"[Passthrough MJPEG Server] MJPEG Server started on port {port}");
                
                // Start server thread
                serverThread = new Thread(ServerThreadMethod)
                {
                    IsBackground = true
                };
                serverThread.Start();
                
                // Start client connection listener
                StartCoroutine(AcceptClientConnections());
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Failed to start MJPEG server: {ex.Message}");
                IsRunning = false;
            }
        }
        
        /// <summary>
        /// Stops the MJPEG HTTP server
        /// </summary>
        public void StopServer()
        {
            if (!IsRunning) return;
            
            IsRunning = false;
            
            // Stop encoding thread
            isEncodingThreadRunning = false;
            if (encodingThread != null && encodingThread.IsAlive)
            {
                try
                {
                    encodingThread.Join(1000); // Wait up to 1 second for thread to exit
                    if (encodingThread.IsAlive)
                    {
                        // If still alive, abort it
                        encodingThread.Abort();
                    }
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogError($"[Passthrough MJPEG Server] Error stopping encoding thread: {ex.Message}");
                }
            }
            
            // Close all client connections
            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    try
                    {
                        client.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error closing MJPEG client connection: {ex.Message}");
                    }
                }
                clients.Clear();
            }
            
            // Clear current frame data
            lock (frameDataLock)
            {
                currentFrameData = null;
            }
            
            // Release textures
            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                renderTexture = null;
            }
            
            if (readTexture != null)
            {
                Destroy(readTexture);
                readTexture = null;
            }
            
            // Stop HTTP listener
            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
                httpListener = null;
            }
            
            QueuedLogger.Log("[Passthrough MJPEG Server] MJPEG Server stopped");
        }
        
        /// <summary>
        /// Returns true if the server is currently running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Toggles the server between running and stopped states
        /// </summary>
        public void ToggleServer()
        {
            if (IsRunning)
                StopServer();
            else
                StartServer();
        }
        
        private IEnumerator AcceptClientConnections()
        {
            while (IsRunning)
            {
                var hadError = false;
                IAsyncResult asyncResult = null;
                
                try
                {
                    // Get the next client connection asynchronously
                    asyncResult = httpListener.BeginGetContext(null, null);
                }
                catch (Exception ex)
                {
                    hadError = true;
                    if (IsRunning)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error starting MJPEG BeginGetContext: {ex.Message}");
                    }
                }
                
                if (!hadError)
                {
                    // Wait for the connection without blocking the main thread
                    while (!asyncResult.IsCompleted && IsRunning)
                    {
                        yield return null;
                    }
                    
                    if (IsRunning)
                    {
                        try
                        {
                            // Get the context
                            var context = httpListener.EndGetContext(asyncResult);
                            
                            // Process the connection
                            ProcessNewConnection(context);
                        }
                        catch (Exception ex)
                        {
                            if (IsRunning)
                            {
                                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error accepting MJPEG client connection: {ex.Message}");
                            }
                            hadError = true;
                        }
                    }
                }
                
                // If we had an error, wait a bit before trying again
                if (hadError)
                {
                    yield return new WaitForSeconds(clientConnectionAttemptIntervalMs / 1000f);
                }
            }
        }
        
        private void ProcessNewConnection(HttpListenerContext context)
        {
            try
            {
                string url = context.Request.Url.LocalPath.ToLower();
                
                if (url == "/stream")
                {
                    // Add the client to our list for the MJPEG stream
                    lock (clientsLock)
                    {
                        clients.Add(context);
                    }
                    
                    QueuedLogger.Log($"[Passthrough MJPEG Server] New MJPEG client connected from {context.Request.RemoteEndPoint}, total clients: {clients.Count}");
                }
                else
                {
                    // Serve a simple HTML page with an MJPEG viewer
                    ServeHtmlPage(context);
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error processing MJPEG connection: {ex.Message}");
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors on cleanup
                }
            }
        }
        
        private void ServeHtmlPage(HttpListenerContext context)
        {
            try
            {
                string html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Passthrough Camera Stream</title>
    <style>
        body { margin: 0; padding: 0; background: #000; height: 100vh; display: flex; justify-content: center; align-items: center; }
        img { max-width: 100%; max-height: 100vh; object-fit: contain; }
    </style>
</head>
<body>
    <img src='/stream' alt='MJPEG Stream'>
</body>
</html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error serving MJPEG HTML page: {ex.Message}");
            }
        }
        
        private void ServerThreadMethod()
        {
            byte[] headerBytes = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: multipart/x-mixed-replace; boundary=--boundary\r\n\r\n");
            
            string frameHeaderTemplate = 
                "--boundary\r\n" +
                "Content-Type: image/jpeg\r\n" +
                "Content-Length: {0}\r\n\r\n";
            
            byte[] newLineBytes = new byte[] { 13, 10, 13, 10 };
            Dictionary<HttpListenerContext, bool> clientHeaders = new Dictionary<HttpListenerContext, bool>();
            
            // Client send loop
            while (IsRunning)
            {
                try
                {
                    // Get current frame
                    byte[] frameJpeg;
                    lock (frameDataLock)
                    {
                        frameJpeg = currentFrameData;
                    }
                    
                    if (frameJpeg == null || frameJpeg.Length == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    
                    // Create header for this frame
                    string frameHeader = string.Format(frameHeaderTemplate, frameJpeg.Length);
                    byte[] frameHeaderBytes = Encoding.ASCII.GetBytes(frameHeader);
                    
                    // Send to all clients
                    List<HttpListenerContext> disconnectedClients = new List<HttpListenerContext>();
                    
                    lock (clientsLock)
                    {
                        foreach (var client in clients)
                        {
                            try
                            {
                                var outputStream = client.Response.OutputStream;
                                
                                // For new clients, send the HTTP header first
                                bool headerSent;
                                if (!clientHeaders.TryGetValue(client, out headerSent) || !headerSent)
                                {
                                    client.Response.ContentType = "multipart/x-mixed-replace; boundary=--boundary";
                                    outputStream.Write(headerBytes, 0, headerBytes.Length);
                                    outputStream.Flush();
                                    clientHeaders[client] = true;
                                }
                                
                                // Send frame header
                                outputStream.Write(frameHeaderBytes, 0, frameHeaderBytes.Length);
                                
                                // Send frame data
                                outputStream.Write(frameJpeg, 0, frameJpeg.Length);
                                
                                // End frame with 2 newlines
                                outputStream.Write(newLineBytes, 0, 4);
                                outputStream.Flush();
                            }
                            catch (Exception)
                            {
                                // Client disconnected, add to clean up list
                                disconnectedClients.Add(client);
                            }
                        }
                        
                        // Remove disconnected clients
                        foreach (var client in disconnectedClients)
                        {
                            clients.Remove(client);
                            clientHeaders.Remove(client);
                            try
                            {
                                client.Response.Close();
                            }
                            catch
                            {
                                // Ignore errors on cleanup
                            }
                        }
                        
                        if (disconnectedClients.Count > 0)
                        {
                            QueuedLogger.Log($"[Passthrough MJPEG Server] MJPEG Clients disconnected: {disconnectedClients.Count}, remaining: {clients.Count}");
                        }
                    }
                    
                    // Sleep to maintain frame rate and prevent CPU hogging
                    Thread.Sleep(Math.Max(1, frameInterval - 5));
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error in MJPEG server thread: {ex.Message}");
                    }
                    
                    // Don't spam errors
                    Thread.Sleep(1000);
                }
            }
        }
        
        void OnApplicationQuit()
        {
            StopServer();
        }
    }
}