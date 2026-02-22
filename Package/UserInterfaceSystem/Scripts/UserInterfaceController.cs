using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.UserInterfaceSystem
{
    public class UserInterfaceController : MonoBehaviour
    {
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private CanvasGroup blackoutOverlay;

        private Dictionary<string, AView> activeViews = new Dictionary<string, AView>();

        private CancellationTokenSource cts = new CancellationTokenSource();

        public async Task BlackIn()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();

            await BlackIn(cts.Token);
        }

        public async Task BlackIn(CancellationToken token)
        {
            blackoutOverlay.gameObject.SetActive(true);
            blackoutOverlay.alpha = 0f;
            while (blackoutOverlay.alpha < 1f - Time.deltaTime * 2f)
            {
                blackoutOverlay.alpha += Time.deltaTime * 2f;
                await UniTask.Yield(token);
            }
            blackoutOverlay.alpha = 1f;
        }

        public async Task BlackOut()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();

            await BlackOut(cts.Token);
        }

        public async Task BlackOut(CancellationToken token)
        {
            blackoutOverlay.alpha = 1f;
            while (blackoutOverlay.alpha > Time.deltaTime * 2f)
            {
                blackoutOverlay.alpha -= Time.deltaTime * 2f;
                await UniTask.Yield(token);
            }
            blackoutOverlay.alpha = 0f;
            blackoutOverlay.gameObject.SetActive(false);
        }

        public async Task Show(string viewPath, bool withBlackIn = false, bool withBlackOut = false, Action<AView> onViewCreated = null)
        {
            AView viewPrefab = Resources.Load<AView>(viewPath);
            if (viewPrefab == null)
            {
                Debug.LogError("View prefab not found at path: " + viewPath);
                return;
            }

            AView viewInstance = Instantiate(viewPrefab, uiRoot);
            activeViews[viewPath] = viewInstance;
            onViewCreated?.Invoke(viewInstance);
            await Show(viewInstance, withBlackIn, withBlackOut);
        }

        public async Task Show(AView viewInstance, bool withBlackIn = false, bool withBlackOut = false)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();

            try
            {
                if (withBlackIn)
                {
                    viewInstance.gameObject.SetActive(false);
                    await BlackIn(cts.Token);
                }

                await viewInstance.Show(cts.Token);

                if (withBlackOut)
                {
                    await BlackOut(cts.Token);
                }

                cts.Dispose();
                cts = null;
            }
            catch (System.OperationCanceledException e)
            {
                Debug.Log("Show operation canceled for view: " + viewInstance.name + ". " + e.Message);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error showing view: " + viewInstance.name + ". " + e.Message);
            }
        }

        public async Task Hide(string viewPath, bool withBlackIn = false, bool withBlackOut = false)
        {
            if (activeViews.TryGetValue(viewPath, out AView viewInstance))
            {
                await Hide(viewInstance, withBlackIn, withBlackOut);
                activeViews.Remove(viewPath);
            }
            else
            {
                Debug.LogError("No active view found for path: " + viewPath);
            }
        }


        public async Task Hide(AView viewInstance, bool withBlackIn = false, bool withBlackOut = false)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();

            try
            {
                if (withBlackIn)
                {
                    await BlackIn(cts.Token);
                }

                await viewInstance.Hide(cts.Token);
                Destroy(viewInstance.gameObject);

                if (withBlackOut)
                {
                    await BlackOut(cts.Token);
                }

                cts.Dispose();
                cts = null;
            }
            catch (System.OperationCanceledException e)
            {
                Debug.Log("Hide operation canceled for view: " + viewInstance.name + ". " + e.Message);
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error hiding view: " + viewInstance.name + ". " + e.Message);
                return;
            }
        }

        public T GetView<T>(string viewPath) where T : AView
        {
            if (activeViews.TryGetValue(viewPath, out AView viewInstance))
            {
                return viewInstance as T;
            }
            else
            {
                Debug.LogError("No active view found for path: " + viewPath);
                return null;
            }
        }

        public T GetView<T>() where T : AView
        {
            foreach (var viewInstance in activeViews.Values)
            {
                if (viewInstance is T typedView)
                {
                    return typedView;
                }
            }

            Debug.LogError("No active view of type: " + typeof(T).Name);
            return null;
        }
    }
}