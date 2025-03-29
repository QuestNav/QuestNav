using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibJpegTurboUnity;
using QuestNav.Passthrough;
using QuestNav.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace QuestNav.CameraStreamer
{
    /// <summary>
    /// Serves the passthrough camera stream as an MJPEG stream over HTTP
    /// with optimized LibJpeg-Turbo encoding and improved threading
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

        [SerializeField]
        [Tooltip("Maximum number of encoding threads to use")]
        [Range(1, 4)]
        private int maxEncodingThreads = 2;
        
        [SerializeField]
        [Tooltip("Maximum frames to queue for encoding")]
        [Range(1, 10)]
        private int maxQueuedFrames = 3;
        
        private HttpListener httpListener;
        private Thread serverThread;
        private readonly List<HttpListenerContext> clients = new List<HttpListenerContext>();
        private readonly object clientsLock = new object();
        private byte[] currentFrameData;
        private DateTime lastFrameTime;
        private readonly object frameDataLock = new object();
        private int frameInterval;
        
        // Improved texture handling
        private RenderTexture renderTexture;
        private int frameCounter = 0;
        private int currentFrameSkip = 0;
        
        // LibJpegTurbo encoder
        private LJTCompressor jpegTurboCompressor;
        
        // Thread pool for encoding
        private SemaphoreSlim encodingSemaphore;
        private bool isServerRunning = false;
        
        // Frame queue to store pending frames to be encoded
        private readonly Queue<RenderTextureData> frameQueue = new Queue<RenderTextureData>();
        private readonly object frameQueueLock = new object();
        
        // Task management
        private CancellationTokenSource serverCancellationTokenSource;
        private int activeEncodingTasks = 0;
        private readonly object taskCountLock = new object();
        private int droppedFrameCount = 0;
        private int lastDropReport = 0;
        private DateTime serverStartTime;
        
        // Struct to hold render texture data
        private struct RenderTextureData
        {
            public byte[] PixelData;
            public int Width;
            public int Height;
            public DateTime CaptureTime;
        }
        
        void Awake()
        {
            // Initialize LibJpegTurbo compressor
            jpegTurboCompressor = new LJTCompressor();
            
            // Initialize semaphore for encoding thread control
            encodingSemaphore = new SemaphoreSlim(maxEncodingThreads, maxEncodingThreads);
            
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
            
            // Dispose LibJpegTurbo compressor
            if (jpegTurboCompressor != null)
            {
                jpegTurboCompressor.Dispose();
                jpegTurboCompressor = null;
            }
            
            // Dispose semaphore
            if (encodingSemaphore != null)
            {
                encodingSemaphore.Dispose();
                encodingSemaphore = null;
            }
        }
        
        void Update()
        {
            if (!isServerRunning || !webCamTextureManager || !webCamTextureManager.WebCamTexture) return;
            
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
            
            // Check if frame queue is too large (indicates encoding can't keep up)
            lock (frameQueueLock)
            {
                if (frameQueue.Count >= maxQueuedFrames)
                {
                    // Skip this frame to prevent backlog
                    droppedFrameCount++;
                    return;
                }
            }
            
            // Dynamically adjust JPEG quality under heavy load
            int dynamicQuality = jpegQuality;
            if (activeEncodingTasks >= maxEncodingThreads)
            {
                // Reduce quality temporarily to ease CPU load
                dynamicQuality = Math.Max(30, jpegQuality - 20);
            }
            
            // Capture frame from WebCamTexture using AsyncGPUReadback for better performance
            CaptureFrameAsync();
        }
        
        private void CaptureFrameAsync()
        {
            var webCamTexture = webCamTextureManager.WebCamTexture;
            if (!webCamTexture || !webCamTexture.isPlaying || webCamTexture.width <= 16)
                return;
                
            // Calculate scaling based on configured resolution
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
            
            // Blit (copy) the WebCamTexture to our scaled RenderTexture
            Graphics.Blit(webCamTexture, renderTexture);
            
            // Use AsyncGPUReadback for better performance
            AsyncGPUReadback.Request(renderTexture, 0, AsyncGPUReadbackCallback);
        }
        
        private void AsyncGPUReadbackCallback(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                QueuedLogger.LogError("[Passthrough MJPEG Server] Error during AsyncGPUReadback");
                return;
            }
            
            if (!isServerRunning || serverCancellationTokenSource == null || serverCancellationTokenSource.IsCancellationRequested)
                return;
                
            // Check if we have too many active encoding tasks
            lock (taskCountLock)
            {
                if (activeEncodingTasks >= maxEncodingThreads * 2)
                {
                    // Drop frame if we're overloaded
                    droppedFrameCount++;
                    
                    // Report dropped frames periodically
                    if (droppedFrameCount % 10 == 0 && droppedFrameCount != lastDropReport)
                    {
                        lastDropReport = droppedFrameCount;
                        QueuedLogger.LogWarning($"[Passthrough MJPEG Server] Dropped {droppedFrameCount} frames due to encoding backlog");
                    }
                    
                    return;
                }
            }
            
            try
            {
                // Get pixel data with minimal copying
                var pixelData = request.GetData<byte>().ToArray();
                
                // Create frame data
                var frameData = new RenderTextureData
                {
                    PixelData = pixelData,
                    Width = renderTexture.width,
                    Height = renderTexture.height,
                    CaptureTime = DateTime.Now
                };
                
                // Check frame queue size before adding
                lock (frameQueueLock)
                {
                    if (frameQueue.Count >= maxQueuedFrames)
                    {
                        // Remove oldest frame to make room
                        frameQueue.Dequeue();
                        droppedFrameCount++;
                    }
                    
                    frameQueue.Enqueue(frameData);
                }
                
                // Start a task to process the frame queue
                ProcessFrameQueueAsync();
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error processing GPU readback: {ex.Message}");
            }
        }
        
        private async void ProcessFrameQueueAsync()
        {
            // Try to acquire a semaphore slot for encoding
            if (!await encodingSemaphore.WaitAsync(0))
            {
                // If no slot is available, return (another thread will handle it)
                return;
            }
            
            // Increment active task count
            lock (taskCountLock)
            {
                activeEncodingTasks++;
            }
            
            try
            {
                // Check if server is still running
                if (!isServerRunning || serverCancellationTokenSource == null)
                    return;
                    
                // Get a local copy of the cancellation token
                CancellationToken cancellationToken = serverCancellationTokenSource.Token;
                
                // Process all frames in the queue
                while (!cancellationToken.IsCancellationRequested)
                {
                    RenderTextureData frameData;
                    
                    // Try to get a frame from the queue
                    lock (frameQueueLock)
                    {
                        if (frameQueue.Count == 0)
                            break;
                            
                        frameData = frameQueue.Dequeue();
                    }
                    
                    // Check if this frame is too old (more than 500ms old)
                    if ((DateTime.Now - frameData.CaptureTime).TotalMilliseconds > 500)
                    {
                        droppedFrameCount++;
                        continue; // Skip old frames
                    }
                    
                    // Encode the frame using LibJpegTurbo in a separate thread
                    try
                    {
                        // Use Task.Run with cancellation token and timeout
                        var encodingTask = Task.Run(() => EncodeFrameWithLibJpegTurbo(frameData), cancellationToken);
                        
                        // Wait for encoding with timeout
                        if (!await TaskExtensions.WaitAsync(encodingTask, TimeSpan.FromMilliseconds(500), cancellationToken))
                        {
                            QueuedLogger.LogWarning("[Passthrough MJPEG Server] Encoding task timed out");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Server was stopped, exit gracefully
                        break;
                    }
                    catch (Exception ex)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error in encoding task: {ex.Message}");
                        
                        // Brief pause after error to prevent CPU spinning
                        await Task.Delay(50, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Server was stopped, exit gracefully
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error processing frame queue: {ex.Message}");
            }
            finally
            {
                // Decrement active task count
                lock (taskCountLock)
                {
                    activeEncodingTasks--;
                }
                
                // Release the semaphore slot
                encodingSemaphore.Release();
            }
        }
        
        private void EncodeFrameWithLibJpegTurbo(RenderTextureData frameData)
        {
            if (!isServerRunning)
                return;
                
            try
            {
                // LibJpegTurbo encoding
                LJTPixelFormat pixelFormat = LJTPixelFormat.RGBA; // Assuming RGBA data from Unity
                
                // Dynamically adjust quality based on system load
                int dynamicQuality = jpegQuality;
                lock (taskCountLock)
                {
                    if (activeEncodingTasks > maxEncodingThreads)
                    {
                        // Reduce quality under heavy load
                        dynamicQuality = Math.Max(30, jpegQuality - 15);
                    }
                }
                
                // Encode with LibJpegTurbo
                byte[] jpegData = jpegTurboCompressor.EncodeJPG(
                    frameData.PixelData, 
                    frameData.Width, 
                    frameData.Height, 
                    pixelFormat, 
                    dynamicQuality
                );
                
                // Update current frame data
                lock (frameDataLock)
                {
                    currentFrameData = jpegData;
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Error encoding frame: {ex.Message}");
            }
            finally
            {
                // Help the garbage collector by explicitly releasing the reference
                frameData.PixelData = null;
            }
        }
        
        /// <summary>
        /// Starts the MJPEG HTTP server
        /// </summary>
        public void StartServer()
        {
            if (isServerRunning) return;
            
            try
            {
                // Create a new cancellation token source for this server session
                serverCancellationTokenSource = new CancellationTokenSource();
                
                // Clear counters
                activeEncodingTasks = 0;
                droppedFrameCount = 0;
                lastDropReport = 0;
                serverStartTime = DateTime.Now;
                
                // Reset frame queue
                lock (frameQueueLock)
                {
                    frameQueue.Clear();
                }
                
                // Reset current frame data
                lock (frameDataLock)
                {
                    currentFrameData = null;
                }
                
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{port}/");
                httpListener.Start();
                
                isServerRunning = true;
                QueuedLogger.Log($"[Passthrough MJPEG Server] MJPEG Server started on port {port}");
                
                // Start server thread
                serverThread = new Thread(ServerThreadMethod)
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.AboveNormal
                };
                serverThread.Start();
                
                // Start client connection listener
                StartCoroutine(AcceptClientConnections());
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"[Passthrough MJPEG Server] Failed to start MJPEG server: {ex.Message}");
                isServerRunning = false;
                
                // Clean up resources
                if (serverCancellationTokenSource != null)
                {
                    serverCancellationTokenSource.Cancel();
                    serverCancellationTokenSource.Dispose();
                    serverCancellationTokenSource = null;
                }
                
                if (httpListener != null)
                {
                    try
                    {
                        httpListener.Stop();
                        httpListener.Close();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                    httpListener = null;
                }
            }
        }
        
        /// <summary>
        /// Stops the MJPEG HTTP server
        /// </summary>
        public void StopServer()
        {
            if (!isServerRunning) return;
            
            isServerRunning = false;
            
            // Cancel all pending tasks
            if (serverCancellationTokenSource != null)
            {
                serverCancellationTokenSource.Cancel();
                serverCancellationTokenSource.Dispose();
                serverCancellationTokenSource = null;
            }
            
            // Wait for encoding tasks to complete (with timeout)
            int waitTime = 0;
            while (activeEncodingTasks > 0 && waitTime < 1000)
            {
                Thread.Sleep(10);
                waitTime += 10;
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
            
            // Clear frame queue
            lock (frameQueueLock)
            {
                frameQueue.Clear();
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
            
            // Stop HTTP listener
            if (httpListener != null)
            {
                try
                {
                    httpListener.Stop();
                    httpListener.Close();
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogError($"[Passthrough MJPEG Server] Error stopping HTTP listener: {ex.Message}");
                }
                httpListener = null;
            }
            
            QueuedLogger.Log("[Passthrough MJPEG Server] MJPEG Server stopped");
            
            // Force a GC collection to clean up any lingering resources
            GC.Collect();
        }
        
        /// <summary>
        /// Returns true if the server is currently running
        /// </summary>
        public bool IsRunning => isServerRunning;

        /// <summary>
        /// Toggles the server between running and stopped states
        /// </summary>
        public void ToggleServer()
        {
            if (isServerRunning)
                StopServer();
            else
                StartServer();
        }
        
        private IEnumerator AcceptClientConnections()
        {
            while (isServerRunning && serverCancellationTokenSource != null && !serverCancellationTokenSource.IsCancellationRequested)
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
                    if (isServerRunning)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error starting MJPEG BeginGetContext: {ex.Message}");
                    }
                }
                
                if (!hadError)
                {
                    // Wait for the connection without blocking the main thread
                    while (!asyncResult.IsCompleted && isServerRunning && 
                           serverCancellationTokenSource != null && !serverCancellationTokenSource.IsCancellationRequested)
                    {
                        yield return null;
                    }
                    
                    if (isServerRunning && serverCancellationTokenSource != null && !serverCancellationTokenSource.IsCancellationRequested)
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
                            if (isServerRunning)
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
                    // Check if we already have too many clients (limit to 5 concurrent clients)
                    lock (clientsLock)
                    {
                        if (clients.Count >= 5)
                        {
                            // Too many clients, send 503 Service Unavailable
                            context.Response.StatusCode = 503;
                            context.Response.StatusDescription = "Service Unavailable - Too many concurrent clients";
                            context.Response.Close();
                            QueuedLogger.LogWarning($"[Passthrough MJPEG Server] Rejected client from {context.Request.RemoteEndPoint} - too many connections");
                            return;
                        }
                        
                        // Add the client to our list for the MJPEG stream
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
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { margin: 0; padding: 0; background: #000; height: 100vh; display: flex; justify-content: center; align-items: center; }
        img { max-width: 100%; max-height: 100vh; object-fit: contain; }
        .status { color: white; font-family: Arial, sans-serif; text-align: center; position: absolute; bottom: 10px; left: 0; right: 0; }
    </style>
    <script>
        // Auto-reconnect if stream fails
        function setupStream() {
            const img = document.getElementById('stream');
            img.onerror = function() {
                document.getElementById('status').textContent = 'Stream disconnected. Reconnecting...';
                setTimeout(() => {
                    img.src = 'stream?' + new Date().getTime();
                }, 1000);
            };
            img.onload = function() {
                document.getElementById('status').textContent = 'Connected';
            };
        }
        window.onload = setupStream;
    </script>
</head>
<body>
    <img id=""stream"" src=""/stream"" alt=""MJPEG Stream"">
    <div id=""status"" class=""status"">Connecting...</div>
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
            Dictionary<HttpListenerContext, DateTime> clientLastActivity = new Dictionary<HttpListenerContext, DateTime>();
            
            // Server health monitoring
            DateTime lastHealthReport = DateTime.Now;
            int consecutiveErrorCount = 0;
            
            // Client send loop
            while (isServerRunning && serverCancellationTokenSource != null && !serverCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Server health reporting (every 5 seconds)
                    if ((DateTime.Now - lastHealthReport).TotalSeconds >= 5)
                    {
                        lastHealthReport = DateTime.Now;
                        TimeSpan uptime = DateTime.Now - serverStartTime;
                        QueuedLogger.Log($"[Passthrough MJPEG Server] Health: Up {(int)uptime.TotalMinutes}m, Clients: {clients.Count}, Dropped: {droppedFrameCount}, Tasks: {activeEncodingTasks}");
                        
                        // Check for inactive clients (30 seconds timeout)
                        List<HttpListenerContext> timeoutClients = new List<HttpListenerContext>();
                        lock (clientsLock)
                        {
                            foreach (var clientEntry in clientLastActivity)
                            {
                                if ((DateTime.Now - clientEntry.Value).TotalSeconds > 30)
                                {
                                    timeoutClients.Add(clientEntry.Key);
                                }
                            }
                            
                            // Remove timed-out clients
                            foreach (var client in timeoutClients)
                            {
                                clients.Remove(client);
                                clientHeaders.Remove(client);
                                clientLastActivity.Remove(client);
                                try { client.Response.Close(); } catch { }
                            }
                            
                            if (timeoutClients.Count > 0)
                            {
                                QueuedLogger.Log($"[Passthrough MJPEG Server] Removed {timeoutClients.Count} timed-out clients");
                            }
                        }
                        
                        // Reset consecutive error count during health check
                        consecutiveErrorCount = 0;
                    }
                    
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
                                
                                // Update client activity timestamp
                                clientLastActivity[client] = DateTime.Now;
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
                            clientLastActivity.Remove(client);
                            try { client.Response.Close(); } catch { }
                        }
                        
                        if (disconnectedClients.Count > 0)
                        {
                            QueuedLogger.Log($"[Passthrough MJPEG Server] MJPEG Clients disconnected: {disconnectedClients.Count}, remaining: {clients.Count}");
                        }
                    }
                    
                    // Reset consecutive error count on success
                    consecutiveErrorCount = 0;
                    
                    // Use adaptive sleep to control frame rate
                    int sleepTime = frameInterval - 5;
                    if (clients.Count == 0)
                    {
                        // Sleep longer if no clients
                        sleepTime = 100;
                    }
                    else if (droppedFrameCount > 0 && droppedFrameCount % 10 == 0)
                    {
                        // Increase sleep time under load
                        sleepTime += 5;
                    }
                    
                    Thread.Sleep(Math.Max(1, sleepTime));
                }
                catch (OperationCanceledException)
                {
                    // Server stopped, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveErrorCount++;
                    
                    if (isServerRunning && serverCancellationTokenSource != null && !serverCancellationTokenSource.IsCancellationRequested)
                    {
                        QueuedLogger.LogError($"[Passthrough MJPEG Server] Error in MJPEG server thread: {ex.Message}");
                        
                        // If we get many consecutive errors, restart the server
                        if (consecutiveErrorCount > 10)
                        {
                            QueuedLogger.LogError("[Passthrough MJPEG Server] Too many consecutive errors, restarting server");
                            
                            // Set a flag to restart server after this thread exits
                            isServerRunning = false;
                            break;
                        }
                    }
                    
                    // Don't spam errors
                    Thread.Sleep(1000);
                }
            }
            
            // If we exited due to too many errors, restart
            if (consecutiveErrorCount > 10)
            {
                StopServer();
                StartServer();
            }
        }
        
        void OnApplicationQuit()
        {
            StopServer();
        }
    }
    
    // Extension method for Task timeout
    public static class TaskExtensions
    {
        public static async Task<bool> WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            return completedTask == task;
        }
    }
}