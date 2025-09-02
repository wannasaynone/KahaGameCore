using System;
using System.Collections.Generic;
using System.Reflection;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Utility methods for accessing ValueContainer data via reflection
    /// </summary>
    public static class ValueContainerInspectorUtility
    {
        /// <summary>
        /// Gets the base values from a ValueContainer using reflection
        /// </summary>
        public static Dictionary<string, int> GetBaseValues(Instance container)
        {
            FieldInfo field = typeof(Instance).GetField("baseValues", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var baseValues = field.GetValue(container) as Dictionary<string, int>;
                return new Dictionary<string, int>(baseValues ?? new Dictionary<string, int>());
            }
            return new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets the temporary values from a ValueContainer using reflection
        /// </summary>
        public static List<ValueContainerInspectorData.TempValueData> GetTempValues(Instance container)
        {
            FieldInfo tempValuesField = typeof(Instance).GetField("tempValues", BindingFlags.NonPublic | BindingFlags.Instance);
            if (tempValuesField != null)
            {
                var tempValues = tempValuesField.GetValue(container) as Dictionary<string, int>;
                if (tempValues != null)
                {
                    var result = new List<ValueContainerInspectorData.TempValueData>();

                    // Convert the dictionary entries to TempValueData objects
                    foreach (var kvp in tempValues)
                    {
                        try
                        {
                            var guid = new Guid(kvp.Key);
                            var tempValueData = new ValueContainerInspectorData.TempValueData
                            {
                                guid = guid,
                                tag = "Temp Value", // We don't have tag information in the dictionary
                                value = kvp.Value
                            };
                            result.Add(tempValueData);
                        }
                        catch (FormatException)
                        {
                            // Skip invalid GUIDs
                            continue;
                        }
                    }

                    return result;
                }
            }
            return new List<ValueContainerInspectorData.TempValueData>();
        }

        /// <summary>
        /// Gets the string key-value pairs from a ValueContainer using reflection
        /// </summary>
        public static Dictionary<string, string> GetStringKeyValues(Instance container)
        {
            FieldInfo field = typeof(Instance).GetField("stringKeyValues", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var stringValues = field.GetValue(container) as Dictionary<string, string>;
                return new Dictionary<string, string>(stringValues ?? new Dictionary<string, string>());
            }
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Checks if a ValueContainer contains the specified search term in any of its values
        /// </summary>
        public static bool ContainerContainsSearchTerm(Instance container, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return true;

            searchTerm = searchTerm.ToLower();

            // Check base values
            Dictionary<string, int> baseValues = GetBaseValues(container);
            foreach (var kvp in baseValues)
            {
                if (kvp.Key.ToLower().Contains(searchTerm) || kvp.Value.ToString().Contains(searchTerm))
                {
                    return true;
                }
            }

            // Check temp values
            List<ValueContainerInspectorData.TempValueData> tempValues = GetTempValues(container);
            foreach (var tempValue in tempValues)
            {
                if (tempValue.tag.ToLower().Contains(searchTerm) ||
                    tempValue.value.ToString().Contains(searchTerm) ||
                    tempValue.guid.ToString().ToLower().Contains(searchTerm))
                {
                    return true;
                }
            }

            // Check string values
            Dictionary<string, string> stringValues = GetStringKeyValues(container);
            foreach (var kvp in stringValues)
            {
                if (kvp.Key.ToLower().Contains(searchTerm) ||
                    (kvp.Value != null && kvp.Value.ToLower().Contains(searchTerm)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
