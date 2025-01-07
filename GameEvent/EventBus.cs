using System;
using System.Collections.Generic;

namespace KahaGameCore.GameEvent
{
    public static class EventBus
    {
        private static int runningPublishCount = 0;

        private static readonly Dictionary<Type, List<Action<GameEventBase>>> eventHandlers = new Dictionary<Type, List<Action<GameEventBase>>>();
        private static readonly Dictionary<int, Action<GameEventBase>> originHashCodeToWrapperHandler = new Dictionary<int, Action<GameEventBase>>();

        public static void ForceClearAll()
        {
            runningPublishCount = 0;
            eventHandlers.Clear();
        }

        public static void Subscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] = new List<Action<GameEventBase>>();
            }

            if (waitToRemoveHandlerHashCode.Contains(handler.GetHashCode()))
            {
                waitToRemoveHandlerHashCode.Remove(handler.GetHashCode());
                return;
            }

            Action<GameEventBase> wrapperHandler = (eventBase) => handler((T)eventBase);
            originHashCodeToWrapperHandler.Add(handler.GetHashCode(), wrapperHandler);

            eventHandlers[eventType].Add(wrapperHandler);
        }


        private static List<int> waitToRemoveHandlerHashCode = new List<int>();
        public static void Unsubscribe<T>(Action<T> handler) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            if (runningPublishCount > 0)
            {
                waitToRemoveHandlerHashCode.Add(handler.GetHashCode());
                return;
            }

            for (int i = 0; i < eventHandlers[eventType].Count; i++)
            {
                if (originHashCodeToWrapperHandler[handler.GetHashCode()] == eventHandlers[eventType][i])
                {
                    eventHandlers[eventType].RemoveAt(i);
                    originHashCodeToWrapperHandler.Remove(handler.GetHashCode());
                    break;
                }
            }
        }

        public static void Publish<T>(T eventToPublish) where T : GameEventBase
        {
            var eventType = typeof(T);

            if (!eventHandlers.ContainsKey(eventType))
            {
                return;
            }

            runningPublishCount++;
            foreach (var handler in eventHandlers[eventType])
            {
                handler(eventToPublish);
            }
            runningPublishCount--;

            if (runningPublishCount == 0)
            {
                for (int i = 0; i < waitToRemoveHandlerHashCode.Count; i++)
                {
                    UnsubscribeWithHashKey(waitToRemoveHandlerHashCode[i]);
                }
                waitToRemoveHandlerHashCode.Clear();
            }
        }

        private static void UnsubscribeWithHashKey(int hashCode)
        {
            Action<GameEventBase> warppedAction = originHashCodeToWrapperHandler[hashCode];
            List<Type> eventTypes = new List<Type>(eventHandlers.Keys);
            for (int i = 0; i < eventTypes.Count; i++)
            {
                if (eventHandlers[eventTypes[i]].Contains(warppedAction))
                {
                    eventHandlers[eventTypes[i]].Remove(warppedAction);
                    originHashCodeToWrapperHandler.Remove(hashCode);
                    break;
                }
            }
        }
    }

}