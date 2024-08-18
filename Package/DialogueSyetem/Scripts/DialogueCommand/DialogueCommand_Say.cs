using System;

namespace KahaGameCore.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_Say : DialogueCommandBase
    {
        public DialogueCommand_Say(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            switch (DialogueData.Arg3)
            {
                case "Left":
                    {
                        DialogueView.HighlightLeftCharacterImage();
                        DialogueView.DehighlightRightCharacterImage();
                        break;
                    }
                case "Right":
                    {
                        DialogueView.HighlightRightCharacterImage();
                        DialogueView.DehighlightLeftCharacterImage();
                        break;
                    }
                case "None":
                    {
                        DialogueView.DehighlightAllCharacterImage();
                        break;
                    }
                case "All":
                default:
                    {
                        DialogueView.HighlightAllCharacterImage();
                        break;
                    }
            }

            DialogueView.SetNameText(DialogueData.Arg1);
            DialogueView.SetContentText(DialogueData.Arg2, onCompleted);
        }
    }
}