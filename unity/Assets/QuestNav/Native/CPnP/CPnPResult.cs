using QuestNav.QuestNav.Geometry;

namespace QuestNav.Native.CPnP
{
    /// <summary>
    /// Represents a result from the CPnP estimator
    /// </summary>
    public class CPnPResult
    {
        /// <summary>
        /// Represents the position of the camera in 3d field-relative FRC coordinate space
        /// </summary>
        public Pose3d CameraPose { get;}
        
        /// <summary>
        /// Creates a new CPnPResult from the native struct returned by the estimator
        /// </summary>
        /// <param name="result">The native CPnPResult to convert</param>
        public CPnPResult(CPnPNatives.CPnPResult result)
        {
            var q = new Quaternion(result.qvec_GN[1], result.qvec_GN[2], result.qvec_GN[3], result.qvec_GN[0]);
            var t = new Translation3d(result.tvec_GN[0], result.tvec_GN[1], result.tvec_GN[2]);

            CameraPose = new Pose3d(t, new Rotation3d(q));
        }
    }
}
