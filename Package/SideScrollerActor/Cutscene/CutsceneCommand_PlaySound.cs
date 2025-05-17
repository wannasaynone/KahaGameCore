using System;
using KahaGameCore.Package.EffectProcessor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_PlaySound : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new PlaySound();
        }
    }

    public class PlaySound : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            AudioClip audioClip = Resources.Load<AudioClip>(vars[0]);
            if (audioClip != null)
            {
                Audio.AudioManager.Instance.PlaySound(audioClip);
                onCompleted?.Invoke();
            }
            else
            {
                Debug.LogError($"Audio clip not found at path: {vars[0]}");
                onCompleted?.Invoke();
            }
        }
    }
}