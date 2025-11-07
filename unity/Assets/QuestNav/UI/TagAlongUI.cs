using QuestNav.Utils;
using UnityEngine;

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
        /// How far the UI should be from the user.
        /// </summary>
        private float followDistance;

        /// <summary>
        /// How quickly the UI moves towards the target position.
        /// </summary>
        private float positionSpeed;

        /// <summary>
        /// How quickly the UI rotates to match the user's rotation.
        /// </summary>
        private float rotationSpeed;

        /// <summary>
        /// Distance threshold for UI movement along the World X-axis (sideways).
        /// </summary>
        private float positionThresholdX;

        /// <summary>
        /// Distance threshold for UI movement along the World Y-axis (up/down).
        /// </summary>
        private float positionThresholdY;

        /// <summary>
        /// The difference in angle (in degrees) at which the UI starts rotating.
        /// </summary>
        private float moveThresholdAngle;

        /// <summary>
        /// The UI to be kept in view.
        /// </summary>
        private Transform transform;

        /// <summary>
        /// Initializes a new instance of the TagAlongUI class.
        /// </summary>
        /// <param name="head">Location of the user's head. Assign OVRCameraRig's CenterEyeAnchor.</param>
        /// <param name="followDistance">How far the UI should be from the user.</param>
        /// <param name="positionSpeed">How quickly the UI moves towards the target position.</param>
        /// <param name="rotationSpeed">How quickly the UI rotates to match the user's rotation.</param>
        /// <param name="positionThresholdX">Distance threshold for UI movement along the World X-axis (sideways).</param>
        /// <param name="positionThresholdY">Distance threshold for UI movement along the World Y-axis (up/down).</param>
        /// <param name="moveThresholdAngle">The difference in angle (in degrees) at which the UI starts rotating.</param>
        /// <param name="transform">The UI to be kept in view.</param>
        public TagAlongUI(
            Transform head,
            float followDistance,
            float positionSpeed,
            float rotationSpeed,
            float positionThresholdX,
            float positionThresholdY,
            float moveThresholdAngle,
            Transform transform
        )
        {
            this.head = head;
            this.followDistance = followDistance;
            this.positionSpeed = positionSpeed;
            this.rotationSpeed = rotationSpeed;
            this.positionThresholdX = positionThresholdX;
            this.positionThresholdY = positionThresholdY;
            this.moveThresholdAngle = moveThresholdAngle;
            this.transform = transform;
        }

        public void Periodic()
        {
            // 1. Calculate the ideal target position
            Vector3 idealPosition = head.position + head.forward * followDistance;

            // 2. Calculate the target rotation
            Vector3 lookDirection = transform.position - head.position;
            Quaternion idealRotation = Quaternion.LookRotation(lookDirection);

            // Determine if the UI needs to move based on position thresholds
            Vector3 delta = transform.position - idealPosition;
            bool needsPositionUpdate =
                Mathf.Abs(delta.x) > positionThresholdX || Mathf.Abs(delta.y) > positionThresholdY;

            if (needsPositionUpdate)
            {
                // Move towards the ideal position
                transform.position = Vector3.Lerp(
                    transform.position,
                    idealPosition,
                    Time.deltaTime * positionSpeed
                );
                // The angle is too large, so we rotate the UI to bring it back into the FOV.
                transform.rotation = idealRotation;
            }
            else
            {
                // The position is OK, but make sure the rotation isn't too far off.
                float angle = Vector3.Angle(head.forward, transform.forward);

                // Determine if the UI needs to rotate based on angle threshold
                if (angle > moveThresholdAngle)
                {
                    // The angle is too large, so we rotate the UI to bring it back into the FOV.
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        idealRotation,
                        Time.deltaTime * rotationSpeed
                    );
                }
            }
        }
    }
}
