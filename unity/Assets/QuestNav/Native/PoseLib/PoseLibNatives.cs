using System.Runtime.InteropServices;

namespace QuestNav.QuestNav.Native.PoseLib
{
    public static class PoseLibNatives
    {
        public enum PoseLibCameraModelIdNative
        {
            POSELIB_CAMERA_NULL = -1,
            POSELIB_CAMERA_SIMPLE_PINHOLE = 0,
            POSELIB_CAMERA_PINHOLE = 1,
            POSELIB_CAMERA_SIMPLE_RADIAL = 2,
            POSELIB_CAMERA_RADIAL = 3,
            POSELIB_CAMERA_OPENCV = 4,
            POSELIB_CAMERA_OPENCV_FISHEYE = 5,
            POSELIB_CAMERA_FULL_OPENCV = 6,
        }

        public unsafe struct PoseLibCameraPoseNative
        {
            /// <summary>
            /// Quaternion: [QW, QX, QY, QZ]
            /// </summary>
            public fixed double Q[4];

            /// <summary>
            /// Translation: [X, Y, Z]
            /// </summary>
            public fixed double T[3];
        }

        /// <summary>
        /// Estimate the camera pose from 2D-3D correspondences
        /// </summary>
        /// <param name="points2d">2D image points [x0, y0, x1, y1, ...] (corners of the AprilTags in the detected image)</param>
        /// <param name="points3d">3D world points [x0, y0, z0, x1, y1, z1, ...] (corners of the AprilTags in real 3D space from field layout)</param>
        /// <param name="numPoints">Total number of points (should be 4 per tag detected)</param>
        /// <param name="cameraModelID">The type of camera to use. Should be <see cref="PoseLibCameraModelIdNative.POSELIB_CAMERA_PINHOLE"/> when using
        /// the passthrough camera without manual calibration</param>
        /// <param name="imageWidth">The width of the frame in px</param>
        /// <param name="imageHeight">The height of the frame in px</param>
        /// <param name="cameraParams">The camera intrinsics</param>
        /// <param name="numCameraParams">The number of camera intrinsics</param>
        /// <param name="maxReprojError">Maximum allowed error between 2D and 3D point fitting Usually should be under 2px</param>
        /// <param name="poseOut">The pose of the camera returned from the solver</param>
        /// <param name="numInliersOut">The number of points that were accepted into the model</param>
        /// <returns>0 on success, non-zero on error</returns>
        [DllImport("poselib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int poselib_estimate_absolute_pose_simple(
            double[] points2d,
            double[] points3d,
            ulong numPoints,
            int cameraModelID,
            int imageWidth,
            int imageHeight,
            double[] cameraParams,
            ulong numCameraParams,
            double maxReprojError,
            out PoseLibCameraPoseNative poseOut,
            out ulong numInliersOut
        );
    }
}
