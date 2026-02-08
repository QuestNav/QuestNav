using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuestNav.Native.AprilTag
{
    /// <summary>
    /// Managed wrapper for AprilTag detector functionality.
    /// Provides detection of AprilTags in images with configurable parameters.
    /// </summary>
    public unsafe class AprilTagDetector : IDisposable
    {
        /// <summary>
        /// The native detector pointer
        /// </summary>
        internal AprilTagDetectorNative* Handle { get; private set; }

        /// <summary>
        /// A list of family handles added to this detector (for tracking purposes)
        /// </summary>
        private readonly List<AprilTagFamily> tagFamilies = new List<AprilTagFamily>();

        /// <summary>
        /// Tracks if the native structure has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Creates a new AprilTag detector with default parameters
        /// </summary>
        /// <param name="threadCount">Number of threads to use for detection (default: 1)</param>
        /// <param name="quadDecimate">Quad decimation factor for detection (default: 2.0)</param>
        /// <param name="quadSigma">Gaussian blur sigma for segmented image (default: 0.0)</param>
        /// <param name="refineEdges">Whether to refine edges by snapping to strong gradients (default: true)</param>
        /// <param name="decodeSharpening">Decode sharpening amount (default: 0.25)</param>
        /// <param name="debug">Whether to write debugging images (default: false)</param>
        public AprilTagDetector(
            int threadCount = 1,
            float quadDecimate = 2.0f,
            float quadSigma = 0.0f,
            bool refineEdges = true,
            double decodeSharpening = 0.25,
            bool debug = false
        )
        {
            Handle = AprilTagNatives.apriltag_detector_create();
            if (Handle == null)
                throw new InvalidOperationException("Failed to create AprilTag detector");

            // Set defaults using properties (which update the native struct)
            ThreadCount = threadCount;
            QuadDecimate = quadDecimate;
            QuadSigma = quadSigma;
            RefineEdges = refineEdges;
            DecodeSharpening = decodeSharpening;
            Debug = debug;
        }

        /// <summary>
        /// Gets or sets the number of threads to use for detection
        /// </summary>
        public int ThreadCount
        {
            get
            {
                ThrowIfDisposed();
                return Handle->nthreads;
            }
            set
            {
                ThrowIfDisposed();
                Handle->nthreads = value;
            }
        }

        /// <summary>
        /// Gets or sets the quad decimation factor.
        /// Detection can be done on a lower-resolution image, improving speed
        /// at a cost of pose accuracy and slight decrease in detection rate.
        /// </summary>
        public float QuadDecimate
        {
            get
            {
                ThrowIfDisposed();
                return Handle->quad_decimate;
            }
            set
            {
                ThrowIfDisposed();
                Handle->quad_decimate = value;
            }
        }

        /// <summary>
        /// Gets or sets the Gaussian blur sigma applied to the segmented image.
        /// Very noisy images benefit from non-zero values (e.g. 0.8).
        /// </summary>
        public float QuadSigma
        {
            get
            {
                ThrowIfDisposed();
                return Handle->quad_sigma;
            }
            set
            {
                ThrowIfDisposed();
                Handle->quad_sigma = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to refine edges by snapping to strong gradients.
        /// Generally recommended to be on. Ignored if QuadDecimate = 1.
        /// </summary>
        public bool RefineEdges
        {
            get
            {
                ThrowIfDisposed();
                return Handle->refine_edges;
            }
            set
            {
                ThrowIfDisposed();
                Handle->refine_edges = value;
            }
        }

        /// <summary>
        /// Gets or sets the decode sharpening amount.
        /// Can help decode small tags but may not help in odd lighting.
        /// Default is 0.25.
        /// </summary>
        public double DecodeSharpening
        {
            get
            {
                ThrowIfDisposed();
                return Handle->decode_sharpening;
            }
            set
            {
                ThrowIfDisposed();
                Handle->decode_sharpening = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to write debugging images to the working directory.
        /// Warning: This is slow.
        /// </summary>
        public bool Debug
        {
            get
            {
                ThrowIfDisposed();
                return Handle->debug;
            }
            set
            {
                ThrowIfDisposed();
                Handle->debug = value;
            }
        }

        /// <summary>
        /// Gets statistics from the last processed frame - number of edges detected
        /// </summary>
        public uint LastFrameEdges
        {
            get
            {
                ThrowIfDisposed();
                return Handle->nedges;
            }
        }

        /// <summary>
        /// Gets statistics from the last processed frame - number of segments detected
        /// </summary>
        public uint LastFrameSegments
        {
            get
            {
                ThrowIfDisposed();
                return Handle->nsegments;
            }
        }

        /// <summary>
        /// Gets statistics from the last processed frame - number of quads detected
        /// </summary>
        public uint LastFrameQuads
        {
            get
            {
                ThrowIfDisposed();
                return Handle->nquads;
            }
        }

        /// <summary>
        /// Adds a tag family to the detector with default error correction (2 bits)
        /// </summary>
        /// <param name="family">The tag family to add</param>
        public void AddFamily(AprilTagFamily family)
        {
            ThrowIfDisposed();
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            AprilTagNatives.apriltag_detector_add_family_bits(Handle, family.Handle, 2);
            tagFamilies.Add(family);
        }

        /// <summary>
        /// Adds a tag family to the detector with custom error correction bits
        /// </summary>
        /// <param name="family">The tag family to add</param>
        /// <param name="bitsCorrected">Number of error correction bits (2 is recommended)</param>
        public void AddFamily(AprilTagFamily family, int bitsCorrected)
        {
            ThrowIfDisposed();
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            AprilTagNatives.apriltag_detector_add_family_bits(Handle, family.Handle, bitsCorrected);
            tagFamilies.Add(family);
        }

        /// <summary>
        /// Removes a specific family from the detector.
        /// Does not destroy the family object itself.
        /// </summary>
        /// <param name="family">The tag family to remove</param>
        public void RemoveFamily(AprilTagFamily family)
        {
            ThrowIfDisposed();
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            AprilTagNatives.apriltag_detector_remove_family(Handle, family.Handle);
            tagFamilies.Remove(family);
        }

        /// <summary>
        /// Removes all families from the detector.
        /// Does not destroy the family objects themselves.
        /// </summary>
        public void RemoveAllFamilies()
        {
            ThrowIfDisposed();
            AprilTagNatives.apriltag_detector_clear_families(Handle);
            tagFamilies.Clear();
        }

        /// <summary>
        /// Detects AprilTags in the given image
        /// </summary>
        /// <param name="image">The grayscale image to process</param>
        /// <returns>List of detected AprilTags</returns>
        public AprilTagDetectionResults Detect(ImageU8 image)
        {
            ThrowIfDisposed();
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var detectionsArrayPtr = AprilTagNatives.apriltag_detector_detect(Handle, image.Handle);

            return new AprilTagDetectionResults(detectionsArrayPtr);
        }

        /// <summary>
        /// Throws ObjectDisposedException if the detector has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(AprilTagDetector));
        }

        /// <summary>
        /// Disposes of the native detector resources
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                // Remove all families and dispose them
                foreach (var tagFamily in tagFamilies)
                {
                    tagFamily.Dispose();
                }
                RemoveAllFamilies();

                // Dispose of the actual detector
                if (Handle != null)
                {
                    AprilTagNatives.apriltag_detector_destroy(Handle);
                    Handle = null;
                }

                disposed = true;
            }
        }
    }
}
