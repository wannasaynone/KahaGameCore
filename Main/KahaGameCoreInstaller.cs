using Zenject;

namespace KahaGameCore.Main
{
    public class KahaGameCoreInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<Common.GameStaticDataManager>().AsSingle();
        }
    }
}
