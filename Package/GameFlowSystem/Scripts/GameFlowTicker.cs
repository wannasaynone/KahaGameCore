using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem
{
    public class GameFlowTicker : MonoBehaviour
    {
        private List<GameFlowBase> m_gameFlowList = new List<GameFlowBase>();

        private void Update()
        {
            for (int i = 0; i < m_gameFlowList.Count; i++)
            {
                m_gameFlowList[i].Update();
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < m_gameFlowList.Count; i++)
            {
                m_gameFlowList[i].FixedUpdate();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_gameFlowList.Count; i++)
            {
                m_gameFlowList[i].LateUpdate();
            }
        }

        public void AddGameFlow(GameFlowBase gameFlow)
        {
            m_gameFlowList.Add(gameFlow);
        }

        public void RemoveGameFlow(GameFlowBase gameFlow)
        {
            m_gameFlowList.Remove(gameFlow);
        }
    }
}