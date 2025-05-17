using System.Collections;
using KahaGameCore.Common;
using KahaGameCore.Package.EffectProcessor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_PlayRepeatSound : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new PlayRepeatSound();
        }
    }

    public class PlayRepeatSound : EffectCommandBase
    {
        public override void Process(string[] vars, System.Action onCompleted, System.Action onForceQuit)
        {
            string path = vars[0];
            int repeatCount = int.Parse(vars[1]);
            float repeatGap = float.Parse(vars[2]);
            bool noWait = vars[3] == "T";

            if (noWait)
            {
                GeneralCoroutineRunner.Instance.StartCoroutine(IEPlayRepeatSound(path, repeatCount, repeatGap, null));
                onCompleted?.Invoke();
            }
            else
            {
                GeneralCoroutineRunner.Instance.StartCoroutine(IEPlayRepeatSound(path, repeatCount, repeatGap, onCompleted));
            }
        }

        private IEnumerator IEPlayRepeatSound(string path, int repeatCount, float repeatGap, System.Action onCompleted)
        {
            AudioClip audioClip = Resources.Load<AudioClip>(path);
            for (int i = 0; i < repeatCount; i++)
            {
                Audio.AudioManager.Instance.PlaySound(audioClip);
                yield return new WaitForSeconds(repeatGap);
            }

            onCompleted?.Invoke();
        }
    }
}