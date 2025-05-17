using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow.Editor
{
    [CustomEditor(typeof(GameFlowStep), true)]
    public class GameFlowStepEditor : UnityEditor.Editor
    {
        private SerializedProperty descriptionProperty;

        protected virtual void OnEnable()
        {
            descriptionProperty = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUIStyle headerStyle = new GUIStyle();
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            headerStyle.margin = new RectOffset(5, 5, 10, 10);

            EditorGUILayout.LabelField(serializedObject.targetObject.name, headerStyle);

            if (GUILayout.Button("Select"))
            {
                // Open the rename dialog
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = serializedObject.targetObject;
                EditorGUIUtility.PingObject(serializedObject.targetObject);
            }

            EditorGUILayout.Space();
            // Draw description
            EditorGUILayout.PropertyField(descriptionProperty, GUILayout.Height(60));

            // Draw a separator
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            // Draw the rest of the properties
            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script", "stepName", "description" });

            serializedObject.ApplyModifiedProperties();
        }
    }
}
