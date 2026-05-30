using System;

namespace KahaGameCore.ActorSystem
{
    public class ChannelBinding
    {
        public int ChannelId { get; }
        public int Priority { get; }
        public Action<IActor> Handler { get; }

        public ChannelBinding(int channelId, int priority, Action<IActor> handler)
        {
            ChannelId = channelId;
            Priority = priority;
            Handler = handler;
        }
    }
}
