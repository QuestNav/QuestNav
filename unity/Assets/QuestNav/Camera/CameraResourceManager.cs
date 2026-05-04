using System;
using System.Collections.Generic;
using System.Threading;
using Meta.XR;
using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Camera
{
    /// <summary>
    /// Priority of a camera resource request. AprilTag detection passes
    /// <see cref="High"/>; the passthrough video stream passes <see cref="Low"/>.
    /// </summary>
    public enum CameraRequestPriority
    {
        Low,
        High,
    }

    /// <summary>
    /// Single owner of <see cref="PassthroughCameraAccess.enabled"/> and
    /// <see cref="PassthroughCameraAccess.RequestedResolution"/>. Multiple subsystems
    /// (AprilTag detector, passthrough MJPEG stream) request a desired resolution
    /// through this arbiter; the arbiter applies the highest-priority winner and
    /// notifies subscribers when the effective resolution changes.
    ///
    /// AprilTag detection wins ties. The passthrough stream re-encodes whatever
    /// frames the camera produces.
    ///
    /// All Meta SDK calls are wrapped in try/catch with explicit logging so that
    /// SDK regressions surface as warnings instead of taking down the Unity app.
    /// </summary>
    public class CameraResourceManager
    {
        /// <summary>
        /// The Meta SDK camera object whose lifetime we manage.
        /// </summary>
        private readonly PassthroughCameraAccess cameraAccess;

        /// <summary>
        /// Captured at construction so we can marshal Meta SDK calls onto the Unity main thread.
        /// </summary>
        private readonly SynchronizationContext mainThreadContext;

        /// <summary>
        /// Active reservations keyed by requester id.
        /// </summary>
        private readonly Dictionary<string, Reservation> reservations =
            new Dictionary<string, Reservation>();

        /// <summary>
        /// The resolution most recently applied to the camera; null if the camera is currently disabled.
        /// </summary>
        private Vector2Int? appliedResolution;

        /// <summary>
        /// Lock object for thread-safe access to <see cref="reservations"/> and <see cref="appliedResolution"/>.
        /// </summary>
        private readonly object stateLock = new object();

        /// <summary>
        /// Raised on the main thread when the effective resolution changes (or the
        /// camera is enabled/disabled). Argument is null when the camera was disabled.
        /// </summary>
        public event Action<Vector2Int?> OnResolutionChanged;

        /// <summary>
        /// Creates a new camera arbiter. Captures the current
        /// <see cref="SynchronizationContext"/> for main-thread marshalling.
        /// </summary>
        /// <param name="cameraAccess">The Meta SDK camera reference to manage.</param>
        public CameraResourceManager(PassthroughCameraAccess cameraAccess)
        {
            this.cameraAccess = cameraAccess;
            mainThreadContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Returns the resolution currently being applied to the camera, or null
        /// if the camera is disabled (no active reservations).
        /// </summary>
        public Vector2Int? EffectiveResolution
        {
            get
            {
                lock (stateLock)
                {
                    return appliedResolution;
                }
            }
        }

        /// <summary>
        /// Returns true when at least one active reservation has
        /// <see cref="CameraRequestPriority.High"/> priority. Used by the web UI
        /// to display a "Locked by AprilTag" badge in the Camera tab.
        /// </summary>
        public bool IsLockedByHighPriority
        {
            get
            {
                lock (stateLock)
                {
                    foreach (var r in reservations.Values)
                    {
                        if (r.Priority == CameraRequestPriority.High)
                            return true;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Reserve the camera with the given resolution and priority. Returns the
        /// resolution actually applied; this may differ from <paramref name="resolution"/>
        /// if a higher-priority requester has already pinned a different resolution.
        ///
        /// Re-calling <see cref="Reserve"/> with the same <paramref name="requesterId"/>
        /// updates the existing reservation (use this to change resolution while remaining
        /// reserved).
        /// </summary>
        public Vector2Int Reserve(
            string requesterId,
            Vector2Int resolution,
            CameraRequestPriority priority
        )
        {
            Vector2Int winner;
            bool changed;
            lock (stateLock)
            {
                reservations[requesterId] = new Reservation(resolution, priority);
                winner = ResolveWinner();
                changed = !appliedResolution.HasValue || appliedResolution.Value != winner;
            }

            if (changed)
            {
                ApplyResolution(winner);
            }
            return winner;
        }

        /// <summary>
        /// Release a previously made reservation. The arbiter recomputes the winning
        /// resolution; if no reservations remain, the camera is disabled.
        /// </summary>
        public void Release(string requesterId)
        {
            Vector2Int? winner;
            bool changed;
            lock (stateLock)
            {
                if (!reservations.Remove(requesterId))
                {
                    return;
                }
                if (reservations.Count == 0)
                {
                    winner = null;
                }
                else
                {
                    winner = ResolveWinner();
                }
                changed = appliedResolution != winner;
            }

            if (changed)
            {
                ApplyResolution(winner);
            }
        }

        /// <summary>
        /// Picks the winning reservation: highest priority wins. On ties, the most
        /// recently inserted entry wins (Dictionary preserves insertion order).
        /// </summary>
        private Vector2Int ResolveWinner()
        {
            CameraRequestPriority bestPriority = CameraRequestPriority.Low;
            Vector2Int best = default;
            bool found = false;
            foreach (var r in reservations.Values)
            {
                if (!found || r.Priority > bestPriority)
                {
                    bestPriority = r.Priority;
                    best = r.Resolution;
                    found = true;
                }
            }
            return best;
        }

        /// <summary>
        /// Applies a new effective resolution to the camera (or disables it when null).
        /// Marshalled to the main thread; SDK calls are wrapped in try/catch so a
        /// failure logs but does not propagate.
        /// </summary>
        private void ApplyResolution(Vector2Int? target)
        {
            void Apply()
            {
                try
                {
                    if (target.HasValue)
                    {
                        cameraAccess.enabled = false;
                        cameraAccess.RequestedResolution = target.Value;
                        cameraAccess.enabled = true;
                        QueuedLogger.Log(
                            $"[CameraResourceManager] Applied resolution {target.Value.x}x{target.Value.y}"
                        );
                    }
                    else
                    {
                        cameraAccess.enabled = false;
                        QueuedLogger.Log(
                            "[CameraResourceManager] Disabled camera (no active reservations)"
                        );
                    }

                    lock (stateLock)
                    {
                        appliedResolution = target;
                    }

                    try
                    {
                        OnResolutionChanged?.Invoke(target);
                    }
                    catch (Exception subEx)
                    {
                        QueuedLogger.LogError(
                            $"[CameraResourceManager] Subscriber threw on OnResolutionChanged: {subEx.Message}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogError(
                        $"[CameraResourceManager] Failed to apply resolution {target}: {ex.Message}"
                    );
                }
            }

            if (mainThreadContext == null)
            {
                Apply();
            }
            else
            {
                mainThreadContext.Post(_ => Apply(), null);
            }
        }

        private readonly struct Reservation
        {
            public Vector2Int Resolution { get; }
            public CameraRequestPriority Priority { get; }

            public Reservation(Vector2Int resolution, CameraRequestPriority priority)
            {
                Resolution = resolution;
                Priority = priority;
            }
        }
    }
}
