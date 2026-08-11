using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.GameEvents;

namespace KahaGameCore.GameFlowSystem.GameEventsIntegration
{
    /// <summary>把 GameFlow timing 轉交給 GameEventRunner 的可選整合層。</summary>
    public sealed class GameFlowGameEventAdapter : IGameFlowEventTriggerService
    {
        private readonly GameEventRunner runner;

        public GameFlowGameEventAdapter(GameEventRunner runner)
        {
            this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public UniTask RaiseTimingAsync(
            string timing,
            CancellationToken cancellationToken = default)
        {
            return runner.TriggerAsync(timing, new EventContext(cancellationToken));
        }
    }
}
