﻿using UnityEngine;

namespace KahaGameCore.Common
{
    public class GeneralCoroutineRunner : MonoBehaviour
    {
        public static GeneralCoroutineRunner Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new GameObject("[General Coroutine Runner]").AddComponent<GeneralCoroutineRunner>();
                    DontDestroyOnLoad(m_instance.gameObject);
                }

                return m_instance;
            }
        }
        private static GeneralCoroutineRunner m_instance = null;

        private void Awake()
        {
            if(m_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            m_instance = this;
            DontDestroyOnLoad(this);
        }
    }
}
