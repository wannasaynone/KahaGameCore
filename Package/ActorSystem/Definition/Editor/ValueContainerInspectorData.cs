using System;
using System.Collections.Generic;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Data structures used by the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorData
    {
        /// <summary>
        /// Represents a temporary value in the ValueContainer
        /// </summary>
        public class TempValueData
        {
            public Guid guid;
            public string tag;
            public int value;

            public override bool Equals(object obj)
            {
                if (obj is TempValueData other)
                {
                    return guid == other.guid && tag == other.tag && value == other.value;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return guid.GetHashCode() ^ tag.GetHashCode() ^ value.GetHashCode();
            }
        }

        /// <summary>
        /// State data for the inspector window
        /// </summary>
        public class InspectorState
        {
            // Foldout states
            public Dictionary<string, bool> containerFoldouts = new Dictionary<string, bool>();
            public Dictionary<string, bool> baseValuesFoldouts = new Dictionary<string, bool>();
            public Dictionary<string, bool> tempValuesFoldouts = new Dictionary<string, bool>();
            public Dictionary<string, bool> stringValuesFoldouts = new Dictionary<string, bool>();

            // Previous values for change detection
            public Dictionary<string, Dictionary<string, int>> previousBaseValues = new Dictionary<string, Dictionary<string, int>>();
            public Dictionary<string, List<TempValueData>> previousTempValues = new Dictionary<string, List<TempValueData>>();
            public Dictionary<string, Dictionary<string, string>> previousStringValues = new Dictionary<string, Dictionary<string, string>>();

            // New value input fields
            public string newBaseValueTag = "";
            public int newBaseValueAmount = 0;
            public string newTempValueTag = "";
            public int newTempValueAmount = 0;
            public string newStringKey = "";
            public string newStringValue = "";

            // Search and refresh settings
            public string searchFilter = "";
            public bool autoRefresh = true;
            public float refreshInterval = 0.5f;
            public float lastRefreshTime;
        }
    }
}
