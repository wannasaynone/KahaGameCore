using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KahaGameCore
{
    public static class GameResourcesManager
    {
        private static readonly string PathURL =
#if UNITY_ANDROID
		"jar:file://" + Application.dataPath + "!/assets/";
#elif UNITY_IPHONE
		Application.dataPath + "/Raw/";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
       Application.dataPath + "/StreamingAssets/";
#else
        string.Empty;
#endif

        private static Dictionary<string, AssetBundle> m_nameToBundle = new Dictionary<string, AssetBundle>();
        private const string PLATFORM_WINDOWS = "StandaloneWindows";

        private static event Action OnAssetBundleInited = null;
        public static State CurrentState { get { return m_state; } }
        private static Dictionary<string, UnityEngine.Object> m_resourcePathToObject = new Dictionary<string, UnityEngine.Object>();
        private static Dictionary<Type, List<UnityEngine.Object>> m_typeToObjects = new Dictionary<Type, List<UnityEngine.Object>>();
        private static List<UnityEngine.Object> m_loadedObjects = new List<UnityEngine.Object>();

        private static State m_state = State.Default;
        public enum State
        {
            Default,
            Initing,
            Inited
        }

        public static void Init(Action onAseetBundleInited)
        {
            if (m_state == State.Inited)
            {
                onAseetBundleInited();
                return;
            }

            OnAssetBundleInited += onAseetBundleInited;

            if (m_state == State.Default)
            {
                GeneralCoroutineRunner.Instance.StartCoroutine(InitAssetBundle());
            }
        }

        private static IEnumerator InitAssetBundle()
        {
            if (m_state != State.Default)
            {
                yield break;
            }

            m_state = State.Initing;

            AssetBundleCreateRequest _mainAssetBundleRequest = AssetBundle.LoadFromFileAsync(string.Format("{0}/{1}", PathURL, PLATFORM_WINDOWS));

            while (!_mainAssetBundleRequest.isDone)
            {
                // Debug.Log("loading main bundle..." + _mainAssetBundleRequest.progress * 100);
                yield return null;
            }

            AssetBundle _mainAssetBundle = _mainAssetBundleRequest.assetBundle;
            AssetBundleRequest _manifestLoadRequest = _mainAssetBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");

            while (!_manifestLoadRequest.isDone)
            {
                // Debug.Log("loading main bundle manifest..." + _manifestLoadRequest.progress * 100);
                yield return null;
            }

            AssetBundleManifest _manifest = _manifestLoadRequest.asset as AssetBundleManifest;
            string[] _allBundleName = _manifest.GetAllAssetBundles();
            for (int i = 0; i < _allBundleName.Length; i++)
            {
                AssetBundleCreateRequest _otherBundleCreateRequest = AssetBundle.LoadFromFileAsync(string.Format("{0}/{1}", PathURL, _allBundleName[i]));
                while (!_otherBundleCreateRequest.isDone)
                {
                    // Debug.Log("loading other bundles..." + _otherBundleCreateRequest.progress * 100);
                    yield return null;
                }
                m_nameToBundle.Add(_allBundleName[i], _otherBundleCreateRequest.assetBundle);
            }

            List<string> _allBundleNames = new List<string>(m_nameToBundle.Keys);
            m_loadedObjects.Clear();
            for (int _bundleIndex = 0; _bundleIndex < _allBundleNames.Count; _bundleIndex++)
            {
                AssetBundle _currentLoadingBundle = m_nameToBundle[_allBundleNames[_bundleIndex]];

                if (_currentLoadingBundle.isStreamedSceneAssetBundle)
                {
                    // Debug.LogFormat("{0} bunlde is Streamed Scene Asset Bundle, skipped", _allBundleName[_bundleIndex]);
                    continue;
                }

                AssetBundleRequest _loadRequest = _currentLoadingBundle.LoadAllAssetsAsync();

                while (!_loadRequest.isDone)
                {
                    // Debug.LogFormat("loading assets in {0} bunlde ...{1}", _allBundleName[_bundleIndex], _loadRequest.progress * 100);
                    yield return null;
                }

                UnityEngine.Object[] _loadedObjects = _loadRequest.allAssets;

                for (int _objectIndex = 0; _objectIndex < _loadedObjects.Length; _objectIndex++)
                {
                    m_loadedObjects.Add(_loadedObjects[_objectIndex]);
                    // Debug.LogFormat("assets {0} loaded", _loadedObjects[_objectIndex].name);
                }
            }

            // Debug.Log("All Asset Bundle Assets Loaded");

            m_state = State.Inited;

            // Debug.Log("Asset Bundle Inited");

            if (OnAssetBundleInited != null)
            {
                OnAssetBundleInited();
                OnAssetBundleInited = null;
            }
        }

        public static List<T> LoadAllBundleAssets<T>() where T : UnityEngine.Object
        {
            if (CurrentState != State.Inited)
            {
                Debug.LogError("Bundle is not inited");
                return null;
            }

            List<T> _results = new List<T>();

            for(int i = 0; i < m_loadedObjects.Count; i++)
            {
                if(m_loadedObjects[i] is T)
                {
                    _results.Add(m_loadedObjects[i] as T);
                }
            }

            return _results;
        }

        public static T LoadBundleAsset<T>(string name) where T : UnityEngine.Object
        {
            if (CurrentState != State.Inited)
            {
                Debug.LogError("Bundle is not inited");
                return null;
            }

            if(m_typeToObjects.ContainsKey(typeof(T)))
            {
                T _obj = m_typeToObjects[typeof(T)].Find(x => x.name == name) as T;

                if (_obj == null)
                {
                    for (int i = 0; i < m_loadedObjects.Count; i++)
                    {
                        if (m_loadedObjects[i].name == name)
                        {
                            if(m_loadedObjects[i] is T)
                            {
                                m_typeToObjects[typeof(T)].Add(m_loadedObjects[i]);
                                return m_loadedObjects[i] as T;
                            }
                            else
                            {
                                if(m_loadedObjects[i] is GameObject)
                                {
                                    T _check = ((GameObject)m_loadedObjects[i]).GetComponent<T>();
                                    if(_check != null)
                                    {
                                        m_typeToObjects[typeof(T)].Add(_check);
                                        return _check;
                                    }
                                }
                            }
                        }
                    }
                    Debug.LogWarningFormat("Can't find bundle asset: type={0}, name={1}", typeof(T).Name, name);
                    return null;
                }
                else
                {
                    return _obj;
                }
            }
            else
            {
                for (int i = 0; i < m_loadedObjects.Count; i++)
                {
                    if (m_loadedObjects[i].name == name)
                    {
                        if (m_loadedObjects[i] is T)
                        {
                            m_typeToObjects.Add(typeof(T), new List<UnityEngine.Object>() { m_loadedObjects[i] });
                            return m_loadedObjects[i] as T;
                        }
                        else
                        {
                            if (m_loadedObjects[i] is GameObject)
                            {
                                T _check = ((GameObject)m_loadedObjects[i]).GetComponent<T>();
                                if (_check != null)
                                {
                                    m_typeToObjects.Add(typeof(T), new List<UnityEngine.Object>() { _check });
                                    return _check;
                                }
                            }
                        }
                    }
                }

                Debug.LogWarningFormat("Can't find bundle asset: type={0}, name={1}", typeof(T).Name, name);
                return null;
            }
        }

        public static T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            if (m_resourcePathToObject.ContainsKey(path))
            {
                return m_resourcePathToObject[path] as T;
            }
            else
            {
                T _obj = Resources.Load<T>(path);

                if (_obj == null)
                {
                    Debug.LogWarningFormat("Can't find {0} in {1}, will return null", typeof(T), path);
                    return null;
                }
                else
                {
                    m_resourcePathToObject.Add(path, _obj);
                    return _obj;
                }
            }
        }
    }
}
