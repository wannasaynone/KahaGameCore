using System.Collections.Generic;

namespace KahaGameCore.ActorSystem
{
    public class ActionContext
    {
        private readonly Dictionary<string, object> _data = new();

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var val))
            {
                return (T)val;
            }
            return defaultValue;
        }

        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }

        public void Clear()
        {
            _data.Clear();
        }
    }
}
