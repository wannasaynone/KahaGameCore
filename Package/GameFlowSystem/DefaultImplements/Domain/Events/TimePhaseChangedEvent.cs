using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events
{
    public class TimePhaseChangedEvent : GameEventBase
    {
        public TimePhaseData Phase { get; }
        public int Day { get; }

        public TimePhaseChangedEvent(TimePhaseData phase, int day)
        {
            Phase = phase;
            Day = day;
        }
    }
}
