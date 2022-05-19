namespace KahaGameCore.GameData
{
    public interface IGameStaticDataHandler
    {
        T[] Load<T>() where T : IGameData;
        System.Threading.Tasks.Task<T[]> LoadAsync<T>() where T : IGameData;
    }
}