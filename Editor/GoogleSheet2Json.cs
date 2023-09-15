using System.IO;

using System.Net;

using UnityEditor;
using UnityEngine;

namespace KahaGameCore.EditorTool
{
    [CustomEditor(typeof(GoogleSheet2JsonSetting))]
    public class GoogleSheet2JsonSettingEditor : Editor
    {
        private const string GET_DATA_URL = "http://172-105-200-91.ip.linodeusercontent.com:8080/api/v1/sheetData/?id={0}&name={1}&pretty=1";

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

    }
}
