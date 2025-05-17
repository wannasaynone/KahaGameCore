using System;
using KahaGameCore.Package.DialogueSystem;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.DialogueSystemExtend
{
    public class DialogueCommand_PlayInGameBGM : DialogueCommandBase
    {
        public DialogueCommand_PlayInGameBGM(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string path = DialogueData.Arg1;
            AudioClip audioClip = Resources.Load<AudioClip>(path);

            Audio.AudioManager.Instance.PlayBGM(audioClip);
            onCompleted?.Invoke();
        }
    }
}