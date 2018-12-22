﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KahaGameCore
{
    public static class GameObjectPoolManager
    {
        private static Dictionary<string, MonoBehaviour> m_fileNameToOrgainPrefab = new Dictionary<string, MonoBehaviour>();
        private static Dictionary<Type, List<MonoBehaviour>> m_typeToMonoBehaviour = new Dictionary<Type, List<MonoBehaviour>>();

        public static T GetUseableObject<T>(string fileName) where T : MonoBehaviour
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

                m_typeToMonoBehaviour[typeof(T)].Add(CreateClone<T>(fileName));
                return m_typeToMonoBehaviour[typeof(T)][m_typeToMonoBehaviour[typeof(T)].Count - 1] as T;
            }
            else
            {
                m_typeToMonoBehaviour.Add(typeof(T), new List<MonoBehaviour>() { CreateClone<T>(fileName) });
                return m_typeToMonoBehaviour[typeof(T)][0] as T;
            }
        }

        private static T CreateClone<T>(string fileName) where T : MonoBehaviour
        {
            if (!m_fileNameToOrgainPrefab.ContainsKey(fileName))
            {
                T _resource = GameResourcesManager.LoadResource<T>("Prefabs/" + fileName);
                Debug.LogFormat("Can't find {0} in \"Resources/Prefabs\", try to load it from assetbundle", fileName);
                if (_resource == null)
                {
                    _resource = GameResourcesManager.LoadBundleAsset<T>(fileName);
                    if(_resource == null)
                    {
                        Debug.LogErrorFormat("Can't find {0} in anywhere, will return null", fileName);
                        return null;
                    }
                }
                m_fileNameToOrgainPrefab.Add(fileName, _resource);
            }

            return UnityEngine.Object.Instantiate(m_fileNameToOrgainPrefab[fileName]) as T;        
        }
    }
}
