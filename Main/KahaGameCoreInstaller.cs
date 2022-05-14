using Zenject;

namespace KahaGameCore.Main
{
    public class KahaGameCoreInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<Common.GameDataManager>().AsSingle();
        }
    }
}
