using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;
using KahaGameCore.Package.GameFlowSystem.Samples.Views;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Presenters
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
