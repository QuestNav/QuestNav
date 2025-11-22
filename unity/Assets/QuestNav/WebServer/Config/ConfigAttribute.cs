using System;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Attribute for marking static fields as runtime-configurable via the web interface.
    /// Decorated fields are automatically discovered via reflection and exposed in the configuration API.
    /// Apply this attribute to static fields in classes like Tunables to make them editable at runtime.
    /// Example: [Config(DisplayName = "Team Number", Min = 1, Max = 9999)] public static int teamNumber = 9999;
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Gets or sets the display name shown in the web interface
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description tooltip text shown to users
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the category/tab for grouping related settings (e.g., "QuestNav", "General")
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for numeric fields (int, float, double)
        /// </summary>
        public object Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for numeric fields (int, float, double)
        /// </summary>
        public object Max { get; set; }

        /// <summary>
        /// Gets or sets the increment step for sliders (e.g., 0.1 for fine control)
        /// </summary>
        public object Step { get; set; }

        /// <summary>
        /// Gets or sets the UI control type: "slider", "input", "checkbox", "select", or "color"
        /// </summary>
        public string ControlType { get; set; }

        /// <summary>
        /// Gets or sets whether changing this setting requires an app restart to take effect
        /// </summary>
        public bool RequiresRestart { get; set; }

        /// <summary>
        /// Gets or sets the display order within the category (lower values appear first)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the available options for select/dropdown controls
        /// </summary>
        public string[] Options { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ConfigAttribute with default values
        /// </summary>
        public ConfigAttribute()
        {
            Order = 100;
            RequiresRestart = false;
        }
        #endregion
    }
}
