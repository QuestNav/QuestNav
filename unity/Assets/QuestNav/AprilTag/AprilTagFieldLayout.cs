using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;
using Wpi.Proto;

namespace QuestNav.QuestNav.AprilTag
{
    public class AprilTagFieldLayout
    {
        [JsonProperty("tags")]
        public List<AprilTagFieldEntry> Tags { get; set; }

        [JsonProperty("field")]
        public Field2d Field { get; set; }

        /// <summary>
        /// Represents the physical size of the AprilTags on the field
        /// </summary>
        public double TagSize { get; }

        /// <summary>
        /// Creates a new AprilTagFieldLayout.
        /// <param name="tagSize">The size of the tags in meters (black part)</param>
        /// <see cref="LoadJsonFromFileAsync">LoadJsonFromFileAsync</see> MUST be called prior to getting data
        /// </summary>
        public AprilTagFieldLayout(double tagSize)
        {
            TagSize = tagSize;
        }

        /// <summary>
        /// Loads field JSON layout from the given filename. MUST be called prior to getting data from this class
        /// </summary>
        /// <param name="fileName">The filename to load</param>
        public async Task LoadJsonFromFileAsync(string fileName)
        {
            string filesPath = FileManager.GetStaticFilesPath("apriltag/fieldlayouts");
#if UNITY_ANDROID && !UNITY_EDITOR
            // Extract the JSON from the APK
            await FileManager.ExtractAndroidFileAsync(fileName, "apriltag/fieldlayouts", filesPath);
#else
            await Task.CompletedTask;
#endif
            string filePath = $"{filesPath}/{fileName}";
            using var file = File.OpenText(filePath);
            var jsonSerializer = new JsonSerializer();
            var root = (AprilTagFieldLayout)
                jsonSerializer.Deserialize(file, typeof(AprilTagFieldLayout));

            if (root == null)
                return;
            Tags = root.Tags;
            Field = root.Field;

            QueuedLogger.Log($"Loaded new AprilTagFieldLayout '{filePath}' with {Tags.Count} tags");
        }

        /// <summary>
        /// Gets all four corners of the AprilTag in field relative space given the ID and loaded layout
        /// </summary>
        /// <param name="id">The ID of the tag's pose to get</param>
        /// <returns>An array containing four Translation3Ds of the corners</returns>
        public Translation3d[] GetTagCorners(int id)
        {
            foreach (var tag in Tags)
            {
                if (tag.ID != id)
                    continue;

                var tagPose = tag.Pose;
                double halfSize = TagSize / 2.0;

                // Corners ordered to match the passthrough camera's clockwise detection order.
                // The Meta Quest passthrough camera produces a mirrored image, so the
                // AprilTag detector returns corners in CW order (BR, BL, UL, UR) rather
                // than the standard CCW convention.
                var cornerTransforms = new Transform3d[]
                {
                    new Transform3d(new Translation3d(0, -halfSize, -halfSize), new Rotation3d()), // p[0]: Bottom-right
                    new Transform3d(new Translation3d(0, halfSize, -halfSize), new Rotation3d()), // p[1]: Bottom-left
                    new Transform3d(new Translation3d(0, halfSize, halfSize), new Rotation3d()), // p[2]: Upper-left
                    new Transform3d(new Translation3d(0, -halfSize, halfSize), new Rotation3d()), // p[3]: Upper-right
                };

                var fieldTransforms = new[]
                {
                    tagPose.Plus(cornerTransforms[0]).Translation,
                    tagPose.Plus(cornerTransforms[1]).Translation,
                    tagPose.Plus(cornerTransforms[2]).Translation,
                    tagPose.Plus(cornerTransforms[3]).Translation,
                };

                return fieldTransforms;
            }
            // ID does not exist in our list. Warn user
            QueuedLogger.LogWarning(
                $"Attempted to get the pose of non-existent ID in the current field layout! ID: {id}"
            );
            return new Translation3d[] { };
        }
    }
}
