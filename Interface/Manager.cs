using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Interface
{
    public abstract class Manager
    {
        private static List<View> m_views = new List<View>();
        private static Dictionary<System.Type, UIView> m_gameObjectNameToUIViews = new Dictionary<System.Type, UIView>();

        public static void RegisterView(View view)
        {
            if (m_views.Contains(view))
            {
                Debug.LogErrorFormat("{0} was registered", view.GetType().Name);
                return;
            }

            UIView _uiView = view as UIView;
            if (_uiView != null)
            {
                if (m_gameObjectNameToUIViews.ContainsValue(_uiView))
                {
                    Debug.LogErrorFormat("Page {0} was registered but is trying to register ui page", view.GetType().Name);
                    return;
                }
                m_gameObjectNameToUIViews.Add(_uiView.GetType(), _uiView);
            }

            m_views.Add(view);
        }

        public static void UnregisterView(View view)
        {
            if (!m_views.Contains(view))
            {
                Debug.LogErrorFormat("{0} was not registered but is trying to unregister", view.GetType().Name);
                return;
            }

            UIView _uiView = view as UIView;
            if (_uiView != null)
            {
                m_gameObjectNameToUIViews.Remove(_uiView.GetType());
            }

            m_views.Remove(view);
        }

#if UNITY_EDITOR
        public Manager()
        {
            System.Diagnostics.StackTrace _stackTrace = new System.Diagnostics.StackTrace();
            for (int i = 0; i < _stackTrace.GetFrames().Length; i++)
            {
                string _methodName = _stackTrace.GetFrames()[i].GetMethod().Name;
                if (_methodName == "Awake")
                {
                    Debug.LogError("Manager CAN'T be created in any Awake() function to make sure GetView<T> working");
                }
            }
        }
#endif

        protected T GetPage<T>() where T : UIView
        {
            if(!m_gameObjectNameToUIViews.ContainsKey(typeof(T)))
            {
                return null;
            }

            return m_gameObjectNameToUIViews[typeof(T)] as T;
        }

        protected List<T> GetViews<T>() where T : View
        {
            List<T> _views = new List<T>();

            for (int i = 0; i < m_views.Count; i++)
            {
                if (m_views[i] is T)
                {
                    _views.Add((T)m_views[i]);
                }
            }
            return _views;
        }

        protected T GetView<T>(int instanceID) where T : View
        {
            for (int i = 0; i < m_views.Count; i++)
            {
                if (m_views[i] is T && m_views[i].GetInstanceID() == instanceID)
                {
                    return m_views[i] as T;
                }
            }

            return null;
        }
    }
}

