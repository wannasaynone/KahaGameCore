using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Events
{
    public class TimePhaseChangedEvent : MessageBase
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
