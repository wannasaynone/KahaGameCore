using System;

namespace KahaGameCore.Processor
{
    public class Processor<T> where T : IProcessable
    {
        private readonly T[] m_processableItems = null;

        private Action m_onDone = null;
        private Action m_onForceQuit = null;
        private int m_currentIndex = -1;

        public Processor(T[] items)
        {
            m_processableItems = items;
        }

        public void Start(Action onCompleted, Action onForceQuit)
        {
            if(m_currentIndex != -1)
            {
                return;
            }
            m_onDone = onCompleted;
            m_onForceQuit = onForceQuit;
            RunProcessableItems();
        }

        private void RunProcessableItems()
        {
            m_currentIndex++;
            if (m_currentIndex >= m_processableItems.Length)
            {
                m_currentIndex = -1;
                m_onDone?.Invoke();
                return;
            }

            if(m_processableItems[m_currentIndex] == null)
            {
                UnityEngine.Debug.LogErrorFormat("m_processableItems[{0}] == null", m_currentIndex);
                RunProcessableItems();
                return;
            }

            m_processableItems[m_currentIndex].Process(RunProcessableItems, m_onForceQuit);
        }
    }
}
