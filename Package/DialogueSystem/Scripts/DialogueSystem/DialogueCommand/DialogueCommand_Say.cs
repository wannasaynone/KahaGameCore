using System;

namespace KahaGameCore.Package.DialogueSystem
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

            switch (DialogueManager.Instance.currentLanguage)
            {
                case DialogueManager.LanguageType.TranditionalChinese:
                    DialogueView.SetNameText(DialogueData.Arg1);
                    DialogueView.SetContentText(DialogueData.Arg2, onCompleted);
                    break;
                case DialogueManager.LanguageType.English:
                    DialogueView.SetNameText(DialogueData.Arg1_en);
                    DialogueView.SetContentText(DialogueData.Arg2_en, onCompleted);
                    break;
                case DialogueManager.LanguageType.SimplifiedChinese:
                    DialogueView.SetNameText(DialogueData.Arg1_hans);
                    DialogueView.SetContentText(DialogueData.Arg2_hans, onCompleted);
                    break;
                default:
                    UnityEngine.Debug.LogError("Language not supported: " + DialogueManager.Instance.currentLanguage);
                    DialogueView.SetNameText(DialogueData.Arg1);
                    DialogueView.SetContentText(DialogueData.Arg2, onCompleted);
                    break;
            }
        }
    }
}