using System;
using System.Collections;
using System.Collections.Generic;

namespace QuestNav.Native.AprilTag
{
    public unsafe class AprilTagDetectionResults : IDisposable, IEnumerable<AprilTagDetection>
    {
        internal ZArrayNative* Handle { get; private set; }

        /// <summary>
        /// Whether the array and the containing detections have been disposed yet
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The number of detections in the array
        /// </summary>
        public int NumberOfDetections { get; private set; }

        /// <summary>
        /// Creates a new AprilTagDetectionResults object
        /// </summary>
        /// <param name="handle">The handle of the ZArrayNative holding the pointers to the results</param>
        public AprilTagDetectionResults(ZArrayNative* handle)
        {
            Handle = handle;
            NumberOfDetections = AprilTagNatives.zarray_size(handle);
        }

        /// <summary>
        /// Gets the detection at a specified index
        /// </summary>
        /// <param name="idx">The index to get</param>
        /// <returns>AprilTagDetection if valid index</returns>
        public AprilTagDetection GetDetection(int idx)
        {
            ThrowIfDisposed();
            if (idx >= NumberOfDetections)
            {
                throw new IndexOutOfRangeException(
                    $"Attempted to access AprilTagDetection out of bounds {idx} for length {NumberOfDetections}"
                );
            }

            // Pull from index
            AprilTagNatives.zarray_get(Handle, idx, out var ptr);

            // Return new AprilTagDetectionNative at that memory address
            return new AprilTagDetection((AprilTagDetectionNative*)ptr);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the detections
        /// </summary>
        public IEnumerator<AprilTagDetection> GetEnumerator()
        {
            ThrowIfDisposed();
            for (int i = 0; i < NumberOfDetections; i++)
            {
                yield return GetDetection(i);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the detections (non-generic)
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Disposes of ALL AprilTagDetection's
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                if (Handle != null)
                {
                    AprilTagNatives.apriltag_detections_destroy(Handle);
                    Handle = null;
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the detector has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(AprilTagDetectionResults));
        }
    }
}
