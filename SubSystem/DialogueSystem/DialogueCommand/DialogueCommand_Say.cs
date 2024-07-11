using System;

namespace KahaGameCore.SubSystem.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_Say : DialogueCommandBase
    {
        public DialogueCommand_Say(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView.SetNameText(DialogueData.Arg1);
            DialogueView.SetContentText(DialogueData.Arg2);
            DialogueView.Show(onCompleted);
        }
    }
}