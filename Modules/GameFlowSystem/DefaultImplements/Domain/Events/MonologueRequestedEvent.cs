using KahaGameCore.Foundation.Messaging;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Events
{
    /// <summary>要求 HUD 顯示一段主角自言自語。</summary>
    public class MonologueRequestedEvent : MessageBase
    {
        public string Text { get; }

        public MonologueRequestedEvent(string text)
        {
            Text = text;
        }
    }
}
