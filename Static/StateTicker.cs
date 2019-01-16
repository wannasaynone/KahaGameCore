using UnityEngine;
using KahaGameCore.Manager.State;

namespace KahaGameCore
{
    public class StateTicker : MonoBehaviour
    {
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

    }
}
