using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    public class HintPresenter : IHintPresenter
    {
        private readonly HintPopupView view;
        private UniTaskCompletionSource pendingConfirm;

        public HintPresenter(HintPopupView view)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
        }

        public async UniTask ShowAsync(string text)
        {
            CancelPending();
            pendingConfirm = new UniTaskCompletionSource();

            view.Bind(text, () => pendingConfirm.TrySetResult());
            await view.Show(CancellationToken.None);

            await pendingConfirm.Task;

            await view.Hide(CancellationToken.None);
        }

        public void CancelPending()
        {
            pendingConfirm?.TrySetResult();
        }
    }
}
