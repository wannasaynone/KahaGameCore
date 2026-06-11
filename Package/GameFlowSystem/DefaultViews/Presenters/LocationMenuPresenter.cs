using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    public class LocationMenuPresenter : ILocationMenuPresenter
    {
        private readonly LocationMenuView view;
        private UniTaskCompletionSource<LocationData> pendingSelection;

        public LocationMenuPresenter(LocationMenuView view)
        {
            this.view = view ? view : throw new ArgumentNullException(nameof(view));
        }

        public async UniTask<LocationData> SelectLocationAsync(IReadOnlyList<LocationData> locations)
        {
            CancelPending();
            pendingSelection = new UniTaskCompletionSource<LocationData>();

            view.Bind(
                locations,
                location => pendingSelection.TrySetResult(location),
                () => pendingSelection.TrySetResult(null));
            await view.Show(CancellationToken.None);

            LocationData selected = await pendingSelection.Task;

            await view.Hide(CancellationToken.None);
            return selected;
        }

        public void CancelPending()
        {
            pendingSelection?.TrySetResult(null);
        }
    }
}
