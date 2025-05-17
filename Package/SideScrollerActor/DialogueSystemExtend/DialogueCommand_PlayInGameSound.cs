using System;
using KahaGameCore.Package.DialogueSystem;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.DialogueSystemExtend
{
    public class DialogueCommand_PlayInGameSound : DialogueCommandBase
    {
        public DialogueCommand_PlayInGameSound(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            string path = DialogueData.Arg1;
            AudioClip audioClip = Resources.Load<AudioClip>(path);

            if (audioClip != null)
            {
                Audio.AudioManager.Instance.PlaySound(audioClip);
            }
            else
            {
                Debug.LogError($"Audio clip not found at path: {path}");
            }
            onCompleted?.Invoke();
        }
    }
}