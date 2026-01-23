using Meta.XR;
using QuestNav.Core;
using QuestNav.Native.AprilTag;
using UnityEngine;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagManager
    {
        private readonly PassthroughCameraAccess cameraAccess;
        private AprilTagDetector aprilTagDetector;
        private readonly AprilTagFamily aprilTagFamily = new Tag36h11();

        public AprilTagManager(PassthroughCameraAccess cameraAccess)
        {
            this.cameraAccess = cameraAccess;
        }

        public void EnableDetector()
        {
            cameraAccess.RequestedResolution = new Vector2Int(
                QuestNavConstants.AprilTag.DETECTION_RESOLUTION_X,
                QuestNavConstants.AprilTag.DETECTION_RESOLUTION_Y
            );
            cameraAccess.enabled = true;
            aprilTagDetector = new AprilTagDetector();
            aprilTagDetector.AddFamily(aprilTagFamily);
        }

        public void DisableDetector()
        {
            cameraAccess.enabled = false;
            aprilTagDetector.Dispose();
            aprilTagFamily.Dispose();
        }
    }
}
