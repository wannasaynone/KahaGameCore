using System;
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

        public static void LoadGameData<T>(string path) where T : IGameData
        {
            T[] data = JsonReader.Deserialize<T[]>(Resources.Load<TextAsset>(path).text);
            if (m_gameData.ContainsKey(typeof(T)))
            {
                m_gameData[typeof(T)] = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    m_gameData[typeof(T)][i] = data[i];
                }
            }
            else
            {
                IGameData[] _gameData = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    _gameData[i] = data[i];
                }
                m_gameData.Add(typeof(T), _gameData);
            }
        }

        // TODO: 只有一個obj也要輸出成json array
        public static void SaveData(object saveObj)
        {
            string _jsonData = JsonWriter.Serialize(saveObj);
            if(!System.IO.Directory.Exists(Application.dataPath + "/Resources/Datas/"))
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources/Datas/");
            }
            string[] _fullName = saveObj.ToString().Split('.');
            string[] _fullClassName = _fullName[_fullName.Length - 1].Split('+');
            System.IO.File.WriteAllText(Application.dataPath + "/Resources/Datas/" + _fullClassName[_fullClassName.Length-1] + ".txt", _jsonData);
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

        public static void LoadScriptableObjectData<T>(string path) where T : ScriptableObject
        {
            T _obj = GameResourcesManager.LoadResource<T>(path);
            if(_obj != null)
            {
                if(m_typeToSO.ContainsKey(typeof(T)))
                {
                    Debug.LogFormat("{0} is existed, but is trying to load it, check it");
                    return;
                }
                else
                {
                    m_typeToSO.Add(typeof(T), _obj);
                }
            }
        }

        public static T GetScriptableObjectData<T>() where T : ScriptableObject
        {
            if (GameResourcesManager.CurrentState != GameResourcesManager.State.Inited)
            {
                Debug.LogError("Bundle not inited");
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
