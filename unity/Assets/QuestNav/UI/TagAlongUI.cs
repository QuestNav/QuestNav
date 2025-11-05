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
        private Transform head;

        private float followDistance;

        private float positionSpeed;

        private float rotationSpeed;

        private float positionThresholdX;

        private float positionThresholdY;

        private float positionThresholdZ;

        private float moveThresholdAngle;

        private Transform transform;

        public TagAlongUI(
            Transform head,
            float followDistance,
            float positionSpeed,
            float rotationSpeed,
            float positionThresholdX,
            float positionThresholdY,
            float positionThresholdZ,
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
            this.positionThresholdZ = positionThresholdZ;
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
                Mathf.Abs(delta.x) > positionThresholdX
                || Mathf.Abs(delta.y) > positionThresholdY
                || Mathf.Abs(delta.z) > positionThresholdZ;

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
