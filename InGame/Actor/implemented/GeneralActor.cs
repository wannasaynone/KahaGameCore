using System;
using System.Collections.Generic;

namespace KahaGameCore.Actor
{
    public class GeneralValueContainer : IValueContainer
    {
        private class BaseStat
        {
            public string tag;
            public int value;
        }

        private class StringKeyValuePair
        {
            public string key;
            public string value;
        }

        private List<BaseStat> baseStats = new List<BaseStat>();
        private List<StringKeyValuePair> stringKeyValuePairs = new List<StringKeyValuePair>();

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
            BaseStat baseStat = baseStats.Find(x => x.tag == tag);
            if (baseStat != null)
            {
                baseStat.value += value;
            }
            else
            {
                baseStat = new BaseStat
                {
                    tag = tag,
                    value = value
                };
                baseStats.Add(baseStat);
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

            BaseStat baseStat = baseStats.Find(x => x.tag == tag);
            if (baseStat != null)
            {
                total += baseStat.value;
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
            BaseStat baseStat = baseStats.Find(x => x.tag == tag);
            if (baseStat != null)
            {
                baseStat.value = value;
            }
            else
            {
                baseStat = new BaseStat
                {
                    tag = tag,
                    value = value
                };
                baseStats.Add(baseStat);
            }
        }

        public void SetTemp(Guid guid, int value)
        {
            TempStat tempStat = tempStats.Find(x => x.guid == guid);
            if (tempStat != null)
            {
                tempStat.value = value;
            }
            else
            {
                tempStat = new TempStat
                {
                    guid = guid,
                    value = value
                };
                tempStats.Add(tempStat);
            }
        }

        public void Remove(Guid guid)
        {
            tempStats.RemoveAll(x => x.guid == guid);
        }

        public class SavableObject
        {
            public class BaseStat
            {
                public string tag;
                public int value;
            }

            public List<BaseStat> baseStats = new List<BaseStat>();

            public class TempStat
            {
                public string guid;
                public string tag;
                public int value;
            }

            public List<TempStat> tempStats = new List<TempStat>();

            public class StringKeyValuePair
            {
                public string key;
                public string value;
            }

            public List<StringKeyValuePair> stringKeyValuePairs = new List<StringKeyValuePair>();
        }

        public SavableObject Convert()
        {
            SavableObject savableObject = new SavableObject();

            foreach (var baseStat in baseStats)
            {
                SavableObject.BaseStat savableBaseStat = new SavableObject.BaseStat
                {
                    tag = baseStat.tag,
                    value = baseStat.value
                };
                savableObject.baseStats.Add(savableBaseStat);
            }

            foreach (var tempStat in tempStats)
            {
                SavableObject.TempStat savableTempStat = new SavableObject.TempStat
                {
                    guid = tempStat.guid.ToString(),
                    tag = tempStat.tag,
                    value = tempStat.value
                };
                savableObject.tempStats.Add(savableTempStat);
            }

            foreach (var stringKeyValuePair in stringKeyValuePairs)
            {
                SavableObject.StringKeyValuePair savableStringKeyValuePair = new SavableObject.StringKeyValuePair
                {
                    key = stringKeyValuePair.key,
                    value = stringKeyValuePair.value
                };
                savableObject.stringKeyValuePairs.Add(savableStringKeyValuePair);
            }

            return savableObject;
        }

        public void Load(SavableObject savableObject)
        {
            baseStats.Clear();
            tempStats.Clear();

            foreach (var baseStat in savableObject.baseStats)
            {
                baseStats.Add(new BaseStat
                {
                    tag = baseStat.tag,
                    value = baseStat.value
                });
            }

            foreach (var tempStat in savableObject.tempStats)
            {
                tempStats.Add(new TempStat
                {
                    guid = Guid.Parse(tempStat.guid),
                    tag = tempStat.tag,
                    value = tempStat.value
                });
            }
        }

        public void AddStringKeyValue(string key, string value)
        {
            StringKeyValuePair stringKeyValuePair = stringKeyValuePairs.Find(x => x.key == key);
            if (stringKeyValuePair != null)
            {
                stringKeyValuePair.value = value;
            }
            else
            {
                stringKeyValuePair = new StringKeyValuePair
                {
                    key = key,
                    value = value
                };
                stringKeyValuePairs.Add(stringKeyValuePair);
            }
        }

        public string GetStringKeyValue(string key)
        {
            StringKeyValuePair stringKeyValuePair = stringKeyValuePairs.Find(x => x.key == key);
            if (stringKeyValuePair != null)
            {
                return stringKeyValuePair.value;
            }
            return null;
        }

        public void RemoveStringKeyValue(string key)
        {
            stringKeyValuePairs.RemoveAll(x => x.key == key);
        }

        public void SetStringKeyValue(string key, string value)
        {
            StringKeyValuePair stringKeyValuePair = stringKeyValuePairs.Find(x => x.key == key);
            if (stringKeyValuePair != null)
            {
                stringKeyValuePair.value = value;
            }
            else
            {
                stringKeyValuePair = new StringKeyValuePair
                {
                    key = key,
                    value = value
                };
                stringKeyValuePairs.Add(stringKeyValuePair);
            }
        }

        public Dictionary<string, string> GetAllStringKeyValuePairs()
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            foreach (var stringKeyValuePair in stringKeyValuePairs)
            {
                keyValuePairs.Add(stringKeyValuePair.key, stringKeyValuePair.value);
            }
            return keyValuePairs;
        }
    }

    public class GeneralActor : IActor
    {
        public IValueContainer Stats { get; private set; } = new GeneralValueContainer();

        public GeneralValueContainer.SavableObject GetSavableObject()
        {
            return (Stats as GeneralValueContainer).Convert();
        }

        public void Load(GeneralValueContainer.SavableObject savableObject)
        {
            (Stats as GeneralValueContainer).Load(savableObject);
        }
    }
}