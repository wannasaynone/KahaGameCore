using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AdvanceTime()：推進到下一個時間階段（依 TimePhaseData.NextID）。</summary>
    public class AdvanceTimeCommand : IEffectCommand
    {
        private readonly ITimeService timeService;

        public AdvanceTimeCommand(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            timeService.AdvanceTime();
            return UniTask.CompletedTask;
        }
    }
}
