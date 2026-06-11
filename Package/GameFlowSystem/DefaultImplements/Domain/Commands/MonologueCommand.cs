using System;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>Monologue(群組名)：從 GameTextData 表依群組與條件隨機抽一句自言自語顯示於 HUD。</summary>
    public class MonologueCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameTextProvider textProvider;

        public MonologueCommand(IGameTextProvider textProvider)
        {
            this.textProvider = textProvider;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            GameTextData text = textProvider.PickRandom(vars[0]);
            if (text != null)
            {
                EventBus.Publish(new MonologueRequestedEvent(text.Text));
            }

            onCompleted?.Invoke();
        }
    }
}
