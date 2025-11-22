using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Provides reflection-based binding to static fields marked with [Config] attribute.
    /// Scans all loaded assemblies for configurable fields and provides get/set/schema generation.
    /// Handles type conversion, value clamping, and JSON serialization for web interface.
    /// This class enables runtime modification of static configuration values through the web UI.
    /// </summary>
    public class ReflectionBinding
    {
        #region Fields
        /// <summary>
        /// Dictionary mapping configuration paths to FieldInfo objects
        /// </summary>
        private readonly Dictionary<string, FieldInfo> fieldsByPath =
            new Dictionary<string, FieldInfo>();

        /// <summary>
        /// Dictionary mapping configuration paths to ConfigAttribute metadata
        /// </summary>
        private readonly Dictionary<string, ConfigAttribute> attributesByPath =
            new Dictionary<string, ConfigAttribute>();

        /// <summary>
        /// Dictionary mapping configuration paths to field types
        /// </summary>
        private readonly Dictionary<string, Type> typesByPath = new Dictionary<string, Type>();
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new ReflectionBinding instance.
        /// Scans all loaded assemblies for fields marked with [Config] attribute.
        /// </summary>
        public ReflectionBinding()
        {
            ScanConfigurableFields();
        }
        #endregion

        #region Assembly Scanning
        /// <summary>
        /// Scans all loaded assemblies for static fields marked with [Config] attribute.
        /// Populates internal dictionaries with field information for fast access.
        /// Handles assembly load failures gracefully.
        /// </summary>
        private void ScanConfigurableFields()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        var fields = type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                        );

                        foreach (var field in fields)
                        {
                            var configAttr = field.GetCustomAttribute<ConfigAttribute>();

                            if (configAttr != null)
                            {
                                // Create path: "ClassName/fieldName"
                                string path = $"{type.Name}/{field.Name}";

                                fieldsByPath[path] = field;
                                attributesByPath[path] = configAttr;
                                typesByPath[path] = field.FieldType;
                            }
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that fail to load types
                    // This is expected for some Unity internal assemblies
                }
            }
        }
        #endregion

        #region Value Access
        /// <summary>
        /// Gets the current value of a configuration field.
        /// </summary>
        /// <param name="path">Path to the field (e.g., "Tunables/defaultTeamNumber")</param>
        /// <returns>Current value of the field</returns>
        /// <exception cref="ArgumentException">Thrown if path is invalid</exception>
        public object GetValue(string path)
        {
            if (!fieldsByPath.TryGetValue(path, out var field))
            {
                throw new ArgumentException($"No configurable field found at path: {path}");
            }

            return field.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a configuration field with type conversion and clamping.
        /// </summary>
        /// <param name="path">Path to the field (e.g., "Tunables/defaultTeamNumber")</param>
        /// <param name="value">New value to set (will be converted to field type)</param>
        /// <returns>True if value was set successfully, false otherwise</returns>
        public bool SetValue(string path, object value)
        {
            if (!fieldsByPath.TryGetValue(path, out var field))
            {
                return false;
            }

            if (!attributesByPath.TryGetValue(path, out var attr))
            {
                return false;
            }

            try
            {
                // Convert value to appropriate type
                object convertedValue = ConvertValue(value, field.FieldType);

                // Clamp value to min/max constraints
                convertedValue = ClampValue(convertedValue, field.FieldType, attr.Min, attr.Max);

                // Set the field value
                field.SetValue(null, convertedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Type Conversion
        /// <summary>
        /// Converts a value to the target type.
        /// Handles special cases like Unity Color and JSON.NET JObject.
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type to convert to</param>
        /// <returns>Converted value</returns>
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            // Special handling for Unity Color type
            if (targetType.Name == "Color")
            {
                if (value is string colorString)
                {
                    // Parse hex color string
                    return ParseColor(colorString);
                }
                else if (value is Newtonsoft.Json.Linq.JObject jObj)
                {
                    // Parse JSON object with r, g, b, a properties
                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float a = 1f;

                    if (jObj.TryGetValue("r", out var rToken))
                        r = rToken.ToObject<float>();
                    if (jObj.TryGetValue("g", out var gToken))
                        g = gToken.ToObject<float>();
                    if (jObj.TryGetValue("b", out var bToken))
                        b = bToken.ToObject<float>();
                    if (jObj.TryGetValue("a", out var aToken))
                        a = aToken.ToObject<float>();

                    return CreateColor(r, g, b, a);
                }
            }

            // Standard type conversions
            if (targetType == typeof(int))
                return Convert.ToInt32(value);

            if (targetType == typeof(float))
                return Convert.ToSingle(value);

            if (targetType == typeof(double))
                return Convert.ToDouble(value);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);

            if (targetType == typeof(string))
                return Convert.ToString(value);

            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Clamps a numeric value to the specified min/max constraints.
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <param name="type">Type of the value</param>
        /// <param name="min">Minimum allowed value (or null)</param>
        /// <param name="max">Maximum allowed value (or null)</param>
        /// <returns>Clamped value</returns>
        private object ClampValue(object value, Type type, object min, object max)
        {
            if (value == null)
                return value;

            try
            {
                if (type == typeof(int))
                {
                    int intValue = (int)value;
                    if (min != null)
                        intValue = Math.Max(intValue, Convert.ToInt32(min));
                    if (max != null)
                        intValue = Math.Min(intValue, Convert.ToInt32(max));
                    return intValue;
                }

                if (type == typeof(float))
                {
                    float floatValue = (float)value;
                    if (min != null)
                        floatValue = Math.Max(floatValue, Convert.ToSingle(min));
                    if (max != null)
                        floatValue = Math.Min(floatValue, Convert.ToSingle(max));
                    return floatValue;
                }

                if (type == typeof(double))
                {
                    double doubleValue = (double)value;
                    if (min != null)
                        doubleValue = Math.Max(doubleValue, Convert.ToDouble(min));
                    if (max != null)
                        doubleValue = Math.Min(doubleValue, Convert.ToDouble(max));
                    return doubleValue;
                }
            }
            catch
            {
                // Return unclamped value on error
            }

            return value;
        }
        #endregion

        #region Schema Generation
        /// <summary>
        /// Generates a complete configuration schema for the web interface.
        /// Includes all fields with their metadata, constraints, and current values.
        /// </summary>
        /// <returns>ConfigSchema with all configuration fields</returns>
        public ConfigSchema GenerateSchema()
        {
            var schema = new ConfigSchema();

            foreach (var kvp in fieldsByPath.OrderBy(x => attributesByPath[x.Key].Order))
            {
                string path = kvp.Key;
                var field = kvp.Value;
                var attr = attributesByPath[path];

                var fieldSchema = new ConfigFieldSchema
                {
                    path = path,
                    displayName = attr.DisplayName ?? field.Name,
                    description = attr.Description ?? "",
                    category = attr.Category ?? "General",
                    type = GetTypeString(field.FieldType),
                    controlType = attr.ControlType ?? InferControlType(field.FieldType),
                    min = attr.Min,
                    max = attr.Max,
                    step = attr.Step,
                    defaultValue = GetDefaultValue(field),
                    currentValue = SerializeValue(field.GetValue(null)),
                    requiresRestart = attr.RequiresRestart,
                    order = attr.Order,
                    options = attr.Options,
                };

                schema.fields.Add(fieldSchema);

                // Create separate instances for categories to avoid circular references
                if (!schema.categories.ContainsKey(fieldSchema.category))
                {
                    schema.categories[fieldSchema.category] = new List<ConfigFieldSchema>();
                }

                // Clone field schema for category dictionary
                var categoryFieldSchema = new ConfigFieldSchema
                {
                    path = fieldSchema.path,
                    displayName = fieldSchema.displayName,
                    description = fieldSchema.description,
                    category = fieldSchema.category,
                    type = fieldSchema.type,
                    controlType = fieldSchema.controlType,
                    min = fieldSchema.min,
                    max = fieldSchema.max,
                    step = fieldSchema.step,
                    defaultValue = fieldSchema.defaultValue,
                    currentValue = fieldSchema.currentValue,
                    requiresRestart = fieldSchema.requiresRestart,
                    order = fieldSchema.order,
                    options = fieldSchema.options,
                };

                schema.categories[fieldSchema.category].Add(categoryFieldSchema);
            }

            return schema;
        }

        /// <summary>
        /// Gets all configuration values as a dictionary.
        /// </summary>
        /// <returns>Dictionary of path to current value</returns>
        public Dictionary<string, object> GetAllValues()
        {
            var values = new Dictionary<string, object>();

            foreach (var kvp in fieldsByPath)
            {
                string path = kvp.Key;
                var field = kvp.Value;
                values[path] = SerializeValue(field.GetValue(null));
            }

            return values;
        }

        /// <summary>
        /// Applies a dictionary of values to configuration fields.
        /// Used when loading saved configuration from disk.
        /// </summary>
        /// <param name="values">Dictionary of path to value</param>
        public void ApplyValues(Dictionary<string, object> values)
        {
            if (values == null)
                return;

            foreach (var kvp in values)
            {
                try
                {
                    SetValue(kvp.Key, kvp.Value);
                }
                catch
                {
                    // Skip invalid values
                }
            }
        }
        #endregion

        #region Type Utilities
        /// <summary>
        /// Gets a string representation of a field type for the web interface.
        /// </summary>
        /// <param name="type">Field type</param>
        /// <returns>Type string ("int", "float", "bool", "string", "color", "object")</returns>
        private string GetTypeString(Type type)
        {
            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(string))
                return "string";
            if (type.Name == "Color")
                return "color";
            return "object";
        }

        /// <summary>
        /// Infers the appropriate UI control type for a field type.
        /// </summary>
        /// <param name="type">Field type</param>
        /// <returns>Control type ("checkbox", "color", "slider", "input")</returns>
        private string InferControlType(Type type)
        {
            if (type == typeof(bool))
                return "checkbox";
            if (type.Name == "Color")
                return "color";
            if (type == typeof(int) || type == typeof(float) || type == typeof(double))
                return "slider";
            return "input";
        }

        /// <summary>
        /// Gets the default value for a field type.
        /// </summary>
        /// <param name="field">Field info</param>
        /// <returns>Default value for the field type</returns>
        private object GetDefaultValue(FieldInfo field)
        {
            var type = field.FieldType;

            if (type == typeof(int))
                return 0;
            if (type == typeof(float))
                return 0f;
            if (type == typeof(double))
                return 0.0;
            if (type == typeof(bool))
                return false;
            if (type == typeof(string))
                return "";
            if (type.Name == "Color")
                return CreateColor(1, 1, 1, 1);

            return null;
        }
        #endregion

        #region Serialization
        /// <summary>
        /// Serializes a value for JSON transmission to web interface.
        /// Handles special cases like Unity Color.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        /// <returns>JSON-serializable object</returns>
        private object SerializeValue(object value)
        {
            if (value == null)
                return null;

            // Special handling for Unity Color type
            if (value.GetType().Name == "Color")
            {
                var type = value.GetType();
                float r = (float)type.GetProperty("r").GetValue(value);
                float g = (float)type.GetProperty("g").GetValue(value);
                float b = (float)type.GetProperty("b").GetValue(value);
                float a = (float)type.GetProperty("a").GetValue(value);

                return new Dictionary<string, float>
                {
                    { "r", r },
                    { "g", g },
                    { "b", b },
                    { "a", a },
                };
            }

            return value;
        }
        #endregion

        #region Color Utilities
        /// <summary>
        /// Creates a Unity Color instance using reflection (to avoid Unity assembly dependency).
        /// </summary>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Green component (0-1)</param>
        /// <param name="b">Blue component (0-1)</param>
        /// <param name="a">Alpha component (0-1)</param>
        /// <returns>Unity Color instance or null if type not found</returns>
        private object CreateColor(float r, float g, float b, float a)
        {
            var colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            if (colorType != null)
            {
                return Activator.CreateInstance(colorType, r, g, b, a);
            }
            return null;
        }

        /// <summary>
        /// Parses a hex color string (#RRGGBB format) into a Unity Color.
        /// </summary>
        /// <param name="hex">Hex color string (e.g., "#FF0000")</param>
        /// <returns>Unity Color instance</returns>
        private object ParseColor(string hex)
        {
            // Simple hex parser for #RRGGBB format
            if (hex.StartsWith("#") && hex.Length == 7)
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return CreateColor(r / 255f, g / 255f, b / 255f, 1f);
            }

            // Default to white on parse error
            return CreateColor(1, 1, 1, 1);
        }
        #endregion

        #region Public Query Methods
        /// <summary>
        /// Checks if a configuration path exists.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if path exists, false otherwise</returns>
        public bool HasPath(string path) => fieldsByPath.ContainsKey(path);

        /// <summary>
        /// Gets all available configuration paths.
        /// </summary>
        /// <returns>Enumerable of all paths</returns>
        public IEnumerable<string> GetAllPaths() => fieldsByPath.Keys;

        /// <summary>
        /// Gets the total number of configurable fields.
        /// </summary>
        public int FieldCount => fieldsByPath.Count;
        #endregion
    }
}
