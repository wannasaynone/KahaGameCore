using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_SetBG : DialogueCommandBase
    {
        public DialogueCommand_SetBG(DialogueData data, IDialogueView dialogueView) : base(data, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView.SetBG(DialogueData.Arg1);
            onCompleted?.Invoke();
        }
    }
}