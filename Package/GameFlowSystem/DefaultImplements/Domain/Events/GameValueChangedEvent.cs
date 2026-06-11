using KahaGameCore.GameEvent;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events
{
    public class GameValueChangedEvent : GameEventBase
    {
        public string Tag { get; }
        public int NewValue { get; }

        public GameValueChangedEvent(string tag, int newValue)
        {
            Tag = tag;
            NewValue = newValue;
        }
    }
}
