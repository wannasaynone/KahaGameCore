using System;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>UnlockLocation(地點ID)：解鎖地點（寫入 $LocationUnlocked_{ID} = 1）。</summary>
    public class UnlockLocationCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly ILocationService locationService;

        public UnlockLocationCommand(IGameState gameState, ILocationService locationService)
        {
            this.gameState = gameState;
            this.locationService = locationService;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            locationService.Unlock(FormulaPreprocessor.EvaluateInt(gameState, vars[0]));
            onCompleted?.Invoke();
        }
    }
}
