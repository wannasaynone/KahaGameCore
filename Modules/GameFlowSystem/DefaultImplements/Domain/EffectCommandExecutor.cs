using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class EffectCommandExecutor : ICommandExecutor
    {
        private readonly EffectRuntime runtime;

        public EffectCommandExecutor(EffectRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public void Execute(string rawCommands, Action onCompleted)
        {
            ExecuteWithRuntimeAsync(rawCommands, CancellationToken.None, onCompleted).Forget();
        }

        public UniTask ExecuteAsync(string rawCommands, CancellationToken cancellationToken)
        {
            return ExecuteWithRuntimeAsync(rawCommands, cancellationToken, null);
        }

        private async UniTask ExecuteWithRuntimeAsync(
            string rawCommands,
            CancellationToken cancellationToken,
            Action onCompleted)
        {
            EffectExecutionResult result = await runtime.ExecuteAsync(
                rawCommands,
                new EffectExecutionContext(),
                cancellationToken);
            if (result.Status == EffectExecutionStatus.Cancelled)
            {
                throw new OperationCanceledException(result.FormatDiagnostic(), cancellationToken);
            }

            if (result.Status == EffectExecutionStatus.Failed)
            {
                throw new InvalidOperationException(result.FormatDiagnostic());
            }

            onCompleted?.Invoke();
        }
    }
}
