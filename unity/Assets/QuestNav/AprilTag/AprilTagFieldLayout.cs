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
        /// Creates a new AprilTagFieldLayout.
        /// <see cref="LoadJsonFromFileAsync">LoadJsonFromFileAsync</see> MUST be called prior to getting data
        /// </summary>
        public AprilTagFieldLayout()
        {
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
#endif
            string filePath = $"{filesPath}/{fileName}";
            using var file = File.OpenText(filePath);
            var jsonSerializer = new JsonSerializer();
            var root = (AprilTagFieldLayout) jsonSerializer.Deserialize(file, typeof(AprilTagFieldLayout));
            
            if (root == null) return;
            Tags = root.Tags;
            Field = root.Field;
            
            QueuedLogger.Log($"Loaded new AprilTagFieldLayout '{filePath}' with {Tags.Count} tags");
        }

        public double[] GetTagCorners(int id)
        {
            foreach (var tag in Tags)
            {
                if (tag.ID == id)
                {
                  return new []
                  {
                      
                  }
                }
            }
            // ID does not exist in our list. Warn user
            QueuedLogger.LogWarning($"Attempted to get the pose of non-existent ID in the current field layout! ID: {id}");
            return new double[] { };
        } 
    }
}
    
