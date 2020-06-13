using System;
using KahaGameCore.Interface;

namespace KahaGameCore.Common
{
    public class Processer<T> where T : IProcessable
    {
        private readonly T[] m_processableItems = null;

        private Action m_onDone = null;
        private int m_currentIndex = -1;

        public Processer(T[] items)
        {
            m_processableItems = items;
        }

        public void Start(Action onCompleted)
        {
            if(m_currentIndex != -1)
            {
                return;
            }
            m_onDone = onCompleted;
            RunProcessableItems();
        }

        private void RunProcessableItems()
        {
            m_currentIndex++;
            if (m_currentIndex >= m_processableItems.Length)
            {
                if (m_onDone != null)
                {
                    m_onDone();
                }

                m_currentIndex = -1;
                return;
            }

            if(m_processableItems[m_currentIndex] == null)
            {
                UnityEngine.Debug.LogErrorFormat("m_processableItems[{0}] == null", m_currentIndex);
                RunProcessableItems();
                return;
            }

            m_processableItems[m_currentIndex].Process(RunProcessableItems);
        }
    }
}
