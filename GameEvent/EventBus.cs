using System;
using System.Collections.Generic;

namespace KahaGameCore.GameEvent
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Action<GameEventBase>>> eventHandlers = new Dictionary<Type, List<Action<GameEventBase>>>();
        private static readonly Dictionary<int, Action<GameEventBase>> originHashCodeToWrapperHandler = new Dictionary<int, Action<GameEventBase>>();

        public static void ForceClearAll()
        {
            eventHandlers.Clear();
            originHashCodeToWrapperHandler.Clear();
        }

        public static void Subscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Action<GameEventBase>>();
            }

            int handlerHashCode = handler.GetHashCode();
            if (originHashCodeToWrapperHandler.ContainsKey(handlerHashCode))
            {
                UnityEngine.Debug.LogError("EventBus: Handler already subscribed, ignoring duplicate subscription.");
                return;
            }

            Action<GameEventBase> wrapperHandler = (eventBase) => handler((T)eventBase);

            originHashCodeToWrapperHandler.Add(handlerHashCode, wrapperHandler);
            eventHandlers[eventType].Add(wrapperHandler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            int handlerHashCode = handler.GetHashCode();
            if (!originHashCodeToWrapperHandler.ContainsKey(handlerHashCode))
            {
                return;
            }

            var wrapperHandler = originHashCodeToWrapperHandler[handlerHashCode];
            eventHandlers[eventType].Remove(wrapperHandler);
            originHashCodeToWrapperHandler.Remove(handlerHashCode);
        }

        public static void Publish<T>(T eventToPublish) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            var handlersCopy = new List<Action<GameEventBase>>(eventHandlers[eventType]);
            foreach (var handler in handlersCopy)
            {
                handler(eventToPublish);
            }
        }
    }
}
