using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_HideCG : DialogueCommandBase
    {
        public DialogueCommand_HideCG(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView.HideCGImage(onCompleted);
        }
    }
}