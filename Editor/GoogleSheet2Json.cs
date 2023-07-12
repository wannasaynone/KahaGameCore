using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Minidragon.EditorTool
{
    public class GoogleSheet2Json : EditorWindow
    {
        private const string GET_DATA_URL = "http://172-105-200-91.ip.linodeusercontent.com:8080/api/v1/sheetData/?id={0}&name={1}";

        private static Object m_folder;
        private static GoogleSheet2JsonSetting m_settingInstance;

        [SerializeField] private GoogleSheet2JsonSetting m_setting;

        private void OnEnable()
        {
            m_setting = m_settingInstance;
        }

        private void OnGUI()
        {
            m_folder = EditorGUILayout.ObjectField("Output Folder", m_folder, typeof(DefaultAsset), false);

            if (m_folder == null)
            {
                EditorGUILayout.HelpBox("Please insert a folder", MessageType.Info);
                return;
            }

            if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(m_folder)))
            {
                EditorGUILayout.HelpBox("Inserted asset must be a folder", MessageType.Warning);
                return;
            }

            SerializedObject _serializedObj = new SerializedObject(this);
            DrawProperty(_serializedObj, nameof(m_setting));

            if (m_settingInstance == null || m_settingInstance != m_setting)
            {
                m_settingInstance = m_setting;
            }

            if (m_settingInstance == null)
            {
                EditorGUILayout.HelpBox("Need input setting", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(m_settingInstance.sheetID)
                || m_settingInstance.sheetNames == null
                || m_settingInstance.sheetNames.Length <= 0)
            {
                EditorGUILayout.HelpBox("Need input datas in setting", MessageType.Warning);
                return;
            }

            for (int i = 0; i < m_settingInstance.sheetNames.Length; i++)
            {
                if (string.IsNullOrEmpty(m_settingInstance.sheetNames[i]))
                {
                    EditorGUILayout.HelpBox("Setting is having empty data", MessageType.Warning);
                    return;
                }
            }

            if (GUILayout.Button("Start Convert"))
            {
                StartConvertTo();
            }
        }

        private void DrawProperty(SerializedObject serializedObject, string fieldName)
        {
            SerializedProperty _property = serializedObject.FindProperty(fieldName);

            EditorGUILayout.PropertyField(_property);
            _property.serializedObject.ApplyModifiedProperties();
        }

        private void StartConvertTo()
        {
            string _path = AssetDatabase.GetAssetPath(m_folder);

            Debug.Log("output path=" + _path);

            Debug.Log("----------------------------");

            for (int i = 0; i < m_settingInstance.sheetNames.Length; i++)
            {
                string _url = string.Format(GET_DATA_URL, m_setting.sheetID, m_settingInstance.sheetNames[i]);
                string result = "";

                Debug.Log(_url);

                HttpWebRequest request = WebRequest.Create(_url) as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        result = sr.ReadToEnd();
                    }
                }

                string _outputFilePath = _path + "/" + m_setting.sheetNames[i] + ".txt";
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