using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>StartDialogue(對話ID)：播放一段劇情對話並等待結束。</summary>
    public class StartDialogueCommand : IEffectCommand
    {
        private readonly GameFlowExpressions expressions;
        private readonly IDialoguePlayer dialoguePlayer;

        public StartDialogueCommand(GameFlowExpressions expressions, IDialoguePlayer dialoguePlayer)
        {
            this.expressions = expressions;
            this.dialoguePlayer = dialoguePlayer;
        }

        public async UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int dialogueId = expressions.CalculateInt(arguments[0]);
            await dialoguePlayer.PlayAsync(dialogueId).AttachExternalCancellation(cancellationToken);
        }
    }
}
