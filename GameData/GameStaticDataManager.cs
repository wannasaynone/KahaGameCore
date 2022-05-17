using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameData
{
    public class GameStaticDataManager
    {
        private Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();

        public void Load<T>(IGameData[] gameDatas, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (isForceUpdate)
                {
                    Unload<T>();
                    RememberWithNewArray<T>(gameDatas);
                }
                else
                {
                    Debug.Log(nameof(T) + " is loaded, use Unload first or set isForceUpdate=true");
                }
            }
            else
            {
                RememberWithNewArray<T>(gameDatas);
            }
        }

        private void RememberWithNewArray<T>(IGameData[] array) where T : IGameData
        {
            IGameData[] newArray = new IGameData[array.Length];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = array[i];
            }

            m_gameData.Add(typeof(T), newArray);
        }

        public void Unload<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                m_gameData.Remove(typeof(T));
            }
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
                Debug.LogFormat("{0} can't be found, use Load<T> first", typeof(T).Name);
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
    }
}
