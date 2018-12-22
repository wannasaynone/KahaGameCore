using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonFx.Json;
using KahaGameCore.Data;

namespace KahaGameCore
{
    public static class GameDataManager
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

        public enum State
        {
            Default,
            Initing,
            Inited
        }

        private const string PLATFORM_WINDOWS = "StandaloneWindows";
        private const string BUNDLE_NAME_STATIC_DATA = "staticdata";
        private const string BUNDLE_NAME_PREFAB = "prefab";

        private static event Action OnAssetBundleInited = null;
        public static State CurrentState { get { return m_state; } }
        private static Dictionary<string, AssetBundle> m_nameToBundle = new Dictionary<string, AssetBundle>();

        private static Dictionary<Type, IGameData[]> m_gameData = new Dictionary<Type, IGameData[]>();
        private static Dictionary<Type, ScriptableObject> m_typeToSO = new Dictionary<Type, ScriptableObject>();
        private static GameObject[] m_allPrefabResources = null;

        private static State m_state = State.Default;

        public static void Init(Action onAseetBundleInited)
        {
            if (m_state == State.Inited)
            {
                onAseetBundleInited();
                return;
            }

            OnAssetBundleInited += onAseetBundleInited;

            if(m_state == State.Default)
            {
                GeneralCoroutineRunner.Instance.StartCoroutine(InitAssetBundle());
            }
        }

        private static IEnumerator InitAssetBundle()
        {
            if(m_state != State.Default)
            {
                yield break;
            }

            m_state = State.Initing;

            AssetBundleCreateRequest _mainAssetBundleRequest = AssetBundle.LoadFromFileAsync(string.Format("{0}/{1}", PathURL, PLATFORM_WINDOWS));

            while (!_mainAssetBundleRequest.isDone)
            {
                Debug.Log("loading main bundle..." + _mainAssetBundleRequest.progress * 100);
                yield return null;
            }

            AssetBundle _mainAssetBundle = _mainAssetBundleRequest.assetBundle;
            AssetBundleRequest _manifestLoadRequest = _mainAssetBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");

            while(!_manifestLoadRequest.isDone)
            {
                Debug.Log("loading main bundle manifest..." + _manifestLoadRequest.progress * 100);
                yield return null;
            }

            AssetBundleManifest _manifest = _manifestLoadRequest.asset as AssetBundleManifest;
            string[] _allBundleName = _manifest.GetAllAssetBundles();
            for(int i = 0; i < _allBundleName.Length; i++)
            {
                AssetBundleCreateRequest _otherBundleCreateRequest = AssetBundle.LoadFromFileAsync(string.Format("{0}/{1}", PathURL, _allBundleName[i]));
                while(!_otherBundleCreateRequest.isDone)
                {
                    Debug.Log("loading other bundles..." + _otherBundleCreateRequest.progress * 100);
                    yield return null;
                }
                m_nameToBundle.Add(_allBundleName[i], _otherBundleCreateRequest.assetBundle);
            }

            AssetBundle _staticDataObjectBundle = m_nameToBundle[BUNDLE_NAME_STATIC_DATA];
            AssetBundleRequest _loadStaticDataObjectRequest = _staticDataObjectBundle.LoadAllAssetsAsync<ScriptableObject>();

            while(!_loadStaticDataObjectRequest.isDone)
            {
                Debug.Log("loading SO files..." + _loadStaticDataObjectRequest.progress * 100);
                yield return null;
            }

            UnityEngine.Object[] _loadedObjects = _loadStaticDataObjectRequest.allAssets;
            ScriptableObject[] _allSO = new ScriptableObject[_loadedObjects.Length];

            for (int i = 0; i < _loadedObjects.Length; i++)
            {
                _allSO[i] = _loadedObjects[i] as ScriptableObject;
                //TryLoadSO<CombineData>(_allSO[i]);
                //TryLoadSO<FurnaceData>(_allSO[i]);
            }

            AssetBundle _gameObjectBundle = m_nameToBundle[BUNDLE_NAME_PREFAB];
            AssetBundleRequest _loadPrefabRequest = _gameObjectBundle.LoadAllAssetsAsync<GameObject>();

            while (!_loadPrefabRequest.isDone)
            {
                Debug.Log("loading prefabs..." + _loadPrefabRequest.progress * 100);
                yield return null;
            }

            _loadedObjects = _loadPrefabRequest.allAssets;
            m_allPrefabResources = new GameObject[_loadedObjects.Length];
            for(int i = 0; i < _loadedObjects.Length; i++)
            {
                m_allPrefabResources[i] = _loadedObjects[i] as GameObject;
            }

            m_state = State.Inited;

            if (OnAssetBundleInited != null)
            {
                OnAssetBundleInited();
                OnAssetBundleInited = null;
            }

            Debug.Log("Asset Bundle Inited");
        }

        public static void LoadGameData<T>(string path) where T : IGameData
        {
            T[] data = JsonReader.Deserialize<T[]>(Resources.Load<TextAsset>(path).text);
            if (m_gameData.ContainsKey(typeof(T)))
            {
                m_gameData[typeof(T)] = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    m_gameData[typeof(T)][i] = data[i];
                }
            }
            else
            {
                IGameData[] _gameData = new IGameData[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    _gameData[i] = data[i];
                }
                m_gameData.Add(typeof(T), _gameData);
            }
        }

        public static T GetGameData<T>(int id) where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                for (int i = 0; i < m_gameData[typeof(T)].Length; i++)
                {
                    if (m_gameData[typeof(T)][i].ID == id)
                    {
                        return (T)m_gameData[typeof(T)][i];
                    }
                }
            }

            return default(T);
        }

        public static T[] GetAllGameData<T>() where T : IGameData
        {
            if (m_gameData.ContainsKey(typeof(T)))
            {
                T[] _gameDatas = new T[m_gameData[typeof(T)].Length];
                for (int i = 0; i < m_gameData[typeof(T)].Length; i++)
                {
                    _gameDatas[i] = (T)m_gameData[typeof(T)][i];
                }
                return _gameDatas;
            }

            return default(T[]);
        }

        public static T GetScriptableObjectData<T>() where T : ScriptableObject
        {
            if (m_state != State.Inited)
            {
                Debug.LogError("Bundle not inited");
            }

            return m_typeToSO[typeof(T)] as T;
        }

        public static T GetPrefabClone<T>(string name) where T : UnityEngine.Object
        {
            if (m_state != State.Inited)
            {
                Debug.LogError("Bundle not inited");
            }

            for (int i = 0; i < m_allPrefabResources.Length; i++)
            {
                if(m_allPrefabResources[i].name != name)
                {
                    continue;
                }

                T _check = m_allPrefabResources[i].GetComponent<T>();

                if (_check != null)
                {
                    return UnityEngine.Object.Instantiate(_check);
                }
            }

            return null;
        }

        private static void TryLoadSO<T>(ScriptableObject obj) where T : ScriptableObject
        {
            if (obj is T)
            {
                if (m_typeToSO.ContainsKey(typeof(T)))
                {
                    Debug.LogErrorFormat("{0} existed but is trying to load it, Check it", typeof(T).Name);
                }
                else
                {
                    m_typeToSO.Add(typeof(T), obj);
                }
            }
        }

    }
}
