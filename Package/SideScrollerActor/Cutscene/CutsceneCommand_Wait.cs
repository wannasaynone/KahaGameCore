using System;
using System.Collections;
using KahaGameCore.Package.EffectProcessor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_Wait : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new Wait();
        }
    }

    public class Wait : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            KahaGameCore.Common.GeneralCoroutineRunner.Instance.StartCoroutine(WaitCoroutine(float.Parse(vars[0]), onCompleted));
        }

        private IEnumerator WaitCoroutine(float time, Action onCompleted)
        {
            yield return new WaitForSeconds(time);
            onCompleted?.Invoke();
        }
    }
}