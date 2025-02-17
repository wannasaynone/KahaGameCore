using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_ShakeCG : DialogueCommandBase
    {
        public DialogueCommand_ShakeCG(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView dialogueView = (DialogueView)DialogueView;

            float strength = float.Parse(DialogueData.Arg1);
            float shakeTime = float.Parse(DialogueData.Arg2);

            dialogueView.ShakeCGImage(strength, shakeTime, onCompleted);
        }
    }
}