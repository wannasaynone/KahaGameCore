using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore
{
    public static class GameResourcesManager
    {
        private static Dictionary<string, Object> m_nameToObject = new Dictionary<string, Object>();

        public static T LoadResource<T>(string path) where T : Object
        {
            if (m_nameToObject.ContainsKey(path))
            {
                return m_nameToObject[path] as T;
            }
            else
            {
                T _obj = Resources.Load<T>(path);

                if (_obj == null)
                {
                    Debug.LogErrorFormat("Can't load resource: type={0}, path={1}", typeof(T).Name, path);
                    return null;
                }
                else
                {
                    m_nameToObject.Add(path, _obj);
                    return _obj;
                }
            }
        }
    }
}
