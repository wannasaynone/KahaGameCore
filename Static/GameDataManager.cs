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

        public static void LoadGameData<T>(string fileName, Action onLoaded = null, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if(!isForceUpdate)
                {
                    if(onLoaded != null)
                    {
                        onLoaded();
                    }
                    return;
                }

                UpdateGameData<T>(fileName, onLoaded);
            }
            else
            {
                UpdateGameData<T>(fileName, onLoaded);
            }
        }

        private static void UpdateGameData<T>(string name, Action onUpdated) where T : IGameData
        {
            GetJsonString(name,
            delegate (string data)
            {
                T[] _data = JsonReader.Deserialize<T[]>(data);
                IGameData[] _gameData = new IGameData[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    _gameData[i] = _data[i];
                }
                if(m_gameData.ContainsKey(typeof(T)))
                {
                    m_gameData[typeof(T)] = _gameData;
                }
                else
                {
                    m_gameData.Add(typeof(T), _gameData);
                }

                if(onUpdated != null)
                {
                    onUpdated();
                }
            });
        }

        private static void GetJsonString(string name, Action<string> onJsonLoaded)
        {
            GameResourcesManager.LoadResource(name,
                delegate (TextAsset asset)
                {
                    if (asset == null)
                    {
                        string _allPath = Application.persistentDataPath + "/Resources/Datas/" + name + ".txt";
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (File.Exists(_allPath))
                            {
                                if(onJsonLoaded != null)
                                {
                                    onJsonLoaded(File.ReadAllText(_allPath));
                                } 
                            }
                            else
                            {
                                Debug.LogError("[GameDataManager] path is not existed:" + _allPath);
                            }
                        }
                        else
                        {
                            Debug.LogError("[GameDataManager] path is empty");
                        }
                    }
                    else
                    {
                        if (onJsonLoaded != null)
                        {
                            onJsonLoaded(asset.text);
                        }
                    }
                });
        }

        public static void SaveData(object[] saveObj, string path = null)
        {
            if(string.IsNullOrEmpty(path))
            {
                path = Application.persistentDataPath + "/Resources/Datas/";
            }

            if(path[path.Length - 1] != '/')
            {
                path += "/";
            }

            string _jsonData = JsonWriter.Serialize(saveObj);
            if(!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            string[] _fullName = saveObj.ToString().Split('.');
            string[] _fullClassName = _fullName[_fullName.Length - 1].Split('+');
            System.IO.File.WriteAllText(path + _fullClassName[_fullClassName.Length-1].Replace("[]","") + ".txt", _jsonData);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
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
                Debug.LogErrorFormat("[GameDataManager] {0} can't be found, use LoadGameData first", typeof(T).Name);
            }

            return default(T);
        }

        public static T[] GetAllGameData<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                return m_gameData[typeof(T)] as T[];
            }

            return default(T[]);
        }
    }
}
