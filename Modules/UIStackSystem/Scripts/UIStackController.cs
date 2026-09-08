using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.UIStackSystem
{
    public class UIStackController : MonoBehaviour
    {
        [SerializeField] private RectTransform uiRoot;
        [SerializeField] private CanvasGroup blackoutOverlay;

        private class ViewStackEntry
        {
            public AStackableView MainView;
            public readonly Dictionary<string, AStackableView> AttachedViews = new Dictionary<string, AStackableView>();
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

        /// <summary>
        /// 即時開關目前最上層主 View（不做淡入淡出）。用於黑幕之下隱藏/顯示 HUD：
        /// 既然畫面已被黑幕蓋住，這裡只需切換 active，避免與黑幕重複淡兩次。
        /// </summary>
        public void SetTopViewActive(bool active)
        {
            if (m_viewStack.Count > 0)
            {
                m_viewStack.Peek().MainView.gameObject.SetActive(active);
            }
        }

        public T GetView<T>() where T : AStackableView
        {
            foreach (ViewStackEntry entry in m_viewStack)
            {
                if (entry.MainView is T mainTyped)
                {
                    return mainTyped;
                }

                foreach (AStackableView attached in entry.AttachedViews.Values)
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
            foreach (AStackableView attached in entry.AttachedViews.Values)
            {
                tasks.Add(attached.Hide(token));
            }
            await Task.WhenAll(tasks);
        }

        private async Task ShowEntry(ViewStackEntry entry, CancellationToken token)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(entry.MainView.Show(token));
            foreach (AStackableView attached in entry.AttachedViews.Values)
            {
                tasks.Add(attached.Show(token));
            }
            await Task.WhenAll(tasks);
        }

        private void DestroyEntry(ViewStackEntry entry)
        {
            foreach (AStackableView attached in entry.AttachedViews.Values)
            {
                Destroy(attached.gameObject);
            }
            entry.AttachedViews.Clear();
            Destroy(entry.MainView.gameObject);
        }

        public async Task<T> PushView<T>(string resourcePath, Action<T> onBeforeShow = null) where T : AStackableView
        {
            T prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[UIStackController] Cannot load prefab at path: {resourcePath}");
                return null;
            }

            T viewInstance = Instantiate(prefab, uiRoot);
            if (viewInstance == null)
            {
                Debug.LogError($"[UIStackController] Prefab does not have component: {typeof(T).Name}");
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
                Debug.Log($"[UIStackController] PushView canceled for: {typeof(T).Name}");
            }
            finally
            {
                stackCts.Dispose();
            }

            return viewInstance;
        }

        public async Task PushView(AStackableView view, Action onBeforeShow = null)
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
                Debug.Log($"[UIStackController] PushView canceled for: {view.name}");
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
                Debug.Log("[UIStackController] PopView canceled.");
            }
            finally
            {
                stackCts.Dispose();
            }

            return true;
        }

        public async Task<T> AttachView<T>(string resourcePath, Action<T> onBeforeShow = null) where T : AStackableView
        {
            if (m_viewStack.Count == 0)
            {
                Debug.LogError($"[UIStackController] Cannot attach view: no main view in stack. Path: {resourcePath}");
                return null;
            }

            T prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[UIStackController] Cannot load prefab at path: {resourcePath}");
                return null;
            }

            T viewInstance = Instantiate(prefab, uiRoot);
            if (viewInstance == null)
            {
                Debug.LogError($"[UIStackController] Prefab does not have component: {typeof(T).Name}");
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
                Debug.Log($"[UIStackController] AttachView canceled for: {typeof(T).Name}");
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

            if (!entry.AttachedViews.TryGetValue(resourcePath, out AStackableView view))
            {
                Debug.LogWarning($"[UIStackController] No attached view found for path: {resourcePath}");
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
                Debug.Log($"[UIStackController] DetachView canceled for: {resourcePath}");
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
                Debug.Log("[UIStackController] ClearViewStack canceled.");
            }
            finally
            {
                stackCts.Dispose();
            }
        }

        #endregion
    }
}
