using QuestNav.QuestNav.Geometry;

namespace QuestNav.QuestNav.Native.PoseLib
{
    public unsafe class PoseLibResult
    {
        /// <summary>
        /// PoseLib's field-to-camera transform (x_cam = R * X + t, CV camera axes).
        /// Conversions.CvToFrc turns it into the camera's field pose.
        /// </summary>
        public Pose3d CameraPose { get; }

        /// <summary>
        /// How many points of the points inputted were fit to the model (inliers)
        /// </summary>
        public double AcceptedPoints { get; }

        /// <summary>
        /// Total number of 2D-3D point correspondences fed to the solver
        /// </summary>
        public int TotalPoints { get; }

        /// <summary>
        /// Distinct tags with enough corners fitting the solved pose to count toward the
        /// tag-count floor. Can be lower than the number of tags passed in.
        /// </summary>
        public int SupportingTags { get; internal set; }

        /// <summary>
        /// Creates a new CPnPResult from the native struct returned by the estimator
        /// </summary>
        /// <param name="cameraPose">The native CameraPose to convert</param>
        /// <param name="numInliersOut">The inlier count to convert</param>
        /// <param name="totalPoints">Total number of point correspondences</param>
        public PoseLibResult(
            PoseLibNatives.PoseLibCameraPoseNative cameraPose,
            ulong numInliersOut,
            int totalPoints
        )
        {
            var q = new Quaternion(
                cameraPose.Q[0],
                cameraPose.Q[1],
                cameraPose.Q[2],
                cameraPose.Q[3]
            );
            var t = new Translation3d(cameraPose.T[0], cameraPose.T[1], cameraPose.T[2]);

            CameraPose = new Pose3d(t, new Rotation3d(q));

            AcceptedPoints = numInliersOut;
            TotalPoints = totalPoints;
        }
    }
}
