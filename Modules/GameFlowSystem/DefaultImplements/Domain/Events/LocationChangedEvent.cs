using KahaGameCore.GameEvent;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Events
{
    public class LocationChangedEvent : GameEventBase
    {
        public LocationData Location { get; }

        public LocationChangedEvent(LocationData location)
        {
            Location = location;
        }
    }
}
