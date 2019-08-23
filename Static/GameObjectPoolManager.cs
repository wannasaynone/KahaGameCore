﻿using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Static
{
    public static class GameObjectPoolManager
    {
        private class PoolObject
        {
            public MonoBehaviour monoBehaviour;
            public bool active;
        }

        private static Dictionary<string, MonoBehaviour> m_fileNameToOrgainPrefab = new Dictionary<string, MonoBehaviour>();
        private static Dictionary<string, List<PoolObject>> m_fileNameToPoolObjects = new Dictionary<string, List<PoolObject>>();

        public static T GetUseableObject<T>(string resourcePath) where T : MonoBehaviour
        {
            if (m_fileNameToPoolObjects.ContainsKey(resourcePath))
            {
                List<PoolObject> _allObject = new List<PoolObject>(m_fileNameToPoolObjects[resourcePath]);

                for (int i = 0; i < _allObject.Count; i++)
                {
                    if(_allObject[i].monoBehaviour == null)
                    {
                        m_fileNameToPoolObjects[resourcePath].Remove(_allObject[i]);
                        continue;
                    }

                    if (!_allObject[i].active)
                    {
                        _allObject[i].monoBehaviour.transform.localPosition = Vector3.zero;
                        _allObject[i].active = true;
                        return _allObject[i].monoBehaviour as T;
                    }
                }

                m_fileNameToPoolObjects[resourcePath].Add(CreateClone<T>(resourcePath));
                return m_fileNameToPoolObjects[resourcePath][m_fileNameToPoolObjects[resourcePath].Count - 1].monoBehaviour as T;
            }
            else
            {
                m_fileNameToPoolObjects.Add(resourcePath, new List<PoolObject>() { CreateClone<T>(resourcePath) });
                return m_fileNameToPoolObjects[resourcePath][0].monoBehaviour as T;
            }
        }

        public static void Recycle(MonoBehaviour obj)
        {
            foreach(KeyValuePair<string, List<PoolObject>> keyValuePair in m_fileNameToPoolObjects)
            {
                PoolObject _poolObject = keyValuePair.Value.Find(x => x.monoBehaviour == obj);
                if (_poolObject != null)
                {
                    _poolObject.monoBehaviour.transform.localPosition += new Vector3(100000, 0);
                    _poolObject.active = false;
                    return;
                }
            }

            Debug.LogErrorFormat("{0} is not created by GameObjectPoolManager", obj);
        }

        private static PoolObject CreateClone<T>(string path) where T : MonoBehaviour
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

            PoolObject _newPoolObj = new PoolObject()
            {
                monoBehaviour = _clone,
                active = true
            };

            return _newPoolObj;        
        }
    }
}
