using System;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>AdvanceTime()：推進到下一個時間階段（依 TimePhaseData.NextID）。</summary>
    public class AdvanceTimeCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly ITimeService timeService;

        public AdvanceTimeCommand(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            timeService.AdvanceTime();
            onCompleted?.Invoke();
        }
    }
}
