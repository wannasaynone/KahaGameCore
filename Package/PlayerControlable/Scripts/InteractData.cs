using KahaGameCore.GameData;

namespace KahaGameCore.Package.PlayerControlable
{
    public class InteractData : IGameData
    {
        public int ID { get; set; }
        public string InteractTargetTag { get; set; }
        public string ActionType { get; set; }
        public string RequireDayArrayString { get; set; } // or
        public string RequireTimeArrayString { get; set; } // or
        public string RquireValueArrayString { get; set; } // and
        public string ReturnValueString { get; set; }
    }
}