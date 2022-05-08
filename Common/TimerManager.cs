using UnityEngine;
using System;
using System.Collections.Generic;

namespace KahaGameCore.Common
{
    public class TimerManager : MonoBehaviour
    {
        private static TimerManager m_instance = null;

        private class Timer
        {
            public float time;
            public Action onTimeEnded;
            public Action<float> onTimeUpdated;
        }

        private static Dictionary<long, Timer> m_timers = new Dictionary<long, Timer>();
        private static List<long> m_waitForRemoveTimers = new List<long>();
        private static long m_currentID = 0;

        /// <summary>
        /// return timer's id
        /// </summary>
        public static long Schedule(float time, Action onTimeEnded, Action<float> onTimeUpdated = null)
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

            m_timers.Add(m_currentID, new Timer() { time = time, onTimeEnded = onTimeEnded, onTimeUpdated = onTimeUpdated });

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
                m_timers[id].time += time;
            }
        }

        public static void SetTime(long id, float newTime)
        {
            if (m_timers.ContainsKey(id))
            {
                m_timers[id].time = newTime;
            }
        }

        public static float GetTime(long id)
        {
            if (m_timers.ContainsKey(id))
            {
                return m_timers[id].time;
            }

            return -1f;
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
                if (!m_timers.ContainsKey(_allTimerIds[i]))
                    continue;

                if (m_timers[_allTimerIds[i]].time > 0f)
                {
                    m_timers[_allTimerIds[i]].time -= _deltaTime;
                    m_timers[_allTimerIds[i]].onTimeUpdated?.Invoke(m_timers[_allTimerIds[i]].time);

                    if (m_timers[_allTimerIds[i]].time <= 0)
                    {
                        m_timers[_allTimerIds[i]].onTimeEnded?.Invoke();
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
