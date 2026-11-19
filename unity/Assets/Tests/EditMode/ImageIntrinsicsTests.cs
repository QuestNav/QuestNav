using System;
using NUnit.Framework;
using QuestNav.Camera;
using UnityEngine;

namespace QuestNav.Tests
{
    public class ImageIntrinsicsTests
    {
        private const double Tolerance = 1e-6;

        // Quest 3 style sensor intrinsics, in sensor pixels
        private static readonly Vector2 FocalLength = new Vector2(868.5f, 869.25f);
        private static readonly Vector2 PrincipalPoint = new Vector2(642.75f, 631.5f);
        private static readonly Vector2Int Sensor = new Vector2Int(1280, 1280);

        private static ImageIntrinsics Convert(
            Vector2 focalLength,
            Vector2 principalPoint,
            Vector2Int sensor,
            Vector2Int image
        )
        {
            Assert.IsTrue(
                ImageIntrinsics.TryFromSensor(
                    focalLength,
                    principalPoint,
                    sensor,
                    image,
                    out var intrinsics
                )
            );
            return intrinsics;
        }

        private static void AssertIntrinsics(
            ImageIntrinsics actual,
            double fx,
            double fy,
            double cx,
            double cy,
            Vector2Int resolution
        )
        {
            Assert.AreEqual(fx, actual.Fx, Tolerance, "fx");
            Assert.AreEqual(fy, actual.Fy, Tolerance, "fy");
            Assert.AreEqual(cx, actual.Cx, Tolerance, "cx");
            Assert.AreEqual(cy, actual.Cy, Tolerance, "cy");
            Assert.AreEqual(resolution, actual.Resolution);
        }

        // Re-implementation of PassthroughCameraAccess.WorldToViewportPoint (with
        // CalcSensorCropRegion), scaled from viewport to CurrentResolution pixels.
        private static Vector2 MetaCameraPointToPixel(
            Vector3 pointInCamera,
            Vector2 focalLength,
            Vector2 principalPoint,
            Vector2Int sensorResolution,
            Vector2Int currentResolution
        )
        {
            var sensorPoint = new Vector2(
                (pointInCamera.x / pointInCamera.z) * focalLength.x + principalPoint.x,
                (pointInCamera.y / pointInCamera.z) * focalLength.y + principalPoint.y
            );

            var sensor = (Vector2)sensorResolution;
            var current = (Vector2)currentResolution;
            Vector2 scaleFactor = new Vector2(current.x / sensor.x, current.y / sensor.y);
            float maxScale = Math.Max(scaleFactor.x, scaleFactor.y);
            scaleFactor = new Vector2(scaleFactor.x / maxScale, scaleFactor.y / maxScale);
            float cropX = sensor.x * (1f - scaleFactor.x) * 0.5f;
            float cropY = sensor.y * (1f - scaleFactor.y) * 0.5f;
            float cropWidth = sensor.x * scaleFactor.x;
            float cropHeight = sensor.y * scaleFactor.y;

            var viewport = new Vector2(
                (sensorPoint.x - cropX) / cropWidth,
                (sensorPoint.y - cropY) / cropHeight
            );
            return new Vector2(viewport.x * current.x, viewport.y * current.y);
        }

        [Test]
        public void IdentityWhenImageMatchesSensor()
        {
            var intrinsics = Convert(FocalLength, PrincipalPoint, Sensor, Sensor);

            AssertIntrinsics(
                intrinsics,
                FocalLength.x,
                FocalLength.y,
                PrincipalPoint.x,
                PrincipalPoint.y,
                Sensor
            );
        }

        [Test]
        public void SmallerSameAspectImageScalesEverything()
        {
            var image = new Vector2Int(640, 640);

            var intrinsics = Convert(FocalLength, PrincipalPoint, Sensor, image);

            AssertIntrinsics(
                intrinsics,
                FocalLength.x * 0.5,
                FocalLength.y * 0.5,
                PrincipalPoint.x * 0.5,
                PrincipalPoint.y * 0.5,
                image
            );
        }

        [Test]
        public void WiderImageCropsSensorRows()
        {
            // 1280x1280 -> 1280x960 keeps the middle 960 sensor rows, starting at row 160
            var image = new Vector2Int(1280, 960);

            var intrinsics = Convert(FocalLength, PrincipalPoint, Sensor, image);

            AssertIntrinsics(
                intrinsics,
                FocalLength.x,
                FocalLength.y,
                PrincipalPoint.x,
                PrincipalPoint.y - 160,
                image
            );
        }

        [Test]
        public void WiderAndSmallerImageCropsThenScales()
        {
            // Same 1280x960 crop at row 160, then scaled by 0.5
            var image = new Vector2Int(640, 480);

            var intrinsics = Convert(FocalLength, PrincipalPoint, Sensor, image);

            AssertIntrinsics(
                intrinsics,
                FocalLength.x * 0.5,
                FocalLength.y * 0.5,
                PrincipalPoint.x * 0.5,
                (PrincipalPoint.y - 160) * 0.5,
                image
            );
        }

        [Test]
        public void TallerImageCropsSensorColumns()
        {
            // 1280x960 -> 800x800 keeps the middle 960 sensor columns, starting at column 160,
            // then scales the 960x960 crop to 800x800
            var sensor = new Vector2Int(1280, 960);
            var image = new Vector2Int(800, 800);
            double scale = 800.0 / 960.0;

            var intrinsics = Convert(FocalLength, PrincipalPoint, sensor, image);

            AssertIntrinsics(
                intrinsics,
                FocalLength.x * scale,
                FocalLength.y * scale,
                (PrincipalPoint.x - 160) * scale,
                PrincipalPoint.y * scale,
                image
            );
        }

        [Test]
        public void ProjectionMatchesMetaWorldToViewportPoint()
        {
            var sensors = new[] { new Vector2Int(1280, 1280), new Vector2Int(1280, 960) };
            var images = new[]
            {
                new Vector2Int(1280, 1280),
                new Vector2Int(1280, 960),
                new Vector2Int(800, 600),
                new Vector2Int(320, 240),
                new Vector2Int(960, 1280),
                new Vector2Int(640, 640),
            };
            var points = new[]
            {
                new Vector3(0f, 0f, 1f),
                new Vector3(0.3f, -0.2f, 1.5f),
                new Vector3(-1.1f, 0.7f, 2.5f),
                new Vector3(0.05f, 0.6f, 0.8f),
                new Vector3(-0.4f, -0.45f, 4f),
            };

            foreach (var sensor in sensors)
            {
                foreach (var image in images)
                {
                    var intrinsics = Convert(FocalLength, PrincipalPoint, sensor, image);
                    foreach (var point in points)
                    {
                        var expected = MetaCameraPointToPixel(
                            point,
                            FocalLength,
                            PrincipalPoint,
                            sensor,
                            image
                        );
                        double u = (point.x / point.z) * intrinsics.Fx + intrinsics.Cx;
                        double v = (point.y / point.z) * intrinsics.Fy + intrinsics.Cy;

                        // Meta's mapping runs in float; allow a few thousandths of a pixel
                        string context = $"sensor {sensor}, image {image}, point {point}";
                        Assert.AreEqual(expected.x, u, 2e-3, "u for " + context);
                        Assert.AreEqual(expected.y, v, 2e-3, "v for " + context);
                    }
                }
            }
        }

        [Test]
        public void ConversionCommutesWithVerticalFlip()
        {
            // Flipping the image rows maps cy to (height - cy) on both sides of the conversion
            var image = new Vector2Int(1280, 960);
            var flippedPrincipalPoint = new Vector2(PrincipalPoint.x, Sensor.y - PrincipalPoint.y);

            var intrinsics = Convert(FocalLength, PrincipalPoint, Sensor, image);
            var flipped = Convert(FocalLength, flippedPrincipalPoint, Sensor, image);

            Assert.AreEqual(image.y - intrinsics.Cy, flipped.Cy, Tolerance);
        }

        [Test]
        public void RejectsMissingIntrinsics()
        {
            Assert.IsFalse(
                ImageIntrinsics.TryFromSensor(
                    Vector2.zero,
                    Vector2.zero,
                    Vector2Int.zero,
                    new Vector2Int(1280, 960),
                    out _
                )
            );
            Assert.IsFalse(
                ImageIntrinsics.TryFromSensor(
                    FocalLength,
                    PrincipalPoint,
                    Sensor,
                    Vector2Int.zero,
                    out _
                )
            );
        }
    }
}
