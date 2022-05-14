using Zenject;

namespace KahaGameCore.Main
{
    public class Core
    {
        [Inject]
        public Common.GameStaticDataManager GameDataManager { get; private set; }
    }
}
