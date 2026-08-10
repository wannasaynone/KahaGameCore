using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Events
{
    public class LocationChangedEvent : MessageBase
    {
        public LocationData Location { get; }

        public LocationChangedEvent(LocationData location)
        {
            Location = location;
        }
    }
}
