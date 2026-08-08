using System;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>SetPhase(階段Key)：直接跳到指定時間階段，例如 SetPhase(Evening)。</summary>
    public class SetPhaseCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly ITimeService timeService;

        public SetPhaseCommand(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            timeService.SetPhase(vars[0]);
            onCompleted?.Invoke();
        }
    }
}
