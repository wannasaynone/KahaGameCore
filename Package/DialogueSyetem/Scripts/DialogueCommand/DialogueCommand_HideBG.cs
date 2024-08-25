using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_HideBG : DialogueCommandBase
    {
        public DialogueCommand_HideBG(DialogueData data, IDialogueView dialogueView) : base(data, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView.HideBG();
            onCompleted?.Invoke();
        }
    }
}