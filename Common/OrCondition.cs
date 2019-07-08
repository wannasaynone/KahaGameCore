using UnityEngine;
using KahaGameCore.Interface;

namespace KahaGameCore.Common
{
    public class OrCondition : ConditionBase
    {
        [SerializeField] private ConditionBase m_conditionA = null;
        [SerializeField] private ConditionBase m_conditionB = null;

        public override bool IsTrue()
        {
            if (m_conditionA == null || m_conditionB == null)
            {
                return true;
            }

            return m_conditionA.IsTrue() || m_conditionB.IsTrue();
        }
    }
}
