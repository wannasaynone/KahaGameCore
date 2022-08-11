using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.GameData.Implemented
{
    public class GameStaticDataManager
    {
        private Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();

        public void Add<T>(IGameData[] gameDatas, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (isForceUpdate)
                {
                    Remove<T>();
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

        public void Add<T>(IGameStaticDataHandler handler, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (isForceUpdate)
                {
                    Remove<T>();
                    RememberWithNewArray<T>(handler.Load<T>() as IGameData[]);
                }
                else
                {
                    Debug.Log(nameof(T) + " is loaded, use Unload first or set isForceUpdate=true");
                }
            }
            else
            {
                RememberWithNewArray<T>(handler.Load<T>() as IGameData[]);
            }
        }

        public async System.Threading.Tasks.Task AddAsync<T>(IGameStaticDataHandler handler, bool isForceUpdate = false) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                if (isForceUpdate)
                {
                    Remove<T>();
                    T[] _data = await handler.LoadAsync<T>();
                    RememberWithNewArray<T>(_data as IGameData[]);
                }
                else
                {
                    Debug.Log(nameof(T) + " is loaded, use Unload first or set isForceUpdate=true");
                }
            }
            else
            {
                T[] _data = await handler.LoadAsync<T>();
                RememberWithNewArray<T>(_data as IGameData[]);
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

        public void Remove<T>() where T : IGameData
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
