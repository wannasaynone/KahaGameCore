using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_GoTo : DialogueCommandBase
    {
        private readonly Player player;

        public DialogueCommand_GoTo(DialogueData dialogueData, IDialogueView dialogueView, Player player) : base(dialogueData, dialogueView)
        {
            this.player = player;
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueManager.Instance.TriggerDialogue(int.Parse(DialogueData.Arg1), DialogueView);
            onCompleted?.Invoke();
        }
    }
}