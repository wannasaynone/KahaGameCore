using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    public class ActionMenuPresenter : IActionMenuPresenter
    {
        private readonly ActionMenuView view;
        private UniTaskCompletionSource<IGameFlowAction> pendingSelection;

        public ActionMenuPresenter(ActionMenuView view)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
        }

        public async UniTask<IGameFlowAction> SelectActionAsync(IReadOnlyList<ActionMenuEntry> entries)
        {
            CancelPending();
            pendingSelection = new UniTaskCompletionSource<IGameFlowAction>();

            view.Bind(entries, entry => pendingSelection.TrySetResult(entry.Action));
            await view.Show(CancellationToken.None);

            IGameFlowAction selected = await pendingSelection.Task;

            await view.Hide(CancellationToken.None);
            return selected;
        }

        /// <summary>結束遊戲流程時呼叫，讓等待中的選擇以 null 結束以利流程退出。</summary>
        public void CancelPending()
        {
            pendingSelection?.TrySetResult(null);
        }
    }
}
