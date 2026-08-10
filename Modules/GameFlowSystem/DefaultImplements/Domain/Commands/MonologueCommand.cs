using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>Monologue(群組名)：從 GameTextData 表依群組與條件隨機抽一句自言自語顯示於 HUD。</summary>
    public class MonologueCommand : IEffectCommand
    {
        private readonly IGameTextProvider textProvider;

        public MonologueCommand(IGameTextProvider textProvider)
        {
            this.textProvider = textProvider;
        }

        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameTextData text = textProvider.PickRandom(arguments[0]);
            if (text != null)
            {
                MessageBus.Publish(new MonologueRequestedEvent(text.Text));
            }

            return UniTask.CompletedTask;
        }
    }
}
