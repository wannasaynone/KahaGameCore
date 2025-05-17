using System;
using KahaGameCore.Package.EffectProcessor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_PlayBGM : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new PlayBGM();
        }
    }

    public class PlayBGM : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            AudioClip audioClip = Resources.Load<AudioClip>(vars[0]);

            if (audioClip != null)
            {
                Audio.AudioManager.Instance.PlayBGM(audioClip);
                onCompleted?.Invoke();
            }
            else
            {
                Debug.LogError("Audio clip not found: " + vars[0]);
                onCompleted?.Invoke();
            }
        }
    }
}