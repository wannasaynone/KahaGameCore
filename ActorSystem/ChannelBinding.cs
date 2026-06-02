using System;

namespace KahaGameCore.ActorSystem
{
    public class ChannelBinding
    {
        public int ChannelId { get; }
        public int Priority { get; }
        public Action<ActionContext> Handler { get; }

        public ChannelBinding(int channelId, int priority, Action<ActionContext> handler)
        {
            ChannelId = channelId;
            Priority = priority;
            Handler = handler;
        }
    }
}
