using System;
using System.Collections.Generic;
using QuestNav.Core;
using QuestNav.Native.NTCore;
using QuestNav.Network;

namespace QuestNav.Network
{
    public enum PixelFormat
    {
        /// <summary>
        /// Unknown format
        /// </summary>
        Unknown,

        /// <summary>
        /// Motion-JPEG (compressed image data)
        /// </summary>
        MJPEG,
    }

    public struct VideoMode : IEquatable<VideoMode>
    {
        public bool Equals(VideoMode other)
        {
            return PixelFormat == other.PixelFormat
                && Width == other.Width
                && Height == other.Height
                && Fps == other.Fps;
        }

        public override bool Equals(object obj)
        {
            return obj is VideoMode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)PixelFormat, Width, Height, Fps);
        }

        /// <summary>
        /// The pixel format.
        /// </summary>
        public PixelFormat PixelFormat { get; }

        /// <summary>
        /// The image width in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The image width in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The camera's nominal frames per second.
        /// </summary>
        public int Fps { get; }

        /// <summary>
        /// Create a new video mode.
        /// </summary>
        /// <param name="pixelFormat">The pixel format enum as an integer.</param>
        /// <param name="width">The image width in pixels.</param>
        /// <param name="height">The image height in pixels.</param>
        /// <param name="fps">The camera's frames per second.</param>
        public VideoMode(PixelFormat pixelFormat, int width, int height, int fps)
        {
            PixelFormat = pixelFormat;
            Width = width;
            Height = height;
            Fps = fps;
        }

        /// <summary>
        /// Provide string description of video mode.
        /// </summary>
        /// <remarks>
        /// The returned string is "{width}x{height} {format} {fps} fps"
        /// </remarks>
        public override string ToString()
        {
            return $"{Width}x{Height} {PixelFormat} {Fps} fps";
        }
    }

    /// <summary>
    /// A camera source exposed through NetworkTables.
    /// </summary>
    public interface INtCameraSource
    {
        /// <summary>
        /// The name of this Camera Source.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descriptive, prefixed with type (eg: "usb:0")
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// URLs that can be used to stream data.
        /// </summary>
        string[] Streams { get; set; }

        /// <summary>
        /// Description of the source.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Whether source is connected
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// Current video mode.
        /// </summary>
        VideoMode Mode { get; set; }

        /// <summary>
        /// Available video modes.
        /// </summary>
        VideoMode[] Modes { get; set; }

        /// <summary>
        /// An event raised when the selected video mode changes.
        /// </summary>
        event Action<VideoMode> SelectedModeChanged;
    }

    /// <summary>
    /// An MJPEG camera source accessible over NetworkTables.
    /// </summary>
    /// <remarks>See CameraServer.java in allwpilib</remarks>
    public class NtCameraSource : INtCameraSource
    {
        private readonly StringPublisher sourcePublisher;
        private readonly StringArrayPublisher streamsPublisher;
        private readonly StringPublisher descriptionPublisher;
        private readonly BooleanPublisher connectedPublisher;
        private readonly StringEntry modeEntry;
        private readonly StringArrayPublisher modesPublisher;
        private string description;
        private bool isConnected;
        private string[] streams = Array.Empty<string>();
        private VideoMode[] modes = Array.Empty<VideoMode>();
        private string source;
        private VideoMode mode;

        /// <summary>
        /// The name of this Camera Source.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Descriptive, prefixed with type (eg: "usb:0")
        /// </summary>
        public string Source
        {
            get => source;
            set
            {
                if (!string.Equals(source, value))
                {
                    source = value;
                    sourcePublisher.Set(Source);
                }
            }
        }

        /// <summary>
        /// URLs that can be used to stream data.
        /// </summary>
        public string[] Streams
        {
            get => streams;
            set
            {
                value ??= Array.Empty<string>();
                if (!SequenceEqual(streams, value))
                {
                    streams = value;
                    streamsPublisher.Set(Streams);
                }
            }
        }

        /// <summary>
        /// Description of the source.
        /// </summary>
        public string Description
        {
            get => description;
            set
            {
                if (!string.Equals(description, value))
                {
                    description = value;
                    descriptionPublisher.Set(Description);
                }
            }
        }

        /// <summary>
        /// Whether source is connected (available?)
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    connectedPublisher.Set(IsConnected);
                }
            }
        }

        /// <summary>
        /// Current video mode.
        /// </summary>
        public VideoMode Mode
        {
            get => mode;
            set
            {
                if (!mode.Equals(value))
                {
                    mode = value;
                    modeEntry.Set(Mode.ToString());
                    SelectedModeChanged?.Invoke(mode);
                }
            }
        }

        /// <summary>
        /// Available video modes.
        /// </summary>
        public VideoMode[] Modes
        {
            get => modes;
            set
            {
                value ??= Array.Empty<VideoMode>();
                if (!SequenceEqual(modes, value))
                {
                    modes = value;
                    var modeStrings = new string[modes.Length];
                    for (int i = 0; i < modes.Length; i++)
                    {
                        modeStrings[i] = modes[i].ToString();
                    }

                    modesPublisher.Set(modeStrings);
                }
            }
        }

        /// <summary>
        /// An event raised when the selected video mode changes.
        /// </summary>
        public event Action<VideoMode> SelectedModeChanged;

        /// <summary>
        /// Represents a camera source that provides an MJPEG feed accessible over NetworkTables.
        /// </summary>
        /// <param name="ntInstance">The NetworkTables instance to use for publishing</param>
        /// <param name="name">The name of the camera source</param>
        /// <remarks>
        /// We publish sources to NetworkTables using the following structure:
        /// "/CameraPublisher/{Source.Name}/" - root
        /// - "source" (string): Descriptive, prefixed with type (e.g. "usb:0")
        /// - "streams" (string array): URLs that can be used to stream data
        /// - "description" (string): Description of the source
        /// - "connected" (boolean): Whether source is connected
        /// - "mode" (string): Current video mode
        /// - "modes" (string array): Available video modes
        /// - "Property/{Property}" - Property values
        /// - "PropertyInfo/{Property}" - Property supporting information
        /// </remarks>
        public NtCameraSource(NtInstance ntInstance, string name)
        {
            Name = name;
            sourcePublisher = ntInstance.GetStringPublisher(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "source"),
                QuestNavConstants.Network.NtPublisherSettings
            );
            streamsPublisher = ntInstance.GetStringArrayPublisher(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "streams"),
                QuestNavConstants.Network.NtPublisherSettings
            );
            descriptionPublisher = ntInstance.GetStringPublisher(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "description"),
                QuestNavConstants.Network.NtPublisherSettings
            );
            connectedPublisher = ntInstance.GetBooleanPublisher(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "connected"),
                QuestNavConstants.Network.NtPublisherSettings
            );

            modeEntry = ntInstance.GetStringEntry(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "mode"),
                QuestNavConstants.Network.NtPublisherSettings
            );
            modesPublisher = ntInstance.GetStringArrayPublisher(
                String.Join('/', QuestNavConstants.Topics.CAMERA_PUBLISHER, name, "modes"),
                QuestNavConstants.Network.NtPublisherSettings
            );

            Source = "unknown:";
        }

        private static bool SequenceEqual<T>(T[] s1, T[] s2)
        {
            if (s1.Length != s2.Length)
            {
                return false;
            }

            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1[i].Equals(s2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
