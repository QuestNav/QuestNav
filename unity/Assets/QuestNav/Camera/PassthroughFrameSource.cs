using System;
using System.Collections;
using Meta.XR;
using QuestNav.Config;
using QuestNav.Core;
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
        private static readonly int[] FpsOptions = { 1, 5, 15, 24, 30, 48, 60 };

        /// <summary>
        /// MonoBehaviour for coroutine execution
        /// </summary>
        private readonly MonoBehaviour coroutineHost;

        private readonly PassthroughCameraAccess cameraAccess;
        private readonly INtCameraSource cameraSource;
        private readonly IConfigManager configManager;

        private bool isInitialized;
        private string baseUrl;

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

        private float FrameDelaySeconds => 1.0f / Math.Max(1, MaxFrameRate);

        private Coroutine frameCaptureCoroutine;

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

            // Attach to ConfigManager callbacks
            configManager.OnEnablePassthroughStreamChanged += OnEnablePassthroughStreamChanged;
        }

        private void OnSelectedModeChanged(VideoMode mode)
        {
            cameraAccess.enabled = false;
            cameraAccess.RequestedResolution = new Vector2Int(mode.Width, mode.Height);
            cameraAccess.enabled = true;
            QueuedLogger.Log($"Changed mode: {mode}");
        }

        private void OnEnablePassthroughStreamChanged(bool enabled)
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
                    // Arbitrarily pick the first, I guess?
                    // TODO: This should be stored in playerPrefs so that it doesn't reset
                    cameraSource.Mode = cameraSource.Modes[3];

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

                    CurrentFrame = new EncodedFrame(Time.frameCount, texture2D.EncodeToJPG());
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
    }
}
