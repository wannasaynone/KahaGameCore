namespace KahaGameCore.Serialization
{
    public interface IJsonReader 
    {
        T Read<T>(string json);
    }
}