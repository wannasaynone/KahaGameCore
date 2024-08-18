using System;

namespace KahaGameCore.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_ShowCG : DialogueCommandBase
    {
        public DialogueCommand_ShowCG(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string cgSprite = DialogueData.Arg1;
            DialogueView.ShowCGImage(cgSprite, onCompleted);
        }
    }
}