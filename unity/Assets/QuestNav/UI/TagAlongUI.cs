using UnityEngine;

namespace QuestNav.UI
{
    /// <summary>
    /// This script makes a UI element follow the user's head position and rotation,
    /// while ensuring it remains within the user's field of view.
    /// </summary>
    public class TagAlongUIEnhanced : MonoBehaviour
    {
        [Tooltip("Location of the user's head. Assign OVRCameraRig's CenterEyeAnchor.")]
        public Transform head;

        [Tooltip("How far the UI should be from the user.")]
        public float followDistance;

        [Tooltip("How quickly the UI moves towards the target position.")]
        public float positionSpeed;

        [Tooltip("How quickly the UI rotates to match the user's rotation.")]
        public float rotationSpeed;

        [Tooltip("Distance threshold for movement along the World X-axis (sideways).")]
        public float positionThresholdX;

        [Tooltip("Distance threshold for movement along the World Y-axis (up/down).")]
        public float positionThresholdY;

        [Tooltip("Distance threshold for movement along the World Z-axis (forward/back).")]
        public float positionThresholdZ;

        [Tooltip("The difference in angle (in degrees) at which the UI starts rotating")]
        public float moveThresholdAngle;

        void LateUpdate()
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
