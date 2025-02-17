using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_Transition : DialogueCommandBase
    {
        public DialogueCommand_Transition(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView dialogueView = (DialogueView)DialogueView;

            float fadeInTime = float.Parse(DialogueData.Arg1);
            float stay = float.Parse(DialogueData.Arg2);
            float fadeOutTime = float.Parse(DialogueData.Arg3);

            dialogueView.Transition(fadeInTime, stay, fadeOutTime, onCompleted);
        }
    }
}