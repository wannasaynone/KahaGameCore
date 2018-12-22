using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore
{
    public static class GameResourcesManager
    {
        private static Dictionary<string, Object> m_nameToObject = new Dictionary<string, Object>();

        private static void LoadAllResource()
        {
            m_nameToObject.Clear();
            LoadResource<Sprite>("Sprites");
        }

        private static void LoadResource<T>(string path) where T : Object
        {
            T[] _allObject = Resources.LoadAll<T>(path);

            for (int i = 0; i < _allObject.Length; i++)
            {
                if (m_nameToObject.ContainsKey(_allObject[i].name))
                {
                    Debug.LogError("Duplicate file name:" + _allObject[i].name);
                    return;
                }
                else
                {
                    m_nameToObject.Add(_allObject[i].name, _allObject[i]);
                }
            }
        }

        public static T GetResource<T>(string name) where T : Object
        {
            if(typeof(T) == typeof(Sprite))
            {
                return TryGetResource<T>(name);
            }
            else
            {
                Debug.LogError("Undefine Resource Type=" + typeof(T));
                return null;
            }
        }

        private static T TryGetResource<T>(string name) where T : Object
        {
            if (m_nameToObject.Count == 0)
            {
                LoadAllResource();
            }

            if (!m_nameToObject.ContainsKey(name))
            {
                Debug.LogErrorFormat("Can't Find {0}:{1}", typeof(T), name);
                return null;
            }

            return m_nameToObject[name] as T;
        }
    }
}
