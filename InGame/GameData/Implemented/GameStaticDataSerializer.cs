namespace KahaGameCore.GameData.Implemented
{
    public class GameStaticDataSerializer : IJsonWriter
    {
        public string Write(object obj)
        {
            return JsonFx.Json.JsonWriter.Serialize(obj);
        }
    }
}