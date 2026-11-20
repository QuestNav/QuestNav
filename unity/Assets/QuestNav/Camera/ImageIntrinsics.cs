using System;
using UnityEngine;

namespace QuestNav.Camera
{
    /// <summary>
    /// Pinhole intrinsics in the pixel space of a captured passthrough image.
    /// </summary>
    public readonly struct ImageIntrinsics
    {
        public double Fx { get; }
        public double Fy { get; }
        public double Cx { get; }
        public double Cy { get; }

        /// <summary>
        /// Size of the image these intrinsics apply to.
        /// </summary>
        public Vector2Int Resolution { get; }

        public ImageIntrinsics(double fx, double fy, double cx, double cy, Vector2Int resolution)
        {
            Fx = fx;
            Fy = fy;
            Cx = cx;
            Cy = cy;
            Resolution = resolution;
        }

        /// <summary>
        /// Converts Meta's static sensor intrinsics into the pixel space of an image captured at
        /// <paramref name="imageResolution"/>. Like <c>PassthroughCameraAccess.CalcSensorCropRegion</c>,
        /// the sensor is centre-cropped to the image aspect ratio and the crop is scaled to the image size.
        /// </summary>
        /// <returns>False if any resolution or focal length is not positive.</returns>
        public static bool TryFromSensor(
            Vector2 focalLength,
            Vector2 principalPoint,
            Vector2Int sensorResolution,
            Vector2Int imageResolution,
            out ImageIntrinsics result
        )
        {
            result = default;
            if (
                sensorResolution.x <= 0
                || sensorResolution.y <= 0
                || imageResolution.x <= 0
                || imageResolution.y <= 0
                || focalLength.x <= 0
                || focalLength.y <= 0
            )
            {
                return false;
            }

            double scaleX = (double)imageResolution.x / sensorResolution.x;
            double scaleY = (double)imageResolution.y / sensorResolution.y;
            double maxScale = Math.Max(scaleX, scaleY);

            double cropWidth = sensorResolution.x * (scaleX / maxScale);
            double cropHeight = sensorResolution.y * (scaleY / maxScale);
            double cropX = (sensorResolution.x - cropWidth) * 0.5;
            double cropY = (sensorResolution.y - cropHeight) * 0.5;

            double pixelsPerSensorX = imageResolution.x / cropWidth;
            double pixelsPerSensorY = imageResolution.y / cropHeight;

            result = new ImageIntrinsics(
                focalLength.x * pixelsPerSensorX,
                focalLength.y * pixelsPerSensorY,
                (principalPoint.x - cropX) * pixelsPerSensorX,
                (principalPoint.y - cropY) * pixelsPerSensorY,
                imageResolution
            );
            return true;
        }
    }
}
