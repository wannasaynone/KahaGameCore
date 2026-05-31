using System;

namespace KahaGameCore.ActorSystem
{
    public class ChannelBinding
    {
        public int ChannelId { get; }
        public int Priority { get; }
        public Action<AGameActor, ActionContext> Handler { get; }

        public ChannelBinding(int channelId, int priority, Action<AGameActor, ActionContext> handler)
        {
            ChannelId = channelId;
            Priority = priority;
            Handler = handler;
        }
    }
}
