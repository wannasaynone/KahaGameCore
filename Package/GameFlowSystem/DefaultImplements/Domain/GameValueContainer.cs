using System;
using System.Collections.Generic;
using KahaGameCore.ValueContainer;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    /// <summary>
    /// IValueContainer 的標準實作：基礎值 + 可追蹤移除的暫時加值 + 字串鍵值對。
    /// 未設定過的標籤一律視為 0，讓條件式可以直接引用尚未出現的旗標。
    /// </summary>
    public class GameValueContainer : IValueContainer
    {
        private readonly Dictionary<string, int> tagToBaseValue = new Dictionary<string, int>();
        private readonly Dictionary<Guid, TempValue> guidToTempValue = new Dictionary<Guid, TempValue>();
        private readonly Dictionary<string, string> stringKeyToValue = new Dictionary<string, string>();

        private class TempValue
        {
            public string Tag;
            public int Value;
        }

        public int GetTotal(string tag, bool baseOnly)
        {
            tagToBaseValue.TryGetValue(tag, out int total);

            if (baseOnly)
            {
                return total;
            }

            foreach (TempValue tempValue in guidToTempValue.Values)
            {
                if (tempValue.Tag == tag)
                {
                    total += tempValue.Value;
                }
            }

            return total;
        }

        public Guid Add(string tag, int value)
        {
            Guid guid = Guid.NewGuid();
            guidToTempValue.Add(guid, new TempValue { Tag = tag, Value = value });
            return guid;
        }

        public void AddToTemp(Guid guid, int value)
        {
            if (guidToTempValue.TryGetValue(guid, out TempValue tempValue))
            {
                tempValue.Value += value;
            }
        }

        public void SetTemp(Guid guid, int value)
        {
            if (guidToTempValue.TryGetValue(guid, out TempValue tempValue))
            {
                tempValue.Value = value;
            }
        }

        public void AddBase(string tag, int value)
        {
            tagToBaseValue.TryGetValue(tag, out int current);
            tagToBaseValue[tag] = current + value;
        }

        public void SetBase(string tag, int value)
        {
            tagToBaseValue[tag] = value;
        }

        public void Remove(Guid guid)
        {
            guidToTempValue.Remove(guid);
        }

        public string GetStringKeyValue(string key)
        {
            return stringKeyToValue.TryGetValue(key, out string value) ? value : string.Empty;
        }

        public void RemoveStringKeyValue(string key)
        {
            stringKeyToValue.Remove(key);
        }

        public void SetStringKeyValue(string key, string value)
        {
            stringKeyToValue[key] = value;
        }

        public Dictionary<string, string> GetAllStringKeyValuePairs()
        {
            return new Dictionary<string, string>(stringKeyToValue);
        }
    }
}
