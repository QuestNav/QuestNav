using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuestNav.Config
{
    /// <summary>
    /// Provides reflection-based binding to static fields marked with [Config] attribute.
    /// Scans all loaded assemblies for configurable fields and provides get/set/schema generation.
    /// Handles type conversion, value clamping, and JSON serialization for web interface.
    /// </summary>
    public class ReflectionBinding
    {
        private readonly Dictionary<string, FieldInfo> m_fieldsByPath =
            new Dictionary<string, FieldInfo>();
        private readonly Dictionary<string, ConfigAttribute> m_attributesByPath =
            new Dictionary<string, ConfigAttribute>();
        private readonly Dictionary<string, Type> m_typesByPath = new Dictionary<string, Type>();

        public ReflectionBinding()
        {
            ScanConfigurableFields();
        }

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
                                string path = $"{type.Name}/{field.Name}";

                                m_fieldsByPath[path] = field;
                                m_attributesByPath[path] = configAttr;
                                m_typesByPath[path] = field.FieldType;
                            }
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that fail to load
                }
            }
        }

        public object GetValue(string path)
        {
            if (!m_fieldsByPath.TryGetValue(path, out var field))
            {
                throw new ArgumentException($"No configurable field found at path: {path}");
            }

            return field.GetValue(null);
        }

        public bool SetValue(string path, object value)
        {
            if (!m_fieldsByPath.TryGetValue(path, out var field))
            {
                return false;
            }

            if (!m_attributesByPath.TryGetValue(path, out var attr))
            {
                return false;
            }

            try
            {
                object convertedValue = ConvertValue(value, field.FieldType);
                convertedValue = ClampValue(convertedValue, field.FieldType, attr.Min, attr.Max);
                field.SetValue(null, convertedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.Name == "Color")
            {
                if (value is string colorString)
                {
                    // Parse hex color
                    return ParseColor(colorString);
                }
                else if (value is Newtonsoft.Json.Linq.JObject jObj)
                {
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

        public ConfigSchema GenerateSchema()
        {
            var schema = new ConfigSchema();

            foreach (var kvp in m_fieldsByPath.OrderBy(x => m_attributesByPath[x.Key].Order))
            {
                string path = kvp.Key;
                var field = kvp.Value;
                var attr = m_attributesByPath[path];

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

        public Dictionary<string, object> GetAllValues()
        {
            var values = new Dictionary<string, object>();

            foreach (var kvp in m_fieldsByPath)
            {
                string path = kvp.Key;
                var field = kvp.Value;
                values[path] = SerializeValue(field.GetValue(null));
            }

            return values;
        }

        public void ApplyValues(Dictionary<string, object> values)
        {
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

        private object SerializeValue(object value)
        {
            if (value == null)
                return null;

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

        private object CreateColor(float r, float g, float b, float a)
        {
            var colorType = Type.GetType("UnityEngine.Color, UnityEngine.CoreModule");
            if (colorType != null)
            {
                return Activator.CreateInstance(colorType, r, g, b, a);
            }
            return null;
        }

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
            return CreateColor(1, 1, 1, 1);
        }

        public bool HasPath(string path) => m_fieldsByPath.ContainsKey(path);

        public IEnumerable<string> GetAllPaths() => m_fieldsByPath.Keys;

        public int FieldCount => m_fieldsByPath.Count;
    }
}
