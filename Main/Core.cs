using Zenject;

namespace KahaGameCore.Main
{
    public class Core
    {
        [Inject]
        public Common.GameDataManager GameDataManager { get; private set; }
    }
}
