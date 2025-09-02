using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition.Editor
{
    /// <summary>
    /// Defines and manages styles for the ValueContainer inspector
    /// </summary>
    public static class ValueContainerInspectorStyles
    {
        // UI Styles
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle SubHeaderStyle { get; private set; }
        public static GUIStyle GuidStyle { get; private set; }
        public static GUIStyle ValueChangedStyle { get; private set; }
        public static GUIStyle SectionStyle { get; private set; }

        /// <summary>
        /// Initialize the GUI styles
        /// </summary>
        public static void InitStyles()
        {
            HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(4, 4, 8, 8)
            };

            SubHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(4, 4, 4, 4)
            };

            GuidStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };

            ValueChangedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.5f, 0.1f) }
            };

            SectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }
    }
}
