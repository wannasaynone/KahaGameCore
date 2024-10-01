using System.Collections.Generic;

namespace KahaGameCore.Actor
{
    public interface IValueContainer
    {
        int GetTotal(string tag, bool baseOnly);
        System.Guid Add(string tag, int value);
        void AddToTemp(System.Guid guid, int value);
        void SetTemp(System.Guid guid, int value);
        void AddBase(string tag, int value);
        void SetBase(string tag, int value);
        void Remove(System.Guid guid);
        void AddStringKeyValue(string key, string value);
        string GetStringKeyValue(string key);
        void RemoveStringKeyValue(string key);
        void SetStringKeyValue(string key, string value);
        Dictionary<string, string> GetAllStringKeyValuePairs();
    }
}