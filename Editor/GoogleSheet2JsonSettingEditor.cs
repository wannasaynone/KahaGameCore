using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.EditorTool
{
    [CustomEditor(typeof(GoogleSheet2JsonSetting))]
    public class GoogleSheet2JsonSettingEditor : Editor
    {
        private const string GET_DATA_URL = "https://sheets.googleapis.com/v4/spreadsheets/{0}/values/{1}?key=AIzaSyCPmWzKOWFJRYWa7V8eg5FenzIHL2IxFEE";

        private SerializedProperty m_sheetIDSerializedProperty;
        private SerializedProperty m_sheetNamesSerializedProperty;

        void OnEnable()
        {
            m_sheetIDSerializedProperty = serializedObject.FindProperty("sheetID");
            m_sheetNamesSerializedProperty = serializedObject.FindProperty("sheetNames");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            string _sheetID = m_sheetIDSerializedProperty.stringValue;
            System.Collections.Generic.List<string> _sheetNames = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(_sheetID) || m_sheetIDSerializedProperty.arraySize <= 0)
            {
                EditorGUILayout.HelpBox("Need input datas in setting", MessageType.Warning);
                return;
            }

            for (int i = 0; i < m_sheetNamesSerializedProperty.arraySize; i++)
            {
                string _sheetName = m_sheetNamesSerializedProperty.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrEmpty(_sheetName))
                {
                    EditorGUILayout.HelpBox("Need input datas in setting", MessageType.Warning);
                    return;
                }

                _sheetNames.Add(_sheetName);
            }

            if (GUILayout.Button("Start Convert"))
            {
                StartConvertTo(_sheetID, _sheetNames.ToArray());
            }
        }

        private void StartConvertTo(string id, string[] names)
        {
            string _path = AssetDatabase.GetAssetPath(target).Replace("/" + target.name + ".asset", "");

            Debug.Log("output path=" + _path);
            Debug.Log("----------------------------");

            for (int i = 0; i < names.Length; i++)
            {
                string _url = string.Format(GET_DATA_URL, id, names[i]);
                string result = "";

                Debug.Log(_url);

                HttpWebRequest request = WebRequest.Create(_url) as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using StreamReader sr = new StreamReader(response.GetResponseStream());
                    result = sr.ReadToEnd();
                    result = Convert(result);
                }

                string _outputFilePath = _path + "/" + names[i] + ".txt";
                if (File.Exists(_outputFilePath))
                {
                    File.WriteAllText(_outputFilePath, result);
                }
                else
                {
                    File.Create(_outputFilePath).Dispose();
                    File.WriteAllText(_outputFilePath, result);
                }

                Debug.Log("Text file is set");
            }

            Debug.Log("---End---");

            AssetDatabase.Refresh();
        }

        [System.Serializable]
        private class RawData
        {
            public string range;
            public string majorDimension;
            public List<List<string>> values;
        }

        private static string Convert(string csvData)
        {
            RawData rawData = JsonFx.Json.JsonReader.Deserialize<RawData>(csvData);
            List<string> keys = rawData.values[0];

            List<Dictionary<string, object>> modifiers = new List<Dictionary<string, object>>();

            for (int i = 1; i < rawData.values.Count; i++)
            {
                List<string> values = rawData.values[i];

                if (values == null || values.Count <= 0)
                    continue;

                Dictionary<string, object> modifier = new Dictionary<string, object>();

                for (int j = 0; j < keys.Count; j++)
                {
                    if (string.IsNullOrEmpty(keys[j]))
                    {
                        continue;
                    }

                    if (keys[j].Contains("NOEX_"))
                    {
                        continue;
                    }

                    if (j >= values.Count)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(values[j]))
                    {
                        continue;
                    }

                    if (int.TryParse(values[j], System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowLeadingSign, null, out int intValue))
                    {
                        modifier.Add(keys[j], intValue);
                    }
                    else if (long.TryParse(values[j], System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowLeadingSign, null, out long longValue))
                    {
                        modifier.Add(keys[j], longValue);
                    }
                    else if (float.TryParse(values[j], System.Globalization.NumberStyles.None | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint, null, out float floatValue))
                    {
                        modifier.Add(keys[j], floatValue);
                    }
                    else
                    {
                        modifier.Add(keys[j], values[j]);
                    }
                }

                if (modifier.Count > 0)
                    modifiers.Add(modifier);
            }

            return JsonFx.Json.JsonWriter.Serialize(modifiers);
        }
    }
}
