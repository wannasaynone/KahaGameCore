using System.IO;
using UnityEngine;

namespace KahaGameCore.GameData.Implemented
{
    public class JsonSaveDataHandler
    {
        private readonly IJsonWriter m_writer;
        private readonly IJsonReader m_reader;

        public JsonSaveDataHandler(IJsonWriter writer, IJsonReader reader)
        {
            m_writer = writer;
            m_reader = reader;
        }

        private static string GetDefaultDataFilePath<T>(int index)
        {
            string filePath = GetDefaultDataFolderPath();

            if (filePath[filePath.Length - 1] != '/')
            {
                filePath += "/";
            }

            string fileName = GetFileName<T>();

            return filePath + index + "/" + fileName;
        }

        public static bool IsSaveExist<T>(int index)
        {
            string filePath = GetDefaultDataFilePath<T>(index);
            return File.Exists(filePath);
        }

        public bool DeleteSave<T>(int index)
        {
            string filePath = GetDefaultDataFilePath<T>(index);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("Json Data Deleted: " + filePath);
                return true;
            }
            else
            {
                return false;
            }
        }

        public T LoadSave<T>(int index)
        {
            string filePath = GetDefaultDataFilePath<T>(index);

            if (File.Exists(filePath))
            {
                string _jsonString = File.ReadAllText(filePath);
                return m_reader.Read<T>(_jsonString);
            }
            else
            {
                return default;
            }
        }

        public void Save(object saveObj, int index)
        {
            string path = GetDefaultDataFolderPath() + index + "/";

            if (path[path.Length - 1] != '/')
            {
                path += "/";
            }

            string jsonData = m_writer.Write(saveObj);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] _fullName = saveObj.ToString().Split('.');
            string[] _fullClassName = _fullName[_fullName.Length - 1].Split('+');
            File.WriteAllText(path + _fullClassName[_fullClassName.Length - 1].Replace("[]", "") + ".txt", jsonData);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log("Saved:" + path);
        }

        private static string GetFileName<T>()
        {
            string[] fullName = typeof(T).FullName.ToString().Split('.');
            string[] fullClassName = fullName[fullName.Length - 1].Split('+');
            return fullClassName[fullClassName.Length - 1].Replace("[]", "") + ".txt";
        }

        private static string GetDefaultDataFolderPath()
        {
            return Application.persistentDataPath + "/Resources/Datas/";
        }

    }
}