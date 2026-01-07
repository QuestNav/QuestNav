using System;
using System.Runtime.InteropServices;

namespace QuestNav.Native.CPnP
{
    /// <summary>
    /// Represents native code entry points for a CPnP solver
    /// </summary>
    public static class CPnPNatives
    {

        /// <summary>
        /// Represents a result from the CPnP estimator
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CPnPResult
        {
            /// <summary>
            /// Quaternion (qw, qx, qy, qz) from world to camera
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public double[] qvec;

            /// <summary>
            /// Translation (tx, ty, tz) from world to camera
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] tvec;

            /// <summary>
            /// Refined quaternion (qw, qx, qy, qz) from world to camera
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public double[] qvec_GN;

            /// <summary>
            /// Refined translation (tx, ty, tz) from world to camera
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] tvec_GN;
        }

        /// <summary>
        /// Estimate the camera pose from 2D-3D correspondences
        /// </summary>
        /// <param name="points2D">2D image points [x0, y0, x1, y1, ...] (corners of the AprilTags in the detected image)</param>
        /// <param name="points3D">3D world points [x0, y0, z0, x1, y1, z1, ...] (corners of the AprilTags in real 3D space from field layout)</param>
        /// <param name="numPoints">Total number of points (should be 4 per tag detected)</param>
        /// <param name="cameraParams">Camera intrinsics [fx, fy, cx, cy]</param>
        /// <param name="result">A pointer to an object to write the result to</param>
        /// <returns>
        ///  0 if OK, 1 if error
        /// </returns>
        /// <remarks>
        /// <para>
        /// The CPnP algorithm is taken from "CPnP: Consistent Pose Estimator for Perspective-n-Point Problem with Bias Elimination"
        /// <seealso href="https://arxiv.org/pdf/2209.05824"/>
        /// </para>
        /// </remarks>
        [DllImport("cpnp", CallingConvention = CallingConvention.Cdecl)]
        public static extern int cpnp_solve(
            double[] points2D,
            double[] points3D,
            int numPoints,
            double[] cameraParams,
            ref CPnPResult result
        );

        /// <summary>
        /// Gets the error from the last CPnP calculation
        /// </summary>
        /// <returns>The error that occurred as a string (native pointer)</returns>
        [DllImport("cpnp", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cpnp_get_last_error();

        /// <summary>
        /// Cleans up the last error message from memory and destroys it
        /// </summary>
        [DllImport("cpnp", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cpnp_cleanup();

        /// <summary>
        /// Gets the last error message from the C++ program
        /// </summary>
        /// <returns>The error message</returns>
        public static string GetLastError()
        {
            var ptr = cpnp_get_last_error();
            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }
    }
}
