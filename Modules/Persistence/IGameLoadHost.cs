using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Parameters;

namespace KahaGameCore.Persistence
{
    /// <summary>
    /// Project composition seam used by GameLoadCoordinator. The production
    /// adapter owns Unity Scene loading; tests may provide an in-memory adapter.
    /// </summary>
    public interface IGameLoadHost
    {
        /// <summary>
        /// Loads and composes the requested Scene using the supplied restored
        /// ParameterStore. Before completing, the adapter must initialize Scene
        /// binders so presentation reflects the restored parameters.
        /// </summary>
        UniTask LoadSceneAsync(
            string sceneKey,
            ParameterStore parameters,
            CancellationToken cancellationToken);
    }
}
