using System;

namespace QuestNav.Config
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public object Min { get; set; }
        public object Max { get; set; }
        public object Step { get; set; }
        public string ControlType { get; set; }
        public bool RequiresRestart { get; set; }
        public int Order { get; set; }
        public string[] Options { get; set; }

        public ConfigAttribute()
        {
            Order = 100;
            RequiresRestart = false;
        }
    }
}
