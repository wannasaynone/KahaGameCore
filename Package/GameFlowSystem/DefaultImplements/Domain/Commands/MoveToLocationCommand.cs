using System;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>MoveToLocation(地點ID)：移動到指定地點（流程會在指令串結束後觸發 EnterLocation 事件）。</summary>
    public class MoveToLocationCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly ILocationService locationService;

        public MoveToLocationCommand(IGameState gameState, ILocationService locationService)
        {
            this.gameState = gameState;
            this.locationService = locationService;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            locationService.MoveTo(FormulaPreprocessor.EvaluateInt(gameState, vars[0]));
            onCompleted?.Invoke();
        }
    }
}
