using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Static
{
    public static class GameObjectPoolManager
    {
        private class PoolObject
        {
            public MonoBehaviour MonoBehaviour { get; private set; }
            public bool actived = false;

            public PoolObject(MonoBehaviour monoBehaviour)
            {
                MonoBehaviour = monoBehaviour;
            }
        }

        private static Dictionary<MonoBehaviour, List<PoolObject>> m_prefabToPoolObjects = new Dictionary<MonoBehaviour, List<PoolObject>>();
        private static Dictionary<MonoBehaviour, MonoBehaviour> m_clonesToPrefab = new Dictionary<MonoBehaviour, MonoBehaviour>();

        public static T GetInstance<T>(MonoBehaviour prefab) where T : MonoBehaviour
        {
            if (m_prefabToPoolObjects.ContainsKey(prefab))
            {
                List<PoolObject> _allInstances = m_prefabToPoolObjects[prefab];
                for (int i = 0; i < _allInstances.Count; i++)
                {
                    if (!_allInstances[i].actived)
                    {
                        _allInstances[i].actived = true;
                        return _allInstances[i].MonoBehaviour as T;
                    }
                }

                PoolObject _newObj = CreateNewPoolObject(prefab);
                m_prefabToPoolObjects[prefab].Add(_newObj);
                _newObj.actived = true;

                return _newObj.MonoBehaviour as T;
            }
            else
            {
                PoolObject _newObj = CreateNewPoolObject(prefab);
                m_prefabToPoolObjects.Add(prefab, new List<PoolObject> { _newObj });
                _newObj.actived = true;

                return _newObj.MonoBehaviour as T;
            }
        }

        private static PoolObject CreateNewPoolObject(MonoBehaviour prefab)
        {
            PoolObject _newObj = new PoolObject(Object.Instantiate(prefab));
            m_clonesToPrefab.Add(_newObj.MonoBehaviour, prefab);

            return _newObj;
        }

        public static void Recycle(MonoBehaviour monoBehaviour)
        {
            List<PoolObject> _allInstances = m_prefabToPoolObjects[m_clonesToPrefab[monoBehaviour]];
            for (int i = 0; i < _allInstances.Count; i++)
            {
                if (_allInstances[i].MonoBehaviour == monoBehaviour)
                {
                    _allInstances[i].MonoBehaviour.transform.position = new Vector3(10000f, 0f, 0f);
                    _allInstances[i].actived = true;
                }
            }
        }
    }
}
