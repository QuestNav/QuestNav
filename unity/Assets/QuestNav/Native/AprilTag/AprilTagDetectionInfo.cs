using Meta.XR;

namespace QuestNav.Native.AprilTag
{
    public unsafe class AprilTagDetectionInfo
    {
        internal ApriltagDetectionInfoNative* Handle { get; private set; }

        private AprilTagDetectionInfo(ApriltagDetectionInfoNative* handle)
        {
            Handle = handle;
        }

        public void SetCameraIntrinsics(double cx, double cy, double fx, double fy)
        {
            Handle->cx = cx;
            Handle->cy = cy;
            Handle->fx = fx;
            Handle->fy = fy;
        }

        public void SetCameraIntrinsics(PassthroughCameraAccess.CameraIntrinsics metaIntrinsics)
        {
            Handle->cx = metaIntrinsics.PrincipalPoint.x;
            Handle->cy = metaIntrinsics.PrincipalPoint.y;
            Handle->fx = metaIntrinsics.FocalLength.x;
            Handle->fy = metaIntrinsics.FocalLength.y;
        }
    }
}
