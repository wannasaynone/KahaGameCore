using System;
using KahaGameCore.GameEvent;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>ReturnToTitle()：結束目前遊戲流程並返回主標題（遊戲結尾使用）。</summary>
    public class ReturnToTitleCommand : KahaGameCore.Effects.EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            EventBus.Publish(new ReturnToTitleRequestedEvent());
            onCompleted?.Invoke();
        }
    }
}
