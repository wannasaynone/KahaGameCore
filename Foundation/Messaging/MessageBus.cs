using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Foundation.Messaging
{
    public static class MessageBus
    {
        private static readonly Dictionary<Type, Dictionary<Delegate, Action<MessageBase>>> handlers =
            new Dictionary<Type, Dictionary<Delegate, Action<MessageBase>>>();

        public static void ForceClearAll()
        {
            handlers.Clear();
        }

        public static void Subscribe<T>(Action<T> handler) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.TryGetValue(
                    messageType,
                    out Dictionary<Delegate, Action<MessageBase>> subscribers))
            {
                subscribers = new Dictionary<Delegate, Action<MessageBase>>();
                handlers.Add(messageType, subscribers);
            }

            if (subscribers.ContainsKey(handler))
            {
                Debug.LogError("MessageBus: Handler already subscribed, ignoring duplicate subscription.");
                return;
            }

            Action<MessageBase> wrapper = message => handler((T)message);
            subscribers.Add(handler, wrapper);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.TryGetValue(
                    messageType,
                    out Dictionary<Delegate, Action<MessageBase>> subscribers))
            {
                return;
            }

            if (!subscribers.Remove(handler))
            {
                return;
            }

            if (subscribers.Count == 0)
            {
                handlers.Remove(messageType);
            }
        }

        public static void Publish<T>(T message) where T : MessageBase
        {
            Type messageType = typeof(T);
            if (!handlers.TryGetValue(
                    messageType,
                    out Dictionary<Delegate, Action<MessageBase>> subscribers))
            {
                return;
            }

            List<Action<MessageBase>> snapshot =
                new List<Action<MessageBase>>(subscribers.Values);
            for (int index = 0; index < snapshot.Count; index++)
            {
                snapshot[index](message);
            }
        }
    }
}
