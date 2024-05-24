using System;
using System.Collections.Generic;

namespace KahaGameCore.GameEvent
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Action<GameEventBase>>> _eventHandlers = new Dictionary<Type, List<Action<GameEventBase>>>();
        private static readonly Dictionary<int, Action<GameEventBase>> originHashCodeToWrapperHandler = new Dictionary<int, Action<GameEventBase>>();

        public static void ForceClearAll()
        {
            _eventHandlers.Clear();
        }

        public static void Subscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Action<GameEventBase>>();
            }

            Action<GameEventBase> wrapperHandler = (eventBase) => handler((T)eventBase);
            originHashCodeToWrapperHandler.Add(handler.GetHashCode(), wrapperHandler);

            _eventHandlers[eventType].Add(wrapperHandler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            for (int i = 0; i < _eventHandlers[eventType].Count; i++)
            {
                if (originHashCodeToWrapperHandler[handler.GetHashCode()] == _eventHandlers[eventType][i])
                {
                    _eventHandlers[eventType].RemoveAt(i);
                    originHashCodeToWrapperHandler.Remove(handler.GetHashCode());
                    break;
                }
            }
        }

        public static void Publish<T>(T eventToPublish) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            foreach (var handler in _eventHandlers[eventType])
            {
                handler?.Invoke(eventToPublish);
            }
        }
    }

}