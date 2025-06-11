using KahaGameCore.GameData;

namespace KahaGameCore.Package.DialogueSystem
{
    public class DialogueData : IGameData
    {
        public int ID { get; set; }
        public int Line { get; set; }
        public string Command { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Arg3 { get; set; }
        public string Arg4 { get; set; }
        public string Arg1_en { get; set; }
        public string Arg2_en { get; set; }
        public string Arg3_en { get; set; }
        public string Arg1_hans { get; set; }
        public string Arg2_hans { get; set; }
        public string Arg3_hans { get; set; }
    }
}
