using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>ReturnToTitle()：結束目前遊戲流程並返回主標題（遊戲結尾使用）。</summary>
    public class ReturnToTitleCommand : IEffectCommand
    {
        public UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MessageBus.Publish(new ReturnToTitleRequestedEvent());
            return UniTask.CompletedTask;
        }
    }
}
