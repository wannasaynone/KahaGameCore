using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_AddCoin : DialogueCommandBase
    {
        public DialogueCommand_AddCoin(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            int addCoin = int.Parse(DialogueData.Arg1);
            PlayerManager.Instance.Player.coin += addCoin;
            onCompleted?.Invoke();
        }
    }
}