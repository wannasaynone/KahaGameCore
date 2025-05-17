using System;
using KahaGameCore.Package.DialogueSystem;

namespace KahaGameCore.Package.SideScrollerActor.DialogueSystemExtend
{
    public class DialogueCommand_StopInGameBGM : DialogueCommandBase
    {
        public DialogueCommand_StopInGameBGM(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            Audio.AudioManager.Instance.StopBGM();
            onCompleted?.Invoke();
        }
    }
}