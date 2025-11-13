using UnityEngine;
using static QuestNav.Core.QuestNavConstants.UITagalong;

namespace QuestNav.UI
{
    public interface ITagAlongUI
    {
        /// <summary>
        /// Updates the position and rotation of the UI element to follow the user's head.
        /// </summary>
        void Periodic();
    }

    /// <summary>
    /// This script makes a UI element follow the user's head position and rotation,
    /// while ensuring it remains within the user's field of view.
    /// </summary>
    public class TagAlongUI : ITagAlongUI
    {
        /// <summary>
        /// Location of the user's head. Most likely OVRCameraRig's CenterEyeAnchor.
        /// </summary>
        private Transform head;

        /// <summary>
        /// The UI to be kept in view.
        /// </summary>
        private Transform transform;

        /// <summary>
        /// Initializes a new instance of the TagAlongUI class.
        /// </summary>
        /// <param name="head">Location of the user's head. Assign OVRCameraRig's CenterEyeAnchor.</param>
        /// <param name="transform">The UI to be kept in view.</param>
        public TagAlongUI(Transform head, Transform transform)
        {
            this.head = head;
            this.transform = transform;
        }

        public void Periodic()
        {
            // 1. Calculate the ideal target position
            Vector3 idealPosition = head.position + head.forward * FOLLOW_DISTANCE;

            // 2. Calculate the target rotation
            Vector3 lookDirection = transform.position - head.position;
            Quaternion idealRotation = Quaternion.LookRotation(lookDirection);

            // Determine if the UI needs to move based on position thresholds
            Vector3 delta = transform.position - idealPosition;
            bool needsPositionUpdate =
                Mathf.Abs(delta.x) > POSITION_THRESHOLD_X
                || Mathf.Abs(delta.y) > POSITION_THRESHOLD_Y;

            if (needsPositionUpdate)
            {
                // Move towards the ideal position
                transform.position = Vector3.Lerp(
                    transform.position,
                    idealPosition,
                    Time.deltaTime * POSITION_SPEED
                );
                // The angle is too large, so we rotate the UI to bring it back into the FOV.
                transform.rotation = idealRotation;
            }
            else
            {
                // The position is OK, but make sure the rotation isn't too far off.
                float angle = Vector3.Angle(head.forward, transform.forward);

                // Determine if the UI needs to rotate based on angle threshold
                if (angle > ANGLE_THRESHOLD)
                {
                    // The angle is too large, so we rotate the UI to bring it back into the FOV.
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        idealRotation,
                        Time.deltaTime * ROTATION_SPEED
                    );
                }
            }
        }
    }
}
