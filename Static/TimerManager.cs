using UnityEngine;
using System;
using System.Collections.Generic;

namespace KahaGameCore
{
    public class TimerManager : MonoBehaviour
    {
        private static TimerManager m_instance = null;

        private class Timer
        {
            public float Time;
            public Action Action;
        }

        private static Dictionary<long, Timer> m_timers = new Dictionary<long, Timer>();
        private static List<long> m_waitForRemoveTimers = new List<long>();
        private static long m_currentID = 0;

        /// <summary>
        /// Schedule a method which will be excuted when time is up.
        /// </summary>
        /// <param name="time">wait for</param>
        /// <param name="action">method</param>
        /// <returns>timer id</returns>
        public static long Schedule(float time, Action action)
        {
            if (m_currentID + 1 > long.MaxValue)
            {
                m_currentID = 0;
            }
            else
            {
                m_currentID++;
            }

            if (m_instance == null)
            {
                DontDestroyOnLoad(new GameObject("[TimerManager]").AddComponent<TimerManager>());
            }

            m_timers.Add(m_currentID, new Timer() { Time = time, Action = action });

            return m_currentID;
        }

        public static void Cancel(long id)
        {
            m_timers.Remove(id);
        }

        public static void AddTime(long id, float time)
        {
            if (m_timers.ContainsKey(id))
            {
                m_timers[id].Time += time;
            }
        }

        private void Awake()
        {
            if (m_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            m_instance = this;
        }

        private void Update()
        {
            float _deltaTime = Time.deltaTime;
            List<long> _allTimerIds = new List<long>(m_timers.Keys);
            for (int i = 0; i < _allTimerIds.Count; i++)
            {
                if (m_timers[_allTimerIds[i]].Time > 0f)
                {
                    m_timers[_allTimerIds[i]].Time -= _deltaTime;
                    if (m_timers[_allTimerIds[i]].Time <= 0)
                    {
                        if (m_timers[_allTimerIds[i]].Action != null)
                        {
                            m_timers[_allTimerIds[i]].Action();
                        }
                        m_waitForRemoveTimers.Add(_allTimerIds[i]);
                    }
                }
            }

            for (int i = 0; i < m_waitForRemoveTimers.Count; i++)
            {
                m_timers.Remove(m_waitForRemoveTimers[i]);
            }

            if (m_waitForRemoveTimers.Count > 0)
            {
                m_waitForRemoveTimers.Clear();
            }
        }
    }
}
