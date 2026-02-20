using KahaGameCore.GameData;

namespace ProjectBSR.DialogueSystem
{
    public class DialogueData : IGameData
    {
        public int ID { get; private set; }
        public int Line { get; private set; }
        public string Command { get; private set; }
        public string Arg1 { get; private set; }
        public string Arg2 { get; private set; }
        public string Arg3 { get; private set; }
        public string Arg4 { get; private set; }
        public string Arg5 { get; private set; }
        public string Arg1_en { get; private set; }
        public string Arg2_en { get; private set; }
        public string Arg3_en { get; private set; }
        public string Arg4_en { get; private set; }
        public string Arg5_en { get; private set; }
        public string Arg1_hans { get; private set; }
        public string Arg2_hans { get; private set; }
        public string Arg3_hans { get; private set; }
        public string Arg4_hans { get; private set; }
        public string Arg5_hans { get; private set; }
        public string Arg1_jp { get; private set; }
        public string Arg2_jp { get; private set; }
        public string Arg3_jp { get; private set; }
        public string Arg4_jp { get; private set; }
        public string Arg5_jp { get; private set; }
    }
}