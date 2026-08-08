using KahaGameCore.GameEvent;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Events
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
