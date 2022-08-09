namespace KahaGameCore.GameData
{
    public interface IJsonReader 
    {
        T Read<T>(string json);
    }
}