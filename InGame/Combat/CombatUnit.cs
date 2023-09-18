using System.Collections.Generic;

namespace KahaGameCore.Combat
{
    public class CombatUnit : IActor
    {
        public class CombatUnitStats : IValueContainer
        {
            private List<ValueObject> m_baseStats;
            private List<ValueObject> m_tempStats = new List<ValueObject>();

            public CombatUnitStats(ValueObject[] baseStats)
            {
                m_baseStats = new List<ValueObject>();
                for (int i = 0; i < baseStats.Length; i++)
                {
                    m_baseStats.Add(new ValueObject(baseStats[i].Tag, baseStats[i].Value));
                }
            }

            public int GetTotal(string tag, bool onlyBase)
            {
                int total = 0;
                for (int i = 0; i < m_baseStats.Count; i++)
                {
                    if (m_baseStats[i].Tag == tag)
                    {
                        total += m_baseStats[i].Value;
                    }
                }
                if (!onlyBase)
                {
                    for (int i = 0; i < m_tempStats.Count; i++)
                    {
                        if (m_tempStats[i].Tag == tag)
                        {
                            total += m_tempStats[i].Value;
                        }
                    }
                }
                return total;
            }

            public void Add(ValueObject valueObject)
            {
                ValueObject findSame = m_tempStats.Find(x => x.UID == valueObject.UID);

                if (findSame == null)
                {
                    m_tempStats.Add(valueObject);
                }
            }

            public void Remove(ValueObject valueObject)
            {
                ValueObject findSame = m_tempStats.Find(x => x.UID == valueObject.UID);

                if (findSame != null)
                {
                    m_tempStats.Remove(valueObject);
                }
            }

            public void Add(string tag, int value)
            {
                Add(new ValueObject(tag, value));
            }
        }


        public IValueContainer Stats { get; private set; }

        public ISkillTrigger SkillTrigger => throw new System.NotImplementedException();

        public CombatUnit(ValueObject[] baseStats)
        {
            Stats = new CombatUnitStats(baseStats);
        }

        public void Add(ValueObject valueObject)
        {
            ((CombatUnitStats)Stats).Add(valueObject);
        }

        public void Remove(ValueObject valueObject)
        {
            ((CombatUnitStats)Stats).Remove(valueObject);
        }
    }
}
