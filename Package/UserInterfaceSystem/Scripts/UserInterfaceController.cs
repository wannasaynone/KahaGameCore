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
        private readonly Stack<AView> m_viewStack = new Stack<AView>();

        public int ViewStackCount => m_viewStack.Count;

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

        #region View Stack

        public async Task<T> PushView<T>(string resourcePath, Action<T> onBeforeShow = null) where T : AView
        {
            T prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[UserInterfaceController] Cannot load prefab at path: {resourcePath}");
                return null;
            }

            T viewInstance = Instantiate(prefab, uiRoot);
            if (viewInstance == null)
            {
                Debug.LogError($"[UserInterfaceController] Prefab does not have component: {typeof(T).Name}");
                return null;
            }

            viewInstance.transform.SetAsLastSibling();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                if (m_viewStack.Count > 0)
                {
                    await m_viewStack.Peek().Hide(stackCts.Token);
                }

                m_viewStack.Push(viewInstance);
                onBeforeShow?.Invoke(viewInstance);
                await viewInstance.Show(stackCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] PushView canceled for: {typeof(T).Name}");
            }
            finally
            {
                stackCts.Dispose();
            }

            return viewInstance;
        }

        public async Task PushView(AView view, Action onBeforeShow = null)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            view.transform.SetAsLastSibling();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                if (m_viewStack.Count > 0)
                {
                    await m_viewStack.Peek().Hide(stackCts.Token);
                }

                m_viewStack.Push(view);
                onBeforeShow?.Invoke();
                await view.Show(stackCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] PushView canceled for: {view.name}");
            }
            finally
            {
                stackCts.Dispose();
            }
        }

        public async Task<T> PushViewWithCoexisting<T>(string resourcePath, Action<T> onBeforeShow = null) where T : AView
        {
            T prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[UserInterfaceController] Cannot load prefab at path: {resourcePath}");
                return null;
            }

            T viewInstance = Instantiate(prefab, uiRoot);
            if (viewInstance == null)
            {
                Debug.LogError($"[UserInterfaceController] Prefab does not have component: {typeof(T).Name}");
                return null;
            }

            viewInstance.transform.SetAsLastSibling();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                m_viewStack.Push(viewInstance);
                onBeforeShow?.Invoke(viewInstance);
                await viewInstance.Show(stackCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] PushViewWithCoexisting canceled for: {typeof(T).Name}");
            }
            finally
            {
                stackCts.Dispose();
            }

            return viewInstance;
        }

        public async Task PushViewWithCoexisting(AView view, Action onBeforeShow = null)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            view.transform.SetAsLastSibling();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                m_viewStack.Push(view);
                onBeforeShow?.Invoke();
                await view.Show(stackCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] PushViewWithCoexisting canceled for: {view.name}");
            }
            finally
            {
                stackCts.Dispose();
            }
        }

        public async Task<bool> PopView()
        {
            if (m_viewStack.Count == 0)
            {
                return false;
            }

            AView view = m_viewStack.Pop();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                await view.Hide(stackCts.Token);
                Destroy(view.gameObject);

                if (m_viewStack.Count > 0)
                {
                    await m_viewStack.Peek().Show(stackCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UserInterfaceController] PopView canceled.");
            }
            finally
            {
                stackCts.Dispose();
            }

            return true;
        }

        public async Task<bool> HandleBackButton()
        {
            if (m_viewStack.Count == 0)
            {
                return false;
            }

            AView currentView = m_viewStack.Peek();
            BackButtonResult result = currentView.OnBackButtonPressed();

            if (result == BackButtonResult.DoNothing)
            {
                return true;
            }

            if (m_viewStack.Count > 1)
            {
                await PopView();
                return true;
            }

            return false;
        }

        public async Task ClearViewStack()
        {
            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                while (m_viewStack.Count > 0)
                {
                    AView view = m_viewStack.Pop();
                    await view.Hide(stackCts.Token);
                    Destroy(view.gameObject);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UserInterfaceController] ClearViewStack canceled.");
            }
            finally
            {
                stackCts.Dispose();
            }
        }

        #endregion
    }
}
