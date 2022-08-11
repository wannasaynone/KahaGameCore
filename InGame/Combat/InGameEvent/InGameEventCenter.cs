namespace KahaGameCore.Combat.InGameEvent
{
    public static class InGameEventCenter
    {
        public abstract class InGameEvent { }

        private class InGameEventHandler<T> where T : InGameEvent
        {
            private System.Collections.Generic.List<System.Action<T>> m_actions = new System.Collections.Generic.List<System.Action<T>>();
            private int m_currentActingIndex = 0;

            public void Add(System.Action<T> action)
            {
                if(!m_actions.Contains(action))
                {
                    m_actions.Add(action);
                }
            }

            public void Remove(System.Action<T> action)
            {
                if(m_actions.Remove(action))
                {
                    m_currentActingIndex--;
                }
            }

            public void Rise(T inGameEvent)
            {
                for(m_currentActingIndex = 0; m_currentActingIndex < m_actions.Count; m_currentActingIndex++)
                {
                    m_actions[m_currentActingIndex].Invoke(inGameEvent);
                }
            }
        }

        private static System.Collections.Generic.Dictionary<System.Type, object> m_typeToHandler = new System.Collections.Generic.Dictionary<System.Type, object>();

        public static void Register<T>(System.Action<T> action) where T : InGameEvent
        {
            if(!m_typeToHandler.ContainsKey(typeof(T)))
            {
                m_typeToHandler.Add(typeof(T), new InGameEventHandler<T>());
            }

            InGameEventHandler<T> _eventHandler = m_typeToHandler[typeof(T)] as InGameEventHandler<T>;
            _eventHandler.Add(action);
        }

        public static void Unregister<T>(System.Action<T> action) where T : InGameEvent
        {
            if (!m_typeToHandler.ContainsKey(typeof(T)))
            {
                m_typeToHandler.Add(typeof(T), new InGameEventHandler<T>());
            }

            InGameEventHandler<T> _eventHandler = m_typeToHandler[typeof(T)] as InGameEventHandler<T>;
            _eventHandler.Remove(action);
        }

        public static void Publish<T>(T inGameEvent) where T : InGameEvent
        {
            if (!m_typeToHandler.ContainsKey(typeof(T)))
            {
                m_typeToHandler.Add(typeof(T), new InGameEventHandler<T>());
            }

            InGameEventHandler<T> _eventHandler = m_typeToHandler[typeof(T)] as InGameEventHandler<T>;
            _eventHandler.Rise(inGameEvent);
        }
    }
}