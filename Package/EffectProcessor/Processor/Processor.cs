using System;

namespace KahaGameCore.Package.EffectProcessor.Processor
{
    public class Processor<T> where T : IProcessable
    {
        private readonly T[] m_processableItems = null;

        private Action m_onDone = null;
        private Action m_onForceQuit = null;
        private int m_currentIndex = -1;
        private bool m_isForceQuitting = false;

        public Processor(T[] items)
        {
            m_processableItems = items;
        }

        public void Start(Action onCompleted, Action onForceQuit)
        {
            if (m_currentIndex != -1)
            {
                return;
            }
            m_onDone = onCompleted;
            m_onForceQuit = onForceQuit;
            m_isForceQuitting = false;
            RunProcessableItems();
        }

        /// <summary>
        /// Force quits the current processing and triggers the onForceQuit callback
        /// </summary>
        public void ForceQuit()
        {
            if (m_onForceQuit != null && !m_isForceQuitting)
            {
                m_isForceQuitting = true;
                Action forceQuitCallback = m_onForceQuit;

                // Reset state before invoking callback to prevent re-entry issues
                m_currentIndex = -1;
                m_onForceQuit = null;
                m_onDone = null;

                // Invoke the callback
                forceQuitCallback.Invoke();
            }
        }

        private void RunProcessableItems()
        {
            // Don't continue processing if we're force quitting
            if (m_isForceQuitting)
            {
                return;
            }

            m_currentIndex++;
            if (m_currentIndex >= m_processableItems.Length)
            {
                m_currentIndex = -1;
                Action onDone = m_onDone;
                m_onDone = null;
                m_onForceQuit = null;
                onDone?.Invoke();
                return;
            }

            if (m_processableItems[m_currentIndex] == null)
            {
                UnityEngine.Debug.LogErrorFormat("m_processableItems[{0}] == null", m_currentIndex);
                RunProcessableItems();
                return;
            }

            m_processableItems[m_currentIndex].Process(RunProcessableItems, m_onForceQuit);
        }
    }
}
