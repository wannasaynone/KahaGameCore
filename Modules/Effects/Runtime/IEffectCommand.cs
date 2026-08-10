using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Effects
{
    public interface IEffectCommand
    {
        UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken);
    }
}
