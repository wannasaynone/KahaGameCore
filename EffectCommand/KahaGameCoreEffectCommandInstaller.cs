using Zenject;

namespace KahaGameCore.EffectCommand
{
    public class KahaGameCoreEffectCommandInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<EffectProcesser>().AsTransient();
            Container.Bind<EffectCommandFactoryContainer>().AsSingle();
            Container.Bind<SignalBus>().AsSingle();
            Container.DeclareSignal<EffectTimingTriggedSignal>();
            Container.BindFactory<EffectProcesser, EffectProcesser.Facotry>();
        }
    }
}
