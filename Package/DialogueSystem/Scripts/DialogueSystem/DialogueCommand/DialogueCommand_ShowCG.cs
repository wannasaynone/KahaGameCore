using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_ShowCG : DialogueCommandBase
    {
        public DialogueCommand_ShowCG(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            if (string.IsNullOrEmpty(DialogueData.Arg2))
            {
                string cgSprite = DialogueData.Arg1;
                DialogueView.ShowCGImage(cgSprite, onCompleted);
            }
            else
            {
                if (bool.TryParse(DialogueData.Arg2, out bool isMini))
                {
                    if (isMini)
                    {
                        string cgSprite = DialogueData.Arg1;
                        ((DialogueView)DialogueView).ShowMiniCGImage(cgSprite, onCompleted);
                    }
                    else
                    {
                        string cgSprite = DialogueData.Arg1;
                        DialogueView.ShowCGImage(cgSprite, onCompleted);
                    }
                }
                else
                {
                    string cgSprite = DialogueData.Arg1;
                    DialogueView.ShowCGImage(cgSprite, onCompleted);
                }
            }

        }
    }
}