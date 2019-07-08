using UnityEngine;
using KahaGameCore.Interface;
using System.Collections.Generic;

namespace KahaGameCore.Static
{
    public class StateTicker : MonoBehaviour
    {
        private static List<StateTicker> m_allStateTickers = new List<StateTicker>();

        public static StateTicker GetUseableTicker()
        {
            for(int i = 0; i < m_allStateTickers.Count; i++)
            {
                if(!m_allStateTickers[i].gameObject.activeSelf)
                {
                    m_allStateTickers[i].gameObject.SetActive(true);
                    return m_allStateTickers[i];
                }
            }

            StateTicker _newTicker = new GameObject("[State Ticker]").AddComponent<StateTicker>();
            m_allStateTickers.Add(_newTicker);
            return _newTicker;
        }

        private StateBase m_currentState = null;

        public void StartTick(StateBase state)
        {
            m_currentState = state;
        }

        private void Update()
        {
            if(m_currentState != null)
            {
                m_currentState.Tick();
            }
        }

        private void OnDisable()
        {
            m_currentState = null;
        }
    }
}
