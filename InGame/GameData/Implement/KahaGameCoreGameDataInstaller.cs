using Zenject;

namespace KahaGameCore.GameData.Implement
{
    public class KahaGameCoreGameDataInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IJsonReader>().To<GameStaticDataDeserializer>().AsSingle();
            Container.Bind<IJsonWriter>().To<GameStaticDataSerializer>().AsSingle();
            Container.Bind<JsonSaveDataHandler>().AsSingle();
            Container.Bind<GameStaticDataManager>().AsSingle();
        }
    }
}