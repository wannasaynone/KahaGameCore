using System;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_StopBGM : DialogueCommandBase
    {
        public DialogueCommand_StopBGM(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            SoundManager.Instance.StopBGM();
            onCompleted?.Invoke();
        }
    }
}