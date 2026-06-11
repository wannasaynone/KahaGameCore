using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events
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
