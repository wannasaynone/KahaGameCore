using System;
using KahaGameCore.Package.EffectProcessor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Command
{
    public class CutsceneCommandFactory_ShowStoryCG : EffectCommandFactoryBase
    {
        public override EffectCommandBase Create()
        {
            return new ShowStoryCG();
        }
    }

    public class ShowStoryCG : EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            Utlity.GeneralBlackScreen.Instance.ShowStoryCGImage(Resources.Load<Sprite>(vars[0]), delegate
            {
                onCompleted?.Invoke();
            });
        }
    }
}