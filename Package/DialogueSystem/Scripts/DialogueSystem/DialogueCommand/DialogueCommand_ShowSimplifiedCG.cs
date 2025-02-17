using System;

namespace KahaGameCore.Package.DialogueSystem
{

    public class DialogueCommand_ShowSimplifiedCG : DialogueCommandBase
    {
        public DialogueCommand_ShowSimplifiedCG(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string cgName = DialogueData.Arg1;
            DialogueView cgView = DialogueView as DialogueView;

            cgView.ShowSimplifiedCG(cgName, onCompleted);
        }
    }
}