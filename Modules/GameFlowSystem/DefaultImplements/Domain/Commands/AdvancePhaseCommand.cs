using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AdvancePhase()：依 TimePhaseData.NextID 推進到下一個 Phase。</summary>
    public sealed class AdvancePhaseCommand : IEffectCommand
    {
        private readonly ITimeService timeService;

        public AdvancePhaseCommand(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            timeService.AdvancePhase();
            return UniTask.CompletedTask;
        }
    }
}
