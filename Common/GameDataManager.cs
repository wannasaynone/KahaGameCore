using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using JsonFx.Json;

namespace KahaGameCore.Common
{
    public class GameDataManager
    {
        private Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();

        public T[] DeserializeGameData<T>(string json, bool isForceUpdate = false) where T : IGameData
        {
            json = json.Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\v", "").Replace("\b", "");

            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (!isForceUpdate)
                {
                    return GetAllGameData<T>();
                }

                T[] data = JsonReader.Deserialize<T[]>(json);
                m_gameData[typeof(T)] = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    m_gameData[typeof(T)][i] = data[i];
                }

                return GetAllGameData<T>();
            }
            else
            {
                T[] data = JsonReader.Deserialize<T[]>(json);
                IGameData[] _gameData = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    _gameData[i] = data[i];
                }
                m_gameData.Add(typeof(T), _gameData);

                return GetAllGameData<T>();
            }
        }

        public void Unload<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                m_gameData.Remove(typeof(T));
            }
        }

        public string GetDefaultDataFilePath<T>()
        {
            string filePath = GetDefaultDataFolderPath();

            if (filePath[filePath.Length - 1] != '/')
            {
                filePath += "/";
            }

            string fileName = GetFileName<T>();

            return filePath + fileName;
        }

        public bool DeleteSave<T>()
        {
            string filePath = GetDefaultDataFilePath<T>();

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

        public T LoadSave<T>()
        {
            string filePath = GetDefaultDataFilePath<T>();

            if (File.Exists(filePath))
            {
                string _jsonString = File.ReadAllText(filePath);
                return JsonReader.Deserialize<T>(_jsonString);
            }
            else
            {
                return default;
            }
        }

        public void Save(object saveObj)
        {
            string path = GetDefaultDataFolderPath();

            if (path[path.Length - 1] != '/')
            {
                path += "/";
            }

            string jsonData = "";
            jsonData = JsonWriter.Serialize(saveObj);

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

        public T GetGameData<T>(int id) where T : IGameData
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
                Debug.LogFormat("{0} can't be found, use LoadGameData first", typeof(T).Name);
            }

            return default;
        }

        public T[] GetAllGameData<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                T[] gameDatas = new T[m_gameData[typeof(T)].Length];
                for (int i = 0; i < m_gameData[typeof(T)].Length; i++)
                {
                    gameDatas[i] = (T)m_gameData[typeof(T)][i];
                }
                return gameDatas;
            }

            return default;
        }

        private string GetFileName<T>()
        {
            string[] fullName = typeof(T).FullName.ToString().Split('.');
            string[] fullClassName = fullName[fullName.Length - 1].Split('+');
            return fullClassName[fullClassName.Length - 1].Replace("[]", "") + ".txt";
        }

        private string GetDefaultDataFolderPath()
        {
            return Application.persistentDataPath + "/Resources/Datas/";
        }
    }
}
