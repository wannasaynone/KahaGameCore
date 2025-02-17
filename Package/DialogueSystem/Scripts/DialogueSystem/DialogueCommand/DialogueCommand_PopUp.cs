using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_PopUp : DialogueCommandBase
    {
        public DialogueCommand_PopUp(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string message = DialogueData.Arg1;
            GeneralPopup.Instance.PopUp(message, onCompleted);
        }
    }
}