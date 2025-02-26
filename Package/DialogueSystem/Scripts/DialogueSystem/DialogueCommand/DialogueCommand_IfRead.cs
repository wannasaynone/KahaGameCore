using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_IfRead : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_IfRead(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            int dialogueID = int.Parse(DialogueData.Arg1);
            int goToIfDid = int.Parse(DialogueData.Arg2);
            int goToIfNot = string.IsNullOrEmpty(DialogueData.Arg3) ? -1 : int.Parse(DialogueData.Arg3);

            if (player.HasReadDialogue(dialogueID))
            {
                DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                {
                    id = goToIfDid,
                    dialogueView = DialogueView,
                    onCompleted = null
                });
                onForceQuit?.Invoke();
                return;
            }
            else if (goToIfNot != -1)
            {
                DialogueManager.Instance.TriggerDialogue(new DialogueManager.PendingDialogueData
                {
                    id = goToIfNot,
                    dialogueView = DialogueView,
                    onCompleted = null
                });
                onForceQuit?.Invoke();
                return;
            }

            onCompleted?.Invoke();
        }
    }
}