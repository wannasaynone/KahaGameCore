using System;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>StartDialogue(對話ID)：播放一段劇情對話並等待結束。</summary>
    public class StartDialogueCommand : KahaGameCore.Effects.EffectCommandBase
    {
        private readonly GameFlowExpressions expressions;
        private readonly IDialoguePlayer dialoguePlayer;

        public StartDialogueCommand(GameFlowExpressions expressions, IDialoguePlayer dialoguePlayer)
        {
            this.expressions = expressions;
            this.dialoguePlayer = dialoguePlayer;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            int dialogueId = expressions.CalculateInt(vars[0]);
            PlayAsync(dialogueId, onCompleted).Forget();
        }

        private async UniTaskVoid PlayAsync(int dialogueId, Action onCompleted)
        {
            await dialoguePlayer.PlayAsync(dialogueId);
            onCompleted?.Invoke();
        }
    }
}
