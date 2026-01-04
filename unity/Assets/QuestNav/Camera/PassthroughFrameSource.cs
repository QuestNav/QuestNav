using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR;
using QuestNav.Config;
using QuestNav.Network;
using QuestNav.Utils;
using QuestNav.WebServer;
using UnityEngine;

namespace QuestNav.Camera
{
    /// <summary>
    /// Handles capturing frames from PassthroughCameraAccess and encoding them for streaming.
    /// Extracted from VideoStreamProvider.
    /// </summary>
    public class PassthroughFrameSource : VideoStreamProvider.IFrameSource
    {
        /// <summary>
        /// Available FPS options for video streaming.
        /// </summary>
        private static readonly int[] FpsOptions = { 1, 5, 15, 24, 30, 48, 60 };

        /// <summary>
        /// MonoBehaviour host for running coroutines.
        /// </summary>
        private readonly MonoBehaviour coroutineHost;

        /// <summary>
        /// Meta SDK passthrough camera accessor.
        /// </summary>
        private readonly PassthroughCameraAccess cameraAccess;

        /// <summary>
        /// NetworkTables camera source for publishing stream info.
        /// </summary>
        private readonly INtCameraSource cameraSource;

        /// <summary>
        /// Configuration manager for settings.
        /// </summary>
        private readonly IConfigManager configManager;

        /// <summary>
        /// Main thread synchronization context for marshalling Unity API calls.
        /// </summary>
        private readonly SynchronizationContext mainThreadContext;

        /// <summary>
        /// Whether the frame source has been initialized.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Cached base URL for stream endpoints.
        /// </summary>
        private string baseUrl;

        /// <summary>
        /// JPEG compression quality (1-100). Higher values mean better quality and larger files.
        /// </summary>
        private int compressionQuality = 75;

        /// <summary>
        /// Maximum desired framerate for capture/stream pacing
        /// </summary>
        public int MaxFrameRate => cameraSource.Mode.Fps;

        /// <summary>
        /// The current frame
        /// </summary>
        public EncodedFrame CurrentFrame { get; private set; }

        /// <summary>
        /// The base URL for the web server.
        /// </summary>
        public string BaseUrl
        {
            get => baseUrl;
            set
            {
                if (string.Equals(baseUrl, value))
                {
                    return;
                }

                baseUrl = value;
                cameraSource.Streams = string.IsNullOrEmpty(baseUrl)
                    ? Array.Empty<string>()
                    : new[] { $"mjpg:{BaseUrl}/video" };
            }
        }

        /// <summary>
        /// Delay between frame captures in seconds.
        /// </summary>
        private float FrameDelaySeconds => 1.0f / Math.Max(1, MaxFrameRate);

        /// <summary>
        /// Reference to the running frame capture coroutine.
        /// </summary>
        private Coroutine frameCaptureCoroutine;

        /// <summary>
        /// Changes the video mode and compression quality based on requested parameters. The best matching mode that
        /// meets or exceeds the requested parameters will be selected.
        /// </summary>
        /// <param name="width">Requested width, or null if not specified</param>
        /// <param name="height">Requested height, or null if not specified</param>
        /// <param name="fps">Requested FPS, or null if not specified</param>
        /// <param name="compression">Requested compression quality (1-100), or null if not specified</param>
        public void SetModeAndCompression(int? width, int? height, int? fps, int? compression)
        {
            // Save the config, which will trigger the change event handler to apply the changes to the stream
            _ = PersistModeAndCompression(width, height, fps, compression);
        }

        /// <summary>
        /// Applies video mode and compression quality changes but does not persist to config.
        /// </summary>
        /// <param name="width">Requested width, or null if not specified</param>
        /// <param name="height">Requested height, or null if not specified</param>
        /// <param name="fps">Requested FPS, or null if not specified</param>
        /// <param name="compression">Requested compression quality (1-100), or null if not specified</param>
        private void ApplyModeAndCompression(int? width, int? height, int? fps, int? compression)
        {
            // Apply compression if specified
            if (compression.HasValue && compressionQuality != compression.Value)
            {
                compressionQuality = Math.Clamp(compression.Value, 1, 100);
                QueuedLogger.Log(
                    $"[PassthroughFrameSource] Switching compression quality: {compressionQuality}"
                );
            }

            if (cameraSource?.Modes == null || cameraSource.Modes.Length == 0)
            {
                QueuedLogger.LogWarning("[PassthroughFrameSource] No video modes available");
                return;
            }

            // If no mode parameters specified, keep current mode
            if (!width.HasValue && !height.HasValue && !fps.HasValue)
            {
                return;
            }

            // Find the best matching mode
            VideoMode? bestMatch = FindBestMatchingModeOrBetter(width, height, fps);

            if (bestMatch.HasValue && !cameraSource.Mode.Equals(bestMatch.Value))
            {
                QueuedLogger.Log($"[PassthroughFrameSource] Switching to mode: {bestMatch.Value}");
                cameraSource.Mode = bestMatch.Value;
            }
        }

        /// <summary>
        /// Persists the current video mode and compression quality to config.
        /// </summary>
        /// <param name="width">Requested width, or null if not specified</param>
        /// <param name="height">Requested height, or null if not specified</param>
        /// <param name="fps">Requested FPS, or null if not specified</param>
        /// <param name="compression">Requested compression quality (1-100), or null if not specified</param>
        private async Task PersistModeAndCompression(
            int? width,
            int? height,
            int? fps,
            int? compression
        )
        {
            // Persist mode if any mode parameters were specified
            if (width.HasValue || height.HasValue || fps.HasValue || compression.HasValue)
            {
                // Use the compression quality if it was specified, otherwise use the current quality
                var quality = compression.HasValue
                    ? Math.Clamp(compression.Value, 1, 100)
                    : compressionQuality;

                // Find the best matching mode, or better
                var bestMatch = FindBestMatchingModeOrBetter(width, height, fps);
                if (bestMatch.HasValue)
                {
                    await configManager.SetStreamModeAsync(
                        new StreamMode(
                            bestMatch.Value.Width,
                            bestMatch.Value.Height,
                            bestMatch.Value.Fps,
                            quality
                        )
                    );
                }
                else
                {
                    // There was no suitable mode found, keep the current mode
                    var currentMode = cameraSource.Mode;
                    await configManager.SetStreamModeAsync(
                        new StreamMode(
                            currentMode.Width,
                            currentMode.Height,
                            currentMode.Fps,
                            quality
                        )
                    );
                }
            }
        }

        /// <summary>
        /// Gets the available video modes.
        /// </summary>
        /// <returns>Array of available video modes.</returns>
        public VideoMode[] GetAvailableModes()
        {
            return cameraSource?.Modes ?? Array.Empty<VideoMode>();
        }

        /// <summary>
        /// Finds the best matching video mode that meets or exceeds requested parameters.
        /// Prefers modes that exactly match or minimally exceed the requested values.
        /// </summary>
        /// <param name="width">Minimum requested width, or null</param>
        /// <param name="height">Minimum requested height, or null</param>
        /// <param name="fps">Minimum requested FPS, or null</param>
        /// <returns>Best matching mode that meets or exceeds requirements, or null if no suitable mode found</returns>
        private VideoMode? FindBestMatchingModeOrBetter(int? width, int? height, int? fps)
        {
            VideoMode? exactMatch = null;
            VideoMode? bestMatch = null;
            int bestExcess = int.MaxValue; // Track how much we exceed the request

            foreach (var mode in cameraSource.Modes)
            {
                bool widthTooSmall = width.HasValue && mode.Width < width.Value;
                bool heightTooSmall = height.HasValue && mode.Height < height.Value;
                bool fpsTooSmall = fps.HasValue && mode.Fps < fps.Value;
                if (widthTooSmall || heightTooSmall || fpsTooSmall)
                {
                    // Skip modes that don't meet minimum requirements
                    continue;
                }

                // Check for exact match
                bool widthExact = !width.HasValue || mode.Width == width.Value;
                bool heightExact = !height.HasValue || mode.Height == height.Value;
                bool fpsExact = !fps.HasValue || mode.Fps == fps.Value;

                if (widthExact && heightExact && fpsExact)
                {
                    exactMatch = mode;
                    break;
                }

                // Calculate how much this mode exceeds the request
                // Lower excess is better (closer to what was requested)
                int widthExcess = width.HasValue ? mode.Width - width.Value : 0;
                int heightExcess = height.HasValue ? mode.Height - height.Value : 0;
                int fpsExcess = fps.HasValue ? mode.Fps - fps.Value : 0;

                // Total excess score (pixel count difference + weighted (100X) FPS difference)
                int totalExcess = (widthExcess * heightExcess) + (fpsExcess * 100);

                if (totalExcess < bestExcess)
                {
                    bestExcess = totalExcess;
                    bestMatch = mode;
                }
            }

            return exactMatch ?? bestMatch;
        }

        /// <summary>
        /// Creates a new PassthroughFrameSource.
        /// </summary>
        /// <param name="coroutineHost">MonoBehaviour for coroutine execution</param>
        /// <param name="cameraAccess">Provides access to the PassthroughCamera through Meta's SDK</param>
        /// <param name="cameraSource">The network table source that will expose this camera stream</param>
        /// <param name="configManager">The config manager to update/querry config values</param>
        public PassthroughFrameSource(
            MonoBehaviour coroutineHost,
            PassthroughCameraAccess cameraAccess,
            INtCameraSource cameraSource,
            IConfigManager configManager
        )
        {
            this.coroutineHost = coroutineHost;
            this.cameraAccess = cameraAccess;
            this.cameraSource = cameraSource;
            this.configManager = configManager;

            // Capture main thread context for marshalling Unity API calls
            mainThreadContext = SynchronizationContext.Current;

            // Attach to ConfigManager callbacks
            configManager.OnEnablePassthroughStreamChanged += OnEnablePassthroughStreamChanged;
            configManager.OnStreamModeChanged += OnStreamModeChanged;
        }

        /// <summary>
        /// Handles video mode changes by updating the camera resolution.
        /// </summary>
        /// <param name="mode">The new video mode.</param>
        private void OnSelectedModeChanged(VideoMode mode)
        {
            // Callbacks likely run on background thread - marshal to main thread
            InvokeOnMainThread(() =>
            {
                cameraAccess.enabled = false;
                cameraAccess.RequestedResolution = new Vector2Int(mode.Width, mode.Height);
                cameraAccess.enabled = true;
                QueuedLogger.Log($"Changed mode: {mode}");
            });
        }

        /// <summary>
        /// Handles stream mode configuration changes.
        /// </summary>
        /// <param name="streamMode">The new stream mode configuration.</param>
        private void OnStreamModeChanged(StreamMode streamMode)
        {
            if (!isInitialized)
            {
                return;
            }

            InvokeOnMainThread(() =>
            {
                QueuedLogger.Log($"[PassthroughFrameSource] Stream mode changed to {streamMode}");
                // Apply without persisting (already persisted by the config manager)
                ApplyModeAndCompression(
                    streamMode.Width,
                    streamMode.Height,
                    streamMode.Framerate,
                    streamMode.Quality
                );
            });
        }

        /// <summary>
        /// Handles passthrough stream enable/disable config changes.
        /// </summary>
        /// <param name="enabled">Whether streaming should be enabled.</param>
        private async void OnEnablePassthroughStreamChanged(bool enabled)
        {
            if (cameraAccess is null || !cameraAccess.enabled)
            {
                QueuedLogger.Log("Disabled - cameraAccess is unset or disabled");
                return;
            }

            switch (enabled)
            {
                // Setting to enabled when already running
                case true when isInitialized:
                {
                    QueuedLogger.Log("Already initialized, skipping");
                    break;
                }
                // Setting to disabled when already not running
                case false when !isInitialized:
                {
                    QueuedLogger.Log("Already disabled, skipping");
                    break;
                }
                // Setting to enabled when not running
                case true when !isInitialized:
                {
                    if (cameraSource is null)
                    {
                        QueuedLogger.LogError(
                            "CameraSource was null! Cannot initialize passthrough"
                        );
                        break;
                    }

                    QueuedLogger.Log("Initializing passthrough camera...");

                    cameraSource.Description = "Quest Headset Camera";

                    // Populate the list of modes from the supported resolutions
                    var supportedResolutions = PassthroughCameraAccess.GetSupportedResolutions(
                        cameraAccess.CameraPosition
                    );
                    var modes = new VideoMode[supportedResolutions.Length * FpsOptions.Length];
                    int i = 0;
                    foreach (var resolution in supportedResolutions)
                    {
                        foreach (var fps in FpsOptions)
                        {
                            modes[i++] = new VideoMode(
                                PixelFormat.MJPEG,
                                resolution.x,
                                resolution.y,
                                fps
                            );
                        }
                    }

                    cameraSource.Modes = modes;
                    cameraSource.SelectedModeChanged += OnSelectedModeChanged;

                    // Load video mode and quality from config
                    var streamMode = await configManager.GetStreamModeAsync();
                    compressionQuality = streamMode.Quality;

                    // Find best matching mode for the configured stream mode
                    var bestMatch = FindBestMatchingModeOrBetter(
                        streamMode.Width,
                        streamMode.Height,
                        streamMode.Framerate
                    );

                    if (bestMatch.HasValue)
                    {
                        cameraSource.Mode = bestMatch.Value;
                        QueuedLogger.Log(
                            $"Selected mode {bestMatch.Value} with quality {compressionQuality}"
                        );
                    }
                    else
                    {
                        // Fallback to default mode if no match found
                        cameraSource.Mode = cameraSource.Modes[3];
                        QueuedLogger.LogWarning(
                            $"Could not find matching mode for {streamMode}, using {cameraSource.Mode}"
                        );
                    }

                    // Start initialization coroutine
                    frameCaptureCoroutine = coroutineHost.StartCoroutine(FrameCaptureCoroutine());
                    isInitialized = true;
                    cameraSource.IsConnected = true;

                    break;
                }
                // Setting to disabled when running
                case false when isInitialized:
                {
                    QueuedLogger.Log("Disabling Passthrough");

                    // Remove callback from cameraSource
                    cameraSource.SelectedModeChanged -= OnSelectedModeChanged;

                    // Stop Coroutine
                    if (frameCaptureCoroutine != null)
                    {
                        coroutineHost.StopCoroutine(frameCaptureCoroutine);
                        frameCaptureCoroutine = null;
                    }

                    isInitialized = false;
                    cameraSource.IsConnected = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Captures frames from the passthrough camera at the requested frame rate and encodes them as JPEG.
        /// </summary>
        public IEnumerator FrameCaptureCoroutine()
        {
            QueuedLogger.Log("Initialized");

            while (true)
            {
                try
                {
                    var texture = cameraAccess.GetTexture();
                    if (texture is not Texture2D texture2D)
                    {
                        QueuedLogger.LogError(
                            $"GetTexture returned an incompatible object ({texture.GetType().Name})"
                        );
                        yield break;
                    }

                    CurrentFrame = new EncodedFrame(
                        Time.frameCount,
                        texture2D.EncodeToJPG(compressionQuality)
                    );
                }
                catch (NullReferenceException ex)
                {
                    // This probably means the app hasn't been given permission to access the headset camera.
                    QueuedLogger.LogError(
                        $"Error capturing frame - verify 'Headset Cameras' app permission is enabled. {ex.Message}"
                    );
                    yield break;
                }

                yield return new WaitForSeconds(FrameDelaySeconds);
            }
        }

        /// <summary>
        /// Invokes an action on the main thread using the captured SynchronizationContext.
        /// Falls back to direct invocation if no context was captured.
        /// </summary>
        private void InvokeOnMainThread(Action action)
        {
            if (mainThreadContext == null)
            {
                action();
            }
            else
            {
                mainThreadContext.Post(_ => action(), null);
            }
        }
    }
}
