using System;
using System.Runtime.InteropServices;

namespace QuestNav.Native.AprilTag
{
    /// <summary>
    /// Base class for AprilTag family wrappers.
    /// Tag families define the specific tag encoding schemes (e.g., tag36h11, tag25h9).
    /// </summary>
    public unsafe abstract class AprilTagFamily : IDisposable
    {
        /// <summary>
        /// The native family pointer
        /// </summary>
        internal abstract AprilTagFamilyNative* Handle { get; private protected set; }

        /// <summary>
        /// Tracks if the native structure has been disposed
        /// </summary>
        protected bool Disposed { get; set; }

        /// <summary>
        /// Gets the human-readable name of the family (e.g., "tag36h11")
        /// </summary>
        public string Name
        {
            get
            {
                ThrowIfDisposed();
                return Marshal.PtrToStringAnsi(Handle->name);
            }
        }

        /// <summary>
        /// Gets the number of codes in this tag family
        /// </summary>
        public uint CodeCount
        {
            get
            {
                ThrowIfDisposed();
                return Handle->ncodes;
            }
        }

        /// <summary>
        /// Gets the width of the tag at the border (in bits)
        /// </summary>
        public int WidthAtBorder
        {
            get
            {
                ThrowIfDisposed();
                return Handle->width_at_border;
            }
        }

        /// <summary>
        /// Gets the total width of the tag (in bits)
        /// </summary>
        public int TotalWidth
        {
            get
            {
                ThrowIfDisposed();
                return Handle->total_width;
            }
        }

        /// <summary>
        /// Gets whether the border is reversed (black on white vs white on black)
        /// </summary>
        public bool ReversedBorder
        {
            get
            {
                ThrowIfDisposed();
                return Handle->reversed_border;
            }
        }

        /// <summary>
        /// Gets the number of data bits encoded in the tag
        /// </summary>
        public uint BitCount
        {
            get
            {
                ThrowIfDisposed();
                return Handle->nbits;
            }
        }

        /// <summary>
        /// Gets the minimum Hamming distance between any two codes in the family.
        /// Higher values mean better error correction (e.g., tag36h11 has h=11).
        /// </summary>
        public uint HammingDistance
        {
            get
            {
                ThrowIfDisposed();
                return Handle->h;
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the family has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Disposes of the tag family native resources
        /// </summary>
        public abstract void Dispose();
    }

    /// <summary>
    /// The tag36h11 family contains 587 unique tags with 11-bit Hamming distance error correction.
    /// This is one of the most commonly used AprilTag families, offering a good balance
    /// between the number of unique tags and error robustness.
    /// </summary>
    public unsafe class Tag36h11 : AprilTagFamily
    {
        /// <summary>
        /// The native family pointer
        /// </summary>
        internal override AprilTagFamilyNative* Handle { get; private protected set; }

        /// <summary>
        /// Creates a new tag36h11 family instance
        /// </summary>
        public Tag36h11()
        {
            Handle = AprilTagNatives.tag36h11_create();
            if (Handle == null)
                throw new InvalidOperationException("Failed to create tag36h11 family");
        }

        /// <summary>
        /// Disposes of the tag36h11 family native resources
        /// </summary>
        public override void Dispose()
        {
            if (!Disposed)
            {
                if (Handle != null)
                {
                    AprilTagNatives.tag36h11_destroy(Handle);
                    Handle = null;
                }
                Disposed = true;
            }
        }
    }
}
