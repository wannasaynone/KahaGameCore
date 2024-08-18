using KahaGameCore.GameData;

namespace KahaGameCore.DialogueSystem
{
    public class DialogueData : IGameData
    {
        public int ID { get; set; }
        public int Line { get; set; }
        public string Command { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Arg3 { get; set; }
    }
}
