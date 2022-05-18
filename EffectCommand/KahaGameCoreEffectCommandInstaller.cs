using Zenject;

namespace KahaGameCore.EffectCommand
{
    public class KahaGameCoreEffectCommandInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);

            Container.Bind<EffectProcesser>().AsTransient();
            Container.Bind<EffectCommandFactoryContainer>().AsSingle();
            Container.Bind<EffectCommandDeserializer>().AsSingle();

            Container.DeclareSignal<EffectTimingTriggedSignal>();
            Container.BindFactory<EffectProcesser, EffectProcesser.Facotry>();
        }
    }
}
