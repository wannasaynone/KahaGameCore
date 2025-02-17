using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_Flash : DialogueCommandBase
    {
        public DialogueCommand_Flash(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            DialogueView dialogueView = (DialogueView)DialogueView;

            float fadeInTime = float.Parse(DialogueData.Arg1);
            float stay = float.Parse(DialogueData.Arg2);
            float fadeOutTime = float.Parse(DialogueData.Arg3);

            dialogueView.Flash(fadeInTime, stay, fadeOutTime, onCompleted);
        }
    }
}