using System;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueCommand_PlayBGM : DialogueCommandBase
    {
        public DialogueCommand_PlayBGM(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string bgmName = DialogueData.Arg1;
            SoundManager.Instance.PlayBGM(Resources.Load<AudioClip>(bgmName));
            onCompleted?.Invoke();
        }
    }
}