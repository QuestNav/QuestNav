using System;
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
        /// O(1) lookup of tag IDs known to the loaded layout. Rebuilt every successful
        /// load (<see cref="TryLoadFrom"/>). The detection pipeline calls
        /// <see cref="ContainsId"/> on every detected tag to drop false-positive IDs
        /// (e.g. tag36h11 noise that decodes to ID 554 when no such tag is on the field).
        /// Feeding such IDs into <c>PoseLibSolver</c> would mismatch the 2D/3D corner
        /// buffer lengths and produce a garbage pose tens of meters from the field.
        /// </summary>
        private HashSet<int> knownIds = new HashSet<int>();

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
        /// Loads a field-layout JSON file. Returns true on success, false on any failure
        /// (missing file, IO error, deserialization failure, empty <c>tags</c> array).
        /// On failure, the existing <see cref="Tags"/> / <see cref="Field"/> values are
        /// preserved so the caller can fall back to a different layout.
        ///
        /// Lookup order: the user-uploaded "custom" directory
        /// (<see cref="FileManager.GetCustomFieldLayoutDir"/>) is checked first. If the
        /// file is not there, falls through to the bundled
        /// <c>StreamingAssets/apriltag/fieldlayouts/</c> directory (extracted from the
        /// APK on Android). Bundled-name shadowing is rejected at upload time, so this
        /// order does not let a custom file silently override a bundled one.
        /// </summary>
        /// <param name="fileName">The filename to load (must include extension).</param>
        public async Task<bool> LoadJsonFromFileAsync(string fileName)
        {
            // 1) Custom-uploaded JSONs live in persistentDataPath; check there first.
            string customPath = Path.Combine(FileManager.GetCustomFieldLayoutDir(), fileName);
            if (File.Exists(customPath))
            {
                return TryLoadFrom(customPath);
            }

            // 2) Fall through to bundled. On Android this requires extracting the
            // JSON out of the APK first; on Editor / Standalone the StreamingAssets
            // path is directly readable.
            string bundledDir = FileManager.GetStaticFilesPath("apriltag/fieldlayouts");
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                await FileManager.ExtractAndroidFileAsync(
                    fileName,
                    "apriltag/fieldlayouts",
                    bundledDir
                );
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError(
                    $"Failed to extract bundled field layout '{fileName}' from APK: {ex.Message}"
                );
                return false;
            }
#else
            await Task.CompletedTask;
#endif
            string bundledPath = Path.Combine(bundledDir, fileName);
            return TryLoadFrom(bundledPath);
        }

        /// <summary>
        /// Synchronous loader used by both the custom and bundled code paths above.
        /// Captures the parse failure modes so the caller (which is async) can return
        /// a simple bool.
        /// </summary>
        private bool TryLoadFrom(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    QueuedLogger.LogError($"Field layout file does not exist: {filePath}");
                    return false;
                }

                using var file = File.OpenText(filePath);
                var jsonSerializer = new JsonSerializer();
                var root = (AprilTagFieldLayout)
                    jsonSerializer.Deserialize(file, typeof(AprilTagFieldLayout));

                if (root == null || root.Tags == null || root.Tags.Count == 0)
                {
                    QueuedLogger.LogError(
                        $"Field layout file '{filePath}' deserialized to an empty / invalid layout."
                    );
                    return false;
                }

                Tags = root.Tags;
                Field = root.Field;

                var rebuilt = new HashSet<int>();
                foreach (var tag in Tags)
                {
                    rebuilt.Add(tag.ID);
                }
                knownIds = rebuilt;

                QueuedLogger.Log(
                    $"Loaded new AprilTagFieldLayout '{filePath}' with {Tags.Count} tags"
                );
                return true;
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError(
                    $"Failed to load field layout '{filePath}': {ex.GetType().Name}: {ex.Message}"
                );
                return false;
            }
        }

        /// <summary>
        /// Gets the field-relative pose of a tag by ID, or null if the ID is not in the
        /// loaded layout. Used by the AprilTag detection pipeline to compute the true
        /// camera-to-tag distance for max-distance gating and dynamic std dev scaling.
        /// </summary>
        /// <param name="id">The ID of the tag's pose to get.</param>
        /// <returns>The tag's field-relative pose, or null if not found.</returns>
        public Pose3d GetTagPose(int id)
        {
            if (Tags == null)
                return null;
            foreach (var tag in Tags)
            {
                if (tag.ID == id)
                    return tag.Pose;
            }
            return null;
        }

        /// <summary>
        /// Fast O(1) check for whether an AprilTag ID is present in the loaded layout.
        /// Returns false before <see cref="LoadJsonFromFileAsync"/> succeeds. The detection
        /// pipeline uses this to drop false-positive detections (random noise patterns
        /// that decode to a valid tag36h11 codeword) before passing them to PoseLib.
        /// </summary>
        public bool ContainsId(int id)
        {
            return knownIds != null && knownIds.Contains(id);
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
            // ID does not exist in our list. Defense in depth: callers should now
            // pre-filter via ContainsId, but we still log once per unknown ID so a
            // regression in the filter is visible without spamming the log every
            // frame at the detection rate.
            if (loggedUnknownIds.Add(id))
            {
                QueuedLogger.LogWarning(
                    $"Attempted to get corners of non-existent ID in the current field layout! ID: {id} "
                        + "(further occurrences of this ID will be silenced)"
                );
            }
            return new Translation3d[] { };
        }

        private readonly HashSet<int> loggedUnknownIds = new HashSet<int>();
    }
}
