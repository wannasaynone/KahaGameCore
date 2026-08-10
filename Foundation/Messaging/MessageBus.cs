using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Foundation.Messaging
{
    public static class MessageBus
    {
        private static readonly Dictionary<Type, List<Action<MessageBase>>> handlers =
            new Dictionary<Type, List<Action<MessageBase>>>();
        private static readonly Dictionary<int, Action<MessageBase>> wrappersByHandlerHash =
            new Dictionary<int, Action<MessageBase>>();

        public static void ForceClearAll()
        {
            handlers.Clear();
            wrappersByHandlerHash.Clear();
        }

        public static void Subscribe<T>(Action<T> handler) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.ContainsKey(messageType))
            {
                handlers[messageType] = new List<Action<MessageBase>>();
            }

            int handlerHash = handler.GetHashCode();
            if (wrappersByHandlerHash.ContainsKey(handlerHash))
            {
                Debug.LogError("MessageBus: Handler already subscribed, ignoring duplicate subscription.");
                return;
            }

            Action<MessageBase> wrapper = message => handler((T)message);
            wrappersByHandlerHash.Add(handlerHash, wrapper);
            handlers[messageType].Add(wrapper);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.ContainsKey(messageType))
            {
                return;
            }

            int handlerHash = handler.GetHashCode();
            if (!wrappersByHandlerHash.TryGetValue(handlerHash, out Action<MessageBase> wrapper))
            {
                return;
            }

            handlers[messageType].Remove(wrapper);
            wrappersByHandlerHash.Remove(handlerHash);
        }

        public static void Publish<T>(T message) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.TryGetValue(messageType, out List<Action<MessageBase>> subscribers))
            {
                return;
            }

            List<Action<MessageBase>> snapshot = new List<Action<MessageBase>>(subscribers);
            for (int index = 0; index < snapshot.Count; index++)
            {
                snapshot[index](message);
            }
        }
    }
}
