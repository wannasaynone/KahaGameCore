namespace KahaGameCore.GameData
{
    public class GameStaticDataDeserializer : IJsonReader
    {
        public T Read<T>(string json)
        {
            return JsonFx.Json.JsonReader.Deserialize<T>(json);
        }
    }
}