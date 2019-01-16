using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KahaGameCore
{
    public static class GameObjectPoolManager
    {
        private static Dictionary<string, MonoBehaviour> m_fileNameToOrgainPrefab = new Dictionary<string, MonoBehaviour>();
        private static Dictionary<string, List<MonoBehaviour>> m_fileNameToMonoBehaviour = new Dictionary<string, List<MonoBehaviour>>();

        public static T GetUseableObject<T>(string path) where T : MonoBehaviour
        {
            if (m_fileNameToMonoBehaviour.ContainsKey(path))
            {
                List<MonoBehaviour> _allObject = m_fileNameToMonoBehaviour[path];

                for (int i = 0; i < _allObject.Count; i++)
                {
                    if (!_allObject[i].gameObject.activeSelf)
                    {
                        _allObject[i].gameObject.SetActive(true);
                        return _allObject[i] as T;
                    }
                }

                m_fileNameToMonoBehaviour[path].Add(CreateClone<T>(path));
                return m_fileNameToMonoBehaviour[path][m_fileNameToMonoBehaviour[path].Count - 1] as T;
            }
            else
            {
                m_fileNameToMonoBehaviour.Add(path, new List<MonoBehaviour>() { CreateClone<T>(path) });
                return m_fileNameToMonoBehaviour[path][0] as T;
            }
        }

        private static T CreateClone<T>(string path) where T : MonoBehaviour
        {
            if (!m_fileNameToOrgainPrefab.ContainsKey(path))
            {
                T _resource = GameResourcesManager.LoadResource<T>(path);

                if (_resource == null)
                {
                    _resource = GameResourcesManager.LoadBundleAsset<T>(path);
                    if(_resource == null)
                    {
                        Debug.LogErrorFormat("Can't find {0} in {1}, will return null.", typeof(T), path);
                        return null;
                    }
                }
                m_fileNameToOrgainPrefab.Add(path, _resource);
            }

            T _clone = UnityEngine.Object.Instantiate(m_fileNameToOrgainPrefab[path]) as T;
            _clone.name = _clone.name + ":" + _clone.GetInstanceID();

            return _clone;        
        }
    }
}
