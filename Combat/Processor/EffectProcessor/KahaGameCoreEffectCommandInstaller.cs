using Zenject;

namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public class KahaGameCoreEffectCommandInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);

            Container.Bind<EffectProcessor>().AsTransient();
            Container.Bind<EffectCommandFactoryContainer>().AsSingle();
            Container.Bind<EffectCommandDeserializer>().AsSingle();

            Container.DeclareSignal<EffectTimingTriggedSignal>();
            Container.BindFactory<EffectProcessor, EffectProcessor.Facotry>();
        }
    }
}
