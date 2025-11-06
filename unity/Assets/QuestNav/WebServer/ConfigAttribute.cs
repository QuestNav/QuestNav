using System;

namespace QuestNav.Config
{
    /// <summary>
    /// Attribute for marking static fields as runtime-configurable via the web interface.
    /// Decorated fields are automatically discovered via reflection and exposed in the configuration API.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {
        /// <summary>Gets or sets the display name shown in the web interface.</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets the description tooltip text.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the category/tab for grouping related settings.</summary>
        public string Category { get; set; }

        /// <summary>Gets or sets the minimum value for numeric fields.</summary>
        public object Min { get; set; }

        /// <summary>Gets or sets the maximum value for numeric fields.</summary>
        public object Max { get; set; }

        /// <summary>Gets or sets the increment step for sliders.</summary>
        public object Step { get; set; }

        /// <summary>Gets or sets the UI control type (slider, input, checkbox, select, color).</summary>
        public string ControlType { get; set; }

        /// <summary>Gets or sets whether changing this setting requires app restart.</summary>
        public bool RequiresRestart { get; set; }

        /// <summary>Gets or sets the display order within the category (lower = first).</summary>
        public int Order { get; set; }

        /// <summary>Gets or sets the available options for select/dropdown controls.</summary>
        public string[] Options { get; set; }

        public ConfigAttribute()
        {
            Order = 100;
            RequiresRestart = false;
        }
    }
}
