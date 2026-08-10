using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>MoveToLocation(地點ID)：移動到指定地點（流程會在指令串結束後觸發 EnterLocation 事件）。</summary>
    public class MoveToLocationCommand : IEffectCommand
    {
        private readonly GameFlowExpressions expressions;
        private readonly ILocationService locationService;

        public MoveToLocationCommand(GameFlowExpressions expressions, ILocationService locationService)
        {
            this.expressions = expressions;
            this.locationService = locationService;
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            locationService.MoveTo(expressions.CalculateInt(arguments[0]));
            return UniTask.CompletedTask;
        }
    }
}
