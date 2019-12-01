using System.Collections.Generic;
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

        public static void GetUseableObject<T>(string resourcePath, System.Action<T> onGot) where T : MonoBehaviour
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
                        if(onGot != null)
                        {
                            onGot(_allObject[i].monoBehaviour as T);
                        }
                        return;
                    }
                }

                CreateClone<T>(resourcePath,
                delegate(PoolObject poolObj)
                {
                    m_fileNameToPoolObjects[resourcePath].Add(poolObj);
                    if (onGot != null)
                    {
                        onGot(m_fileNameToPoolObjects[resourcePath][m_fileNameToPoolObjects[resourcePath].Count - 1].monoBehaviour as T);
                    }
                });
            }
            else
            {
                CreateClone<T>(resourcePath,
                delegate (PoolObject poolObj)
                {
                    m_fileNameToPoolObjects.Add(resourcePath, new List<PoolObject>() { poolObj });
                    if (onGot != null)
                    {
                        onGot(poolObj.monoBehaviour as T);
                    }
                });
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

            Debug.LogErrorFormat("[GameObjectPoolManager] {0} is not created by GameObjectPoolManager", obj);
        }

        private static void LoadOrgainResource<T>(string path, System.Action onLoaded) where T : MonoBehaviour
        {
            if (!m_fileNameToOrgainPrefab.ContainsKey(path))
            {
                GameResourcesManager.LoadResource(path, 
                delegate(T res)
                {
                    if(res == null)
                    {
                        Debug.LogError("[GameObjectPoolManager] Can't find asset at " + path);
                        return;
                    }

                    if(onLoaded != null)
                    {
                        m_fileNameToOrgainPrefab.Add(path, res);
                        if(onLoaded != null)
                        {
                            onLoaded();
                        }
                    }
                });
            }
            else
            {
                if (onLoaded != null)
                {
                    onLoaded();
                }
            }
        }

        private static void CreateClone<T>(string path, System.Action<PoolObject> onCreated) where T : MonoBehaviour
        {
            if(m_fileNameToOrgainPrefab.ContainsKey(path))
            {
                T _clone = Object.Instantiate(m_fileNameToOrgainPrefab[path]) as T;
                _clone.name = _clone.name + ":" + _clone.GetInstanceID();

                PoolObject _newPoolObj = new PoolObject()
                {
                    monoBehaviour = _clone,
                    active = true
                };

                if (onCreated != null)
                {
                    onCreated(_newPoolObj);
                }
            }
            else
            {
                LoadOrgainResource<T>(path, 
                delegate
                {
                    CreateClone<T>(path, onCreated);
                });
            }
        }
    }
}
