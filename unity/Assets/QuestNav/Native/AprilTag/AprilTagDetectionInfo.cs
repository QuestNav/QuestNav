using Meta.XR;

namespace QuestNav.Native.AprilTag
{
    public unsafe class AprilTagDetectionInfo
    {
        internal ApriltagDetectionInfoNative* Handle { get; private set; }

        public AprilTagDetectionInfo(ApriltagDetectionInfoNative* handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Sets the intrinsics (properties) of the camera used for the detection
        /// </summary>
        /// <param name="cx">Focal center x</param>
        /// <param name="cy">Focal center y</param>
        /// <param name="fx">Focal length x</param>
        /// <param name="fy">Focal length y </param>
        public void SetCameraIntrinsics(double cx, double cy, double fx, double fy)
        {
            Handle->cx = cx;
            Handle->cy = cy;
            Handle->fx = fx;
            Handle->fy = fy;
        }

        /// <summary>
        /// Sets the intrinsics (properties) of the camera used for the detection
        /// </summary>
        /// <param name="metaIntrinsics">The <see cref="PassthroughCameraAccess.CameraIntrinsics">Meta Camera Intrinsics</see> from MRUK</param>
        public void SetCameraIntrinsics(PassthroughCameraAccess.CameraIntrinsics metaIntrinsics)
        {
            Handle->cx = metaIntrinsics.PrincipalPoint.x;
            Handle->cy = metaIntrinsics.PrincipalPoint.y;
            Handle->fx = metaIntrinsics.FocalLength.x;
            Handle->fy = metaIntrinsics.FocalLength.y;
        }

        /// <summary>
        /// Sets the PHYSICAL size of the tag to be used for pose estimation
        /// </summary>
        /// <param name="tagSize"></param>
        public void SetTagSize(double tagSize)
        {
            Handle->tagsize = tagSize;
        }
    }
}
