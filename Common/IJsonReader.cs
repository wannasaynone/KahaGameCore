namespace KahaGameCore.Common
{
    public interface IJsonReader 
    {
        T Read<T>(string json);
    }
}