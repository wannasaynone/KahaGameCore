using System;

namespace KahaGameCore.Input
{
    public static class InputEventHanlder
    {
        public static event Action OnSingleTapped;
        public static event Action OnDoubleTapped;
        public static event Action OnPressing;
        public static event Action<UnityEngine.Vector2> OnSwiped;
        public static event Action<UnityEngine.Vector2> OnDrag;

        public static void SendOnSingleTapped()
        {
            OnSingleTapped?.Invoke();
        }

        public static void SendOnDoubleTapped()
        {
            OnDoubleTapped?.Invoke();
        }

        public static void SendOnPressing()
        {
            OnPressing?.Invoke();
        }

        public static void SendOnSwiped(UnityEngine.Vector2 direction)
        {
            OnSwiped?.Invoke(direction);
        }

        public static void SendOnDraged(UnityEngine.Vector2 direction)
        {
            OnDrag?.Invoke(direction);
        }
    }
}