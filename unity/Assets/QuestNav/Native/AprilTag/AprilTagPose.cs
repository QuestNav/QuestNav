namespace QuestNav.Native.AprilTag
{
    public unsafe class AprilTagPose
    {
        internal AprilTagPoseNative* Handle { get; private set; }

        private AprilTagPose(AprilTagPoseNative* handle)
        {
            Handle = handle;
        }
    }
}
