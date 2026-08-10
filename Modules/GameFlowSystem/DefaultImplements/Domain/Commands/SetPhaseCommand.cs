using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetPhase(階段Key)：直接跳到指定時間階段，例如 SetPhase(Evening)。</summary>
    public class SetPhaseCommand : IEffectCommand
    {
        private readonly ITimeService timeService;

        public SetPhaseCommand(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            timeService.SetPhase(arguments[0]);
            return UniTask.CompletedTask;
        }
    }
}
