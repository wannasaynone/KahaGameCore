using System;
using System.Collections.Generic;

namespace KahaGameCore.Actor
{
    public class GeneralValueContainer : IValueContainer
    {
        private Dictionary<string, int> baseStats = new Dictionary<string, int>();

        private class TempStat
        {
            public Guid guid;
            public string tag;
            public int value;
        }

        private List<TempStat> tempStats = new List<TempStat>();

        public Guid Add(string tag, int value)
        {
            Guid guid = Guid.NewGuid();
            TempStat tempStat = new TempStat
            {
                guid = guid,
                tag = tag,
                value = value
            };
            tempStats.Add(tempStat);
            return guid;
        }

        public void AddBase(string tag, int value)
        {
            if (baseStats.ContainsKey(tag))
            {
                baseStats[tag] += value;
            }
            else
            {
                baseStats.Add(tag, value);
            }
        }

        public void AddToTemp(Guid guid, int value)
        {
            TempStat tempStat = tempStats.Find(x => x.guid == guid);
            if (tempStat != null)
            {
                tempStat.value += value;
            }
        }

        public int GetTotal(string tag, bool baseOnly)
        {
            int total = 0;
            if (baseStats.ContainsKey(tag))
            {
                total += baseStats[tag];
            }

            if (!baseOnly)
            {
                for (int i = 0; i < tempStats.Count; i++)
                {
                    if (tempStats[i].tag == tag)
                    {
                        total += tempStats[i].value;
                    }
                }
            }

            return total;
        }

        public void SetBase(string tag, int value)
        {
            if (baseStats.ContainsKey(tag))
            {
                baseStats[tag] = value;
            }
            else
            {
                baseStats.Add(tag, value);
            }
        }

        public void SetTemp(Guid guid, int value)
        {
            TempStat tempStat = tempStats.Find(x => x.guid == guid);
            if (tempStat != null)
            {
                tempStat.value = value;
            }
        }
    }

    public class GeneralActor : IActor
    {
        public IValueContainer Stats { get; private set; } = new GeneralValueContainer();
    }
}