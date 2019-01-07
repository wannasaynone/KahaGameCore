using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KahaGameCore
{
    public static class GameObjectPoolManager
    {
        private static Dictionary<string, MonoBehaviour> m_fileNameToOrgainPrefab = new Dictionary<string, MonoBehaviour>();
        private static Dictionary<Type, List<MonoBehaviour>> m_typeToMonoBehaviour = new Dictionary<Type, List<MonoBehaviour>>();

        public static T GetUseableObject<T>(string path) where T : MonoBehaviour
        {
            if (m_typeToMonoBehaviour.ContainsKey(typeof(T)))
            {
                List<MonoBehaviour> _allObject = m_typeToMonoBehaviour[typeof(T)];

                for (int i = 0; i < _allObject.Count; i++)
                {
                    if (!_allObject[i].gameObject.activeSelf)
                    {
                        return _allObject[i] as T;
                    }
                }

                m_typeToMonoBehaviour[typeof(T)].Add(CreateClone<T>(path));
                return m_typeToMonoBehaviour[typeof(T)][m_typeToMonoBehaviour[typeof(T)].Count - 1] as T;
            }
            else
            {
                m_typeToMonoBehaviour.Add(typeof(T), new List<MonoBehaviour>() { CreateClone<T>(path) });
                return m_typeToMonoBehaviour[typeof(T)][0] as T;
            }
        }

        private static T CreateClone<T>(string path) where T : MonoBehaviour
        {
            if (!m_fileNameToOrgainPrefab.ContainsKey(path))
            {
                T _resource = GameResourcesManager.LoadResource<T>(path);

                if (_resource == null)
                {
                    Debug.LogFormat("Can't find {0} in {1}, try to load it from assetbundle", typeof(T), path);
                    _resource = GameResourcesManager.LoadBundleAsset<T>(path);
                    if(_resource == null)
                    {
                        Debug.LogErrorFormat("Can't find {0} in anywhere, will return null", typeof(T));
                        return null;
                    }
                }
                m_fileNameToOrgainPrefab.Add(path, _resource);
            }

            return UnityEngine.Object.Instantiate(m_fileNameToOrgainPrefab[path]) as T;        
        }
    }
}
