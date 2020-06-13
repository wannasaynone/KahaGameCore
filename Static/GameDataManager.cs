using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using JsonFx.Json;
using KahaGameCore.Interface;

namespace KahaGameCore.Static
{
    public static class GameDataManager
    {
        private static Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();
        private static Dictionary<Type, ScriptableObject> m_typeToSO = new Dictionary<Type, ScriptableObject>();

        public static T[] LoadGameData<T>(string path, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (!isForceUpdate)
                {
                    return GetAllGameData<T>();
                }

                T[] _data = JsonReader.Deserialize<T[]>(GetJsonString(path));
                m_gameData[typeof(T)] = new IGameData[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    m_gameData[typeof(T)][i] = _data[i];
                }

                return GetAllGameData<T>();
            }
            else
            {
                T[] _data = JsonReader.Deserialize<T[]>(GetJsonString(path));
                IGameData[] _gameData = new IGameData[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    _gameData[i] = _data[i];
                }
                m_gameData.Add(typeof(T), _gameData);

                return GetAllGameData<T>();
            }
        }

        private static string GetJsonString(string path)
        {
            TextAsset _dataTextAsset = Resources.Load<TextAsset>(path);
            if (_dataTextAsset == null)
            {
                string _allPath = Application.persistentDataPath + "/Resources/Datas/" + path + ".txt";
                if (!string.IsNullOrEmpty(path))
                {
                    if (File.Exists(_allPath))
                    {
                        return File.ReadAllText(_allPath);
                    }
                }
                Debug.LogErrorFormat("Can't find json file at {0} or {1} while getting json string.", path, _allPath);
                return null;
            }
            return _dataTextAsset.text;
        }

        public static T LoadGameData<T>(string path, int id, bool isForceUpdate = false) where T : IGameData
        {
            LoadGameData<T>(path, isForceUpdate);
            return GetGameData<T>(id);
        }

        public static string GetDefaultDataFolderPath()
        {
            return Application.persistentDataPath + "/Resources/Datas/";
        }

        public static string GetDefaultDataFilePath<T>()
        {
            string _filePath = GetDefaultDataFolderPath();

            if (_filePath[_filePath.Length - 1] != '/')
            {
                _filePath += "/";
            }

            string[] _fullName = typeof(T).FullName.ToString().Split('.');
            string[] _fullClassName = _fullName[_fullName.Length - 1].Split('+');
            string _fileName = _fullClassName[_fullClassName.Length - 1].Replace("[]", "");

            return _filePath + _fileName + ".txt";
        }

        public static bool DeleteJsonData<T>(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = GetDefaultDataFilePath<T>();
            }

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

        public static T LoadJsonData<T>(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = GetDefaultDataFilePath<T>();
            }

            if (File.Exists(filePath))
            {
                string _jsonString = File.ReadAllText(filePath);
                return JsonReader.Deserialize<T>(_jsonString);
            }
            else
            {
                return default(T);
            }
        }

        public static void SaveData(object[] saveObj, string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = GetDefaultDataFolderPath();
            }

            if (path[path.Length - 1] != '/')
            {
                path += "/";
            }

            string _jsonData = "";

            if (saveObj.Length > 1)
            {
                _jsonData = JsonWriter.Serialize(saveObj);
            }
            else
            {
                _jsonData = JsonWriter.Serialize(saveObj[0]);
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] _fullName = saveObj.ToString().Split('.');
            string[] _fullClassName = _fullName[_fullName.Length - 1].Split('+');
            File.WriteAllText(path + _fullClassName[_fullClassName.Length - 1].Replace("[]", "") + ".txt", _jsonData);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log("Saved:" + path);
        }

        public static T GetGameData<T>(int id) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                for (int i = 0; i < m_gameData[typeof(T)].Length; i++)
                {
                    if (m_gameData[typeof(T)][i].ID == id)
                    {
                        return (T)m_gameData[typeof(T)][i];
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("{0} can't be found, use LoadGameData first", typeof(T).Name);
            }

            return default(T);
        }

        public static T[] GetAllGameData<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                T[] _gameDatas = new T[m_gameData[typeof(T)].Length];
                for (int i = 0; i < m_gameData[typeof(T)].Length; i++)
                {
                    _gameDatas[i] = (T)m_gameData[typeof(T)][i];
                }
                return _gameDatas;
            }

            return default(T[]);
        }

        /// <summary>
        /// Load ScriptableObject-Form-Data from path. Only can be used when ScriptableObject is one and only.
        /// </summary>
        public static void LoadScriptableObjectData<T>(string path) where T : ScriptableObject
        {
            T _obj = GameResourcesManager.LoadResource<T>(path);
            if (_obj != null)
            {
                if (m_typeToSO.ContainsKey(typeof(T)))
                {
                    Debug.LogErrorFormat("{0} is existed, but is trying to load it, check it", typeof(T).Name);
                    return;
                }
                else
                {
                    m_typeToSO.Add(typeof(T), _obj);
                }
            }
        }
    }
}
