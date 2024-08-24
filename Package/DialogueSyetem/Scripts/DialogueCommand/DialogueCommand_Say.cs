using System;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
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
                        DialogueView.DehighlightCenterCharacterImage();
                        break;
                    }
                case "Right":
                    {
                        DialogueView.HighlightRightCharacterImage();
                        DialogueView.DehighlightLeftCharacterImage();
                        DialogueView.DehighlightCenterCharacterImage();
                        break;
                    }
                case "Center":
                    {
                        DialogueView.HighlightCenterCharacterImage();
                        DialogueView.DehighlightLeftCharacterImage();
                        DialogueView.DehighlightRightCharacterImage();
                        break;
                    }
                case "None":
                    {
                        DialogueView.DehighlightAllCharacterImage();
                        break;
                    }
                case "All":
                    {
                        DialogueView.HighlightAllCharacterImage();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            DialogueView.SetNameText(DialogueData.Arg1);
            DialogueView.SetContentText(DialogueData.Arg2, onCompleted);
        }
    }
}