﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonFx.Json;
using KahaGameCore.Data;

namespace KahaGameCore
{
    public static class GameDataManager
    {
        private static Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();
        private static Dictionary<Type, ScriptableObject> m_typeToSO = new Dictionary<Type, ScriptableObject>();

        public static void LoadGameData<T>(string path, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if(!isForceUpdate)
                {
                    return;
                }

                T[] _data = JsonReader.Deserialize<T[]>(GetJsonString(path));
                m_gameData[typeof(T)] = new IGameData[_data.Length];
                for (int i = 0; i < _data.Length; i++)
                {
                    m_gameData[typeof(T)][i] = _data[i];
                }
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
            }
        }

        private static string GetJsonString(string path)
        {
            TextAsset _dataTextAsset = Resources.Load<TextAsset>(path);
            if (_dataTextAsset == null)
            {
                // TODO: add Application.persistentDataPath checking flow here
                Debug.LogErrorFormat("Can't find text asset at {0} while getting json string.", path);
                return null;
            }
            return _dataTextAsset.text;
        }

        public static T LoadGameData<T>(string path, int id, bool isForceUpdate = false) where T : IGameData
        {
            LoadGameData<T>(path, isForceUpdate);
            return GetGameData<T>(id);
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
            if(_obj != null)
            {
                if(m_typeToSO.ContainsKey(typeof(T)))
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

        /// <summary>
        /// Get ScriptableObject-Form-Data which is one and only.
        /// </summary>
        public static T GetScriptableObjectData<T>() where T : ScriptableObject
        {
            if (GameResourcesManager.CurrentState != GameResourcesManager.State.Inited)
            {
                Debug.LogError("Bundle not inited");
                return null;
            }

            if(m_typeToSO.ContainsKey(typeof(T)))
            {
                return m_typeToSO[typeof(T)] as T;
            }
            else
            {
                List<ScriptableObject> _allSO = GameResourcesManager.LoadAllBundleAssets<ScriptableObject>();
                for(int i = 0; i < _allSO.Count; i++)
                {
                    if (TryRegisterSO<T>(_allSO[i]))
                    {
                        return m_typeToSO[typeof(T)] as T;
                    }
                }

                Debug.LogErrorFormat("Can't get ScriptableObject:{0}", typeof(T).Name);

                return null;
            }
        }

        private static bool TryRegisterSO<T>(ScriptableObject obj) where T : ScriptableObject
        {
            if(obj == null)
            {
                return false;
            }

            if (obj is T)
            {
                if (m_typeToSO.ContainsKey(typeof(T)))
                {
                    Debug.LogFormat("{0} is existed, but is trying to load it, check it");
                    return false;
                }
                else
                {
                    m_typeToSO.Add(typeof(T), obj);
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
