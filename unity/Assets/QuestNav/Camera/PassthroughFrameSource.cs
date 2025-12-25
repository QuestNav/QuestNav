// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
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
        /// Whether the frame source has been initialized.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Cached base URL for stream endpoints.
        /// </summary>
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

        /// <summary>
        /// Delay between frame captures in seconds.
        /// </summary>
        private float FrameDelaySeconds => 1.0f / Math.Max(1, MaxFrameRate);

        /// <summary>
        /// Reference to the running frame capture coroutine.
        /// </summary>
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

        /// <summary>
        /// Handles video mode changes by updating the camera resolution.
        /// </summary>
        /// <param name="mode">The new video mode.</param>
        private void OnSelectedModeChanged(VideoMode mode)
        {
            cameraAccess.enabled = false;
            cameraAccess.RequestedResolution = new Vector2Int(mode.Width, mode.Height);
            cameraAccess.enabled = true;
            QueuedLogger.Log($"Changed mode: {mode}");
        }

        /// <summary>
        /// Handles passthrough stream enable/disable config changes.
        /// </summary>
        /// <param name="enabled">Whether streaming should be enabled.</param>
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
