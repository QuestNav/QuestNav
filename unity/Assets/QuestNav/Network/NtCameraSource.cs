using System;
using QuestNav.Core;
using QuestNav.Native.NTCore;

namespace QuestNav.Network
{
    /// <summary>
    /// Pixel format for video frames.
    /// </summary>
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

    /// <summary>
    /// Represents a video mode configuration with resolution, format, and framerate.
    /// </summary>
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
        /// <list>
        ///   <item>0: MJPEG, 320x240 @ 1fps</item>
        ///   <item>1: MJPEG, 320x240 @ 5fps</item>
        ///   <item>2: MJPEG, 320x240 @ 15fps</item>
        ///   <item>3: MJPEG, 320x240 @ 24fps</item>
        ///   <item>4: MJPEG, 320x240 @ 30fps</item>
        ///   <item>5: MJPEG, 320x240 @ 48fps</item>
        ///   <item>6: MJPEG, 320x240 @ 60fps</item>
        ///   <item>7: MJPEG, 640x360 @ 1fps</item>
        ///   <item>8: MJPEG, 640x360 @ 5fps</item>
        ///   <item>9: MJPEG, 640x360 @ 15fps</item>
        ///   <item>10: MJPEG, 640x360 @ 24fps</item>
        ///   <item>11: MJPEG, 640x360 @ 30fps</item>
        ///   <item>12: MJPEG, 640x360 @ 48fps</item>
        ///   <item>13: MJPEG, 640x360 @ 60fps</item>
        ///   <item>14: MJPEG, 640x480 @ 1fps</item>
        ///   <item>15: MJPEG, 640x480 @ 5fps</item>
        ///   <item>16: MJPEG, 640x480 @ 15fps</item>
        ///   <item>17: MJPEG, 640x480 @ 24fps</item>
        ///   <item>18: MJPEG, 640x480 @ 30fps</item>
        ///   <item>19: MJPEG, 640x480 @ 48fps</item>
        ///   <item>20: MJPEG, 640x480 @ 60fps</item>
        ///   <item>21: MJPEG, 720x480 @ 1fps</item>
        ///   <item>22: MJPEG, 720x480 @ 5fps</item>
        ///   <item>23: MJPEG, 720x480 @ 15fps</item>
        ///   <item>24: MJPEG, 720x480 @ 24fps</item>
        ///   <item>25: MJPEG, 720x480 @ 30fps</item>
        ///   <item>26: MJPEG, 720x480 @ 48fps</item>
        ///   <item>27: MJPEG, 720x480 @ 60fps</item>
        ///   <item>28: MJPEG, 720x576 @ 1fps</item>
        ///   <item>29: MJPEG, 720x576 @ 5fps</item>
        ///   <item>30: MJPEG, 720x576 @ 15fps</item>
        ///   <item>31: MJPEG, 720x576 @ 24fps</item>
        ///   <item>32: MJPEG, 720x576 @ 30fps</item>
        ///   <item>33: MJPEG, 720x576 @ 48fps</item>
        ///   <item>34: MJPEG, 720x576 @ 60fps</item>
        ///   <item>35: MJPEG, 800x600 @ 1fps</item>
        ///   <item>36: MJPEG, 800x600 @ 5fps</item>
        ///   <item>37: MJPEG, 800x600 @ 15fps</item>
        ///   <item>38: MJPEG, 800x600 @ 24fps</item>
        ///   <item>39: MJPEG, 800x600 @ 30fps</item>
        ///   <item>40: MJPEG, 800x600 @ 48fps</item>
        ///   <item>41: MJPEG, 800x600 @ 60fps</item>
        ///   <item>42: MJPEG, 1024x576 @ 1fps</item>
        ///   <item>43: MJPEG, 1024x576 @ 5fps</item>
        ///   <item>44: MJPEG, 1024x576 @ 15fps</item>
        ///   <item>45: MJPEG, 1024x576 @ 24fps</item>
        ///   <item>46: MJPEG, 1024x576 @ 30fps</item>
        ///   <item>47: MJPEG, 1024x576 @ 48fps</item>
        ///   <item>48: MJPEG, 1024x576 @ 60fps</item>
        ///   <item>49: MJPEG, 1280x720 @ 1fps</item>
        ///   <item>50: MJPEG, 1280x720 @ 5fps</item>
        ///   <item>51: MJPEG, 1280x720 @ 15fps</item>
        ///   <item>52: MJPEG, 1280x720 @ 24fps</item>
        ///   <item>53: MJPEG, 1280x720 @ 30fps</item>
        ///   <item>54: MJPEG, 1280x720 @ 48fps</item>
        ///   <item>55: MJPEG, 1280x720 @ 60fps</item>
        ///   <item>56: MJPEG, 1280x960 @ 1fps</item>
        ///   <item>57: MJPEG, 1280x960 @ 5fps</item>
        ///   <item>58: MJPEG, 1280x960 @ 15fps</item>
        ///   <item>59: MJPEG, 1280x960 @ 24fps</item>
        ///   <item>60: MJPEG, 1280x960 @ 30fps</item>
        ///   <item>61: MJPEG, 1280x960 @ 48fps</item>
        ///   <item>62: MJPEG, 1280x960 @ 60fps</item>
        ///   <item>63: MJPEG, 1280x1080 @ 1fps</item>
        ///   <item>64: MJPEG, 1280x1080 @ 5fps</item>
        ///   <item>65: MJPEG, 1280x1080 @ 15fps</item>
        ///   <item>66: MJPEG, 1280x1080 @ 24fps</item>
        ///   <item>67: MJPEG, 1280x1080 @ 30fps</item>
        ///   <item>68: MJPEG, 1280x1080 @ 48fps</item>
        ///   <item>69: MJPEG, 1280x1080 @ 60fps</item>
        ///   <item>70: MJPEG, 1280x1280 @ 1fps</item>
        ///   <item>71: MJPEG, 1280x1280 @ 5fps</item>
        ///   <item>72: MJPEG, 1280x1280 @ 15fps</item>
        ///   <item>73: MJPEG, 1280x1280 @ 24fps</item>
        ///   <item>74: MJPEG, 1280x1280 @ 30fps</item>
        ///   <item>75: MJPEG, 1280x1280 @ 48fps</item>
        ///   <item>76: MJPEG, 1280x1280 @ 60fps</item>
        /// </list>
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
        /// <summary>
        /// Publisher for the source identifier string.
        /// </summary>
        private readonly StringPublisher sourcePublisher;

        /// <summary>
        /// Publisher for available stream URLs.
        /// </summary>
        private readonly StringArrayPublisher streamsPublisher;

        /// <summary>
        /// Publisher for source description.
        /// </summary>
        private readonly StringPublisher descriptionPublisher;

        /// <summary>
        /// Publisher for connection status.
        /// </summary>
        private readonly BooleanPublisher connectedPublisher;

        /// <summary>
        /// Entry for current video mode.
        /// </summary>
        private readonly StringEntry modeEntry;

        /// <summary>
        /// Publisher for available video modes.
        /// </summary>
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

        /// <summary>
        /// Compares two arrays for element-wise equality.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="s1">First array.</param>
        /// <param name="s2">Second array.</param>
        /// <returns>True if arrays have equal length and elements.</returns>
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
