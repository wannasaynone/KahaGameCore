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

        private static Dictionary<int, List<PoolObject>> m_prefabMonoInstanceIDToPoolObjects = new Dictionary<int, List<PoolObject>>();
        private static Dictionary<MonoBehaviour, int> m_clonesToPrefabMonoInstanceID = new Dictionary<MonoBehaviour, int>();

        public static T GetInstance<T>(MonoBehaviour prefab) where T : MonoBehaviour
        {
            if (m_prefabMonoInstanceIDToPoolObjects.ContainsKey(prefab.GetInstanceID()))
            {
                List<PoolObject> _allInstances = m_prefabMonoInstanceIDToPoolObjects[prefab.GetInstanceID()];
                for (int i = 0; i < _allInstances.Count; i++)
                {
                    if (!_allInstances[i].actived)
                    {
                        _allInstances[i].actived = true;
                        return _allInstances[i].MonoBehaviour as T;
                    }
                }

                PoolObject _newObj = CreateNewPoolObject(prefab);
                m_prefabMonoInstanceIDToPoolObjects[prefab.GetInstanceID()].Add(_newObj);
                _newObj.actived = true;

                return _newObj.MonoBehaviour as T;
            }
            else
            {
                PoolObject _newObj = CreateNewPoolObject(prefab);
                m_prefabMonoInstanceIDToPoolObjects.Add(prefab.GetInstanceID(), new List<PoolObject> { _newObj });
                _newObj.actived = true;

                return _newObj.MonoBehaviour as T;
            }
        }

        private static PoolObject CreateNewPoolObject(MonoBehaviour prefab)
        {
            PoolObject _newObj = new PoolObject(Object.Instantiate(prefab));
            m_clonesToPrefabMonoInstanceID.Add(_newObj.MonoBehaviour, prefab.GetInstanceID());

            return _newObj;
        }

        public static void Recycle(MonoBehaviour monoBehaviour)
        {
            List<PoolObject> _allInstances = m_prefabMonoInstanceIDToPoolObjects[m_clonesToPrefabMonoInstanceID[monoBehaviour]];
            for (int i = 0; i < _allInstances.Count; i++)
            {
                if (_allInstances[i].MonoBehaviour == monoBehaviour)
                {
                    _allInstances[i].MonoBehaviour.transform.position = new Vector3(10000f, 0f, 0f);
                    _allInstances[i].actived = false;
                }
            }
        }
    }
}
