using System;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>StartDialogue(對話ID)：播放一段劇情對話並等待結束。</summary>
    public class StartDialogueCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        private readonly IGameState gameState;
        private readonly IDialoguePlayer dialoguePlayer;

        public StartDialogueCommand(IGameState gameState, IDialoguePlayer dialoguePlayer)
        {
            this.gameState = gameState;
            this.dialoguePlayer = dialoguePlayer;
        }

        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            int dialogueId = FormulaPreprocessor.EvaluateInt(gameState, vars[0]);
            PlayAsync(dialogueId, onCompleted).Forget();
        }

        private async UniTaskVoid PlayAsync(int dialogueId, Action onCompleted)
        {
            await dialoguePlayer.PlayAsync(dialogueId);
            onCompleted?.Invoke();
        }
    }
}
