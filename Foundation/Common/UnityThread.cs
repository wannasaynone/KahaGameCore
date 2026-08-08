using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KahaGameCore.Common
{
    public class UnityThread : MonoBehaviour
    {
        public static UnityThread Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private static List<Action> m_actions = new List<Action>();
        private List<Action> m_copiedActions = new List<Action>();
        private volatile static bool m_do;

        public static void Do(Action action)
        {
            lock (m_actions)
            {
                m_actions.Add(action);
                m_do = true;
            }
        }

        private void Update()
        {
            if (!m_do)
                return;

            lock (m_actions)
            {
                m_copiedActions.AddRange(m_actions);
                m_actions.Clear();
                m_do = false;
            }

            if (m_copiedActions.Count > 0)
            {
                for (int i = 0; i < m_copiedActions.Count; i++)
                {
                    m_copiedActions[i]?.Invoke();
                }
                m_copiedActions.Clear();
            }
        }
    }
}
