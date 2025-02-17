using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace KahaGameCore.Package.DialogueSystem
{
    [CustomEditor(typeof(Localizer))]
    public class LocalizerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Localizer localizer = (Localizer)target;
            SerializedProperty idProperty = serializedObject.FindProperty("id");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localized Content Preview", EditorStyles.boldLabel);

            string content = GetLocalizedContent(idProperty.intValue);
            EditorGUILayout.LabelField("en_us:", content);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private string GetLocalizedContent(int id)
        {
            LocalizeData[] localizeDataList;

            string filePath = Path.Combine(Application.dataPath, "Resources/Data/LocalizeData.txt");
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                localizeDataList = JsonFx.Json.JsonReader.Deserialize<LocalizeData[]>(jsonContent);
            }
            else
            {
                Debug.LogError("Localize data file not found at: " + filePath);
                localizeDataList = new LocalizeData[0];
            }

            LocalizeData data = new List<LocalizeData>(localizeDataList).Find(d => d.ID == id);
            return data != null ? data.en_us : "not found";
        }
    }
}
