using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_IfDayLess : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_IfDayLess(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            int targetDay = int.Parse(DialogueData.Arg1);
            int goToIfDid = int.Parse(DialogueData.Arg2);
            int goToIfNot = string.IsNullOrEmpty(DialogueData.Arg3) ? -1 : int.Parse(DialogueData.Arg3);

            if (player.day <= targetDay)
            {
                DialogueManager.Instance.TriggerDialogue(goToIfDid, DialogueView);
                onForceQuit?.Invoke();
                return;
            }
            else if (goToIfNot != -1)
            {
                DialogueManager.Instance.TriggerDialogue(goToIfNot, DialogueView);
                onForceQuit?.Invoke();
                return;
            }

            onCompleted?.Invoke();
        }
    }
}