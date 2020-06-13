using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace KahaGameCore.Static
{
    public static class GameResourcesManager
    {
        public static readonly string ASSET_BUNDLE_LOCAL_PATH =
#if UNITY_EDITOR
    Application.streamingAssetsPath + "/";
#elif UNITY_ANDROID
    "jar:file://" + Application.dataPath + "!/assets/";
#elif UNITY_IPHONE
    Application.dataPath + "/Raw/";
#elif UNITY_STANDALONE_WIN
	"file://" + Application.dataPath + "/StreamingAssets/";
#else
    string.Empty;
#endif
        private static Dictionary<string, UnityEngine.Object> m_resourceNameToResource = new Dictionary<string, UnityEngine.Object>();
        private static Dictionary<string, AssetBundle> m_bundleNameToAssetBundle = new Dictionary<string, AssetBundle>();

        public enum AssetBundleSource
        {
            Editor,
            StreamingAsset
        }

        public static bool IsBundleLoaded(string bundleName)
        {
            return m_bundleNameToAssetBundle.ContainsKey(bundleName);
        }

        public static bool IsResourceLoaded(string sourceName)
        {
            return m_resourceNameToResource.ContainsKey(sourceName);
        }

        public static void UnloadBundle(string bundleName)
        {
            if(!IsBundleLoaded(bundleName))
            {
                return;
            }

            m_bundleNameToAssetBundle[bundleName].Unload(true);
            m_bundleNameToAssetBundle.Remove(bundleName);
        }

        public static void UnloadResource(string sourceName)
        {
            if(!IsResourceLoaded(sourceName))
            {
                return;
            }

            Resources.UnloadAsset(m_resourceNameToResource[sourceName]);
            m_resourceNameToResource.Remove(sourceName);
        }

        public static void LoadAssetBundle(AssetBundleSource source, string bundleName, Action onLoaded, Action<float> onProgressUpdated = null)
        {
            if(IsBundleLoaded(bundleName))
            {
                if(onLoaded != null)
                {
                    onLoaded();
                }
            }

            switch(source)
            {
                case AssetBundleSource.Editor:
                    {
#if UNITY_EDITOR
                        UnityEditor.BuildTarget _buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                        string _path = Application.dataPath + "/../AssetBundle/" + _buildTarget + "/";
                        LoadAssetBundle(_path, bundleName, onLoaded, onProgressUpdated);
#else
                        Debug.LogError("DO NOT RUN THIS SOURCE WHILE RELEASE");
#endif
                        break;
                    }
                case AssetBundleSource.StreamingAsset:
                    {
                        LoadAssetBundle(ASSET_BUNDLE_LOCAL_PATH, bundleName, onLoaded, onProgressUpdated);
                        break;
                    }
            }
        }

        public static void LoadAssetBundle(string host, string bundleName, Action onLoaded, Action<float> onProgressUpdated)
        {
            if (IsBundleLoaded(bundleName))
            {
                if (onLoaded != null)
                {
                    onLoaded();
                }
                return;
            }

            if (host.Contains("http"))
            {
                UnityWebRequest _webRequest = UnityWebRequestAssetBundle.GetAssetBundle(host + bundleName, 0);
                UnityWebRequestAsyncOperation _oper = _webRequest.SendWebRequest();

                GeneralCoroutineRunner.Instance.StartCoroutine(IELoadAssetBundle(_oper, bundleName, onLoaded, onProgressUpdated));
            }
            else
            {
                if(!System.IO.File.Exists(host + bundleName))
                {
                    Debug.LogError(host + bundleName + " not exist");
                    return;
                }
                AssetBundleCreateRequest _request = AssetBundle.LoadFromFileAsync(host + bundleName, 0);
                GeneralCoroutineRunner.Instance.StartCoroutine(IELoadAssetBundle(_request, bundleName, onLoaded, onProgressUpdated));
            }
        }

        private static IEnumerator IELoadAssetBundle(UnityWebRequestAsyncOperation request, string bundleName, Action onLoaded, Action<float> onProgressUpdated)
        {
            while(!request.isDone)
            {
                if (onProgressUpdated != null)
                {
                    onProgressUpdated(request.progress);
                }
                yield return null;
            }

            if(request.webRequest.isNetworkError || request.webRequest.isHttpError)
            {
                Debug.LogError("[GameResourcesManager] Network error while downloading asset bundle");
                yield break;
            }

            AssetBundle _ab = DownloadHandlerAssetBundle.GetContent(request.webRequest);
            m_bundleNameToAssetBundle.Add(bundleName, _ab);

            if(onLoaded != null)
            {
                onLoaded();
            }
        }

        private static IEnumerator IELoadAssetBundle(AssetBundleCreateRequest request, string bundleName, Action onLoaded, Action<float> onProgressUpdated)
        {
            while (!request.isDone)
            {
                if (onProgressUpdated != null)
                {
                    onProgressUpdated(request.progress);
                }
                yield return null;
            }

            if (request.assetBundle == null)
            {
                Debug.LogError("[GameResourcesManager] Can't find asset bundle");
                yield break;
            }

            AssetBundle _ab = request.assetBundle;
            m_bundleNameToAssetBundle.Add(bundleName, _ab);

            if (onLoaded != null)
            {
                onLoaded();
            }
        }

        public static void LoadResource<T>(string resourceName, Action<T> onLoaded, Action<float> onProgressUpdated = null) where T : UnityEngine.Object
        {
            if(IsResourceLoaded(resourceName))
            {
                onLoaded(m_resourceNameToResource[resourceName] as T);
                return;
            }

            ResourceRequest _resRequest = Resources.LoadAsync<T>(resourceName);
            GeneralCoroutineRunner.Instance.StartCoroutine(IELoadResource(resourceName, _resRequest, onLoaded, onProgressUpdated));
        }

        public static T LoadResource<T>(string resourceName) where T : UnityEngine.Object
        {
            if (IsResourceLoaded(resourceName))
            {
                return m_resourceNameToResource[resourceName] as T;
            }

            T _obj = Resources.Load<T>(resourceName);
            if(_obj != null)
            {
                m_resourceNameToResource.Add(resourceName, _obj);
            }
            else
            {
                Debug.LogError("[GameResourcesManager][Resources] Can't find resource asset at " + resourceName);
                return null;
            }

            return _obj;
        }

        public static void LoadResource<T>(string bundleName, string resourceName, Action<T> onLoaded, Action<float> onProgressUpdated = null) where T : UnityEngine.Object
        {
            if (IsResourceLoaded(resourceName))
            {
                onLoaded(m_resourceNameToResource[resourceName] as T);
                return;
            }

            if (!m_bundleNameToAssetBundle.ContainsKey(bundleName))
            {
                Debug.LogErrorFormat("[GameResourcesManager][AssetBundle] AssetBundle {0} has not been loaded", bundleName);
                return;
            }

            if (!m_bundleNameToAssetBundle[bundleName].Contains(resourceName))
            {
                Debug.LogErrorFormat("[GameResourcesManager][AssetBundle] Can't find resource asset {0} in bundle {1}", resourceName, bundleName);
                return;
            }

            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                AssetBundleRequest _request = m_bundleNameToAssetBundle[bundleName].LoadAssetAsync<GameObject>(resourceName);
                GeneralCoroutineRunner.Instance.StartCoroutine(IELoadAssetFromBundle(_request, resourceName, onLoaded, onProgressUpdated));
            }
            else
            {
                AssetBundleRequest _request = m_bundleNameToAssetBundle[bundleName].LoadAssetAsync<T>(resourceName);
                GeneralCoroutineRunner.Instance.StartCoroutine(IELoadAssetFromBundle(_request, resourceName, onLoaded, onProgressUpdated));
            }
        }

        public static T LoadResource<T>(string bundleName, string resourceName) where T : UnityEngine.Object
        {
            if (IsResourceLoaded(resourceName))
            {
                return m_resourceNameToResource[resourceName] as T;
            }

            if (!IsBundleLoaded(bundleName))
            {
                Debug.LogErrorFormat("[GameResourcesManager][AssetBundle] AssetBundle {0} has not been loaded", bundleName);
                return null;
            }

            T _obj = null;
            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                _obj = m_bundleNameToAssetBundle[bundleName].LoadAsset<GameObject>(resourceName).GetComponent<T>();
            }
            else
            {
                _obj = m_bundleNameToAssetBundle[bundleName].LoadAsset<T>(resourceName);
            }

            if (_obj != null)
            {
                m_resourceNameToResource.Add(resourceName, _obj);
            }
            else
            {
                Debug.LogErrorFormat("[GameResourcesManager][AssetBundle] Can't find resource asset {0} in bundle {1}", resourceName, bundleName);
            }

            return _obj;
        }

        private static IEnumerator IELoadResource<T>(string resourceName, ResourceRequest request, Action<T> onLoaded, Action<float> onProgressUpdated) where T : UnityEngine.Object
        {
            while(!request.isDone)
            {
                if(onProgressUpdated != null)
                {
                    onProgressUpdated(request.progress);
                }
                yield return null;
            }

            if(request.asset == null)
            {
                Debug.LogError("[GameResourcesManager][Resources] Can't find resource asset at " + resourceName);
                yield break;
            }

            m_resourceNameToResource.Add(resourceName, request.asset);

            if(onLoaded != null)
            {
                onLoaded(request.asset as T);
            }
        }

        private static IEnumerator IELoadAssetFromBundle<T>(AssetBundleRequest request, string assetName, Action<T> onLoaded, Action<float> onProgressUpdated) where T : UnityEngine.Object
        {
            while (!request.isDone)
            {
                if (onProgressUpdated != null)
                {
                    onProgressUpdated(request.progress);
                }
                yield return null;
            }

            if (request.asset == null)
            {
                Debug.LogError("[GameResourcesManager][AssetBundle] request.asset == null");
                yield break;
            }

            T _t = request.asset as T;
            if (_t == null)
            {
                _t = ((GameObject)request.asset).GetComponent<T>();
                if (_t == null)
                {
                    Debug.LogError("[GameResourcesManager][AssetBundle] asset doesn't have/is not " + typeof(T));
                    yield break;
                }
            }

            m_resourceNameToResource.Add(assetName, _t);
            if (onLoaded != null)
            {
                onLoaded(_t);
            }
        }
    }
}
