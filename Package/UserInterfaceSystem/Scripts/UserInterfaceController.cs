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

        private class ViewStackEntry
        {
            public AView MainView;
            public readonly Dictionary<string, AView> AttachedViews = new Dictionary<string, AView>();
        }

        private readonly Stack<ViewStackEntry> m_viewStack = new Stack<ViewStackEntry>();

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

        public T GetView<T>() where T : AView
        {
            foreach (ViewStackEntry entry in m_viewStack)
            {
                if (entry.MainView is T mainTyped)
                {
                    return mainTyped;
                }

                foreach (AView attached in entry.AttachedViews.Values)
                {
                    if (attached is T attachedTyped)
                    {
                        return attachedTyped;
                    }
                }
            }

            Debug.LogError("No active view of type: " + typeof(T).Name);
            return null;
        }

        #region View Stack

        private async Task HideEntry(ViewStackEntry entry, CancellationToken token)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(entry.MainView.Hide(token));
            foreach (AView attached in entry.AttachedViews.Values)
            {
                tasks.Add(attached.Hide(token));
            }
            await Task.WhenAll(tasks);
        }

        private async Task ShowEntry(ViewStackEntry entry, CancellationToken token)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(entry.MainView.Show(token));
            foreach (AView attached in entry.AttachedViews.Values)
            {
                tasks.Add(attached.Show(token));
            }
            await Task.WhenAll(tasks);
        }

        private void DestroyEntry(ViewStackEntry entry)
        {
            foreach (AView attached in entry.AttachedViews.Values)
            {
                Destroy(attached.gameObject);
            }
            entry.AttachedViews.Clear();
            Destroy(entry.MainView.gameObject);
        }

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
                    await HideEntry(m_viewStack.Peek(), stackCts.Token);
                }

                ViewStackEntry newEntry = new ViewStackEntry { MainView = viewInstance };
                m_viewStack.Push(newEntry);

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
                    await HideEntry(m_viewStack.Peek(), stackCts.Token);
                }

                ViewStackEntry newEntry = new ViewStackEntry { MainView = view };
                m_viewStack.Push(newEntry);

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

        public async Task<bool> PopView()
        {
            if (m_viewStack.Count == 0)
            {
                return false;
            }

            ViewStackEntry entry = m_viewStack.Pop();

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                await HideEntry(entry, stackCts.Token);
                DestroyEntry(entry);

                if (m_viewStack.Count > 0)
                {
                    await ShowEntry(m_viewStack.Peek(), stackCts.Token);
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

        public async Task<T> AttachView<T>(string resourcePath, Action<T> onBeforeShow = null) where T : AView
        {
            if (m_viewStack.Count == 0)
            {
                Debug.LogError($"[UserInterfaceController] Cannot attach view: no main view in stack. Path: {resourcePath}");
                return null;
            }

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
                ViewStackEntry entry = m_viewStack.Peek();
                entry.AttachedViews[resourcePath] = viewInstance;

                onBeforeShow?.Invoke(viewInstance);
                await viewInstance.Show(stackCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] AttachView canceled for: {typeof(T).Name}");
            }
            finally
            {
                stackCts.Dispose();
            }

            return viewInstance;
        }

        public async Task<bool> DetachView(string resourcePath)
        {
            if (m_viewStack.Count == 0)
            {
                return false;
            }

            ViewStackEntry entry = m_viewStack.Peek();

            if (!entry.AttachedViews.TryGetValue(resourcePath, out AView view))
            {
                Debug.LogWarning($"[UserInterfaceController] No attached view found for path: {resourcePath}");
                return false;
            }

            entry.AttachedViews.Remove(resourcePath);

            CancellationTokenSource stackCts = new CancellationTokenSource();

            try
            {
                await view.Hide(stackCts.Token);
                Destroy(view.gameObject);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[UserInterfaceController] DetachView canceled for: {resourcePath}");
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

            ViewStackEntry entry = m_viewStack.Peek();
            BackButtonResult result = entry.MainView.OnBackButtonPressed();

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
                    ViewStackEntry entry = m_viewStack.Pop();
                    await HideEntry(entry, stackCts.Token);
                    DestroyEntry(entry);
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
