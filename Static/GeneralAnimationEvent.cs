using UnityEngine;
using System;
using System.Collections.Generic;

namespace KahaGameCore
{
    public class GeneralAnimationEvent : MonoBehaviour
    {
        [SerializeField] private bool m_enable = true;
        [SerializeField] private GameObject m_root = null;

        private void Disable()
        {
            if (!m_enable)
            {
                return;
            }
            m_root.SetActive(false);
        }
    }
}

