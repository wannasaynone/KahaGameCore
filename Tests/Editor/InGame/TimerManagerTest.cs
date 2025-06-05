using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using KahaGameCore.Common;

namespace KahaGameCore.Tests
{
    public partial class TimerManager
    {
        // Expose manual update for tests
        public static void ManualUpdate(float deltaTime)
        {
            List<long> _allTimerIds = new List<long>(m_timers.Keys);
            for (int i = 0; i < _allTimerIds.Count; i++)
            {
                if (!m_timers.ContainsKey(_allTimerIds[i]))
                    continue;

                if (m_timers[_allTimerIds[i]].time > 0f)
                {
                    m_timers[_allTimerIds[i]].time -= deltaTime;
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

        public static void ClearAll()
        {
            m_timers.Clear();
            m_waitForRemoveTimers.Clear();
            m_currentID = 0;
            if (m_instance != null)
            {
                GameObject.DestroyImmediate(m_instance.gameObject);
                m_instance = null;
            }
        }
    }

    public class TimerManagerTest
    {
        [SetUp]
        public void SetUp()
        {
            TimerManager.ClearAll();
        }

        [Test]
        public void Timer_callbacks_and_removal()
        {
            bool updatedCalled = false;
            bool endedCalled = false;
            long id = TimerManager.Schedule(1f, () => { endedCalled = true; }, (t) => { updatedCalled = true; });

            TimerManager.ManualUpdate(0.5f);
            Assert.IsTrue(updatedCalled);
            Assert.IsFalse(endedCalled);
            Assert.IsTrue(Mathf.Approximately(TimerManager.GetTime(id), 0.5f));

            updatedCalled = false;
            TimerManager.ManualUpdate(0.5f);
            Assert.IsTrue(updatedCalled);
            Assert.IsTrue(endedCalled);
            Assert.AreEqual(-1f, TimerManager.GetTime(id));
        }
    }
}
