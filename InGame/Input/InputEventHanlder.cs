using System;
using System.Collections.Generic;

namespace KahaGameCore.Input
{
    public static class InputEventHanlder
    {
        public static class Movement
        {
            public static event Action IsMovingUp;
            public static event Action IsMovingDown;
            public static event Action IsMovingLeft;
            public static event Action IsMovingRight;
            public static event Action IsInteracting;

            public static void RiseMovingUp()
            {
                if (movementLocker.Count > 0)
                    return;

                IsMovingUp?.Invoke();
            }

            public static void RiseMovingDown()
            {
                if (movementLocker.Count > 0)
                    return;

                IsMovingDown?.Invoke();
            }

            public static void RiseMovingLeft()
            {
                if (movementLocker.Count > 0)
                    return;

                IsMovingLeft?.Invoke();
            }

            public static void RiseMovingRight()
            {
                if (movementLocker.Count > 0)
                    return;

                IsMovingRight?.Invoke();
            }

            public static void RiseInteracting()
            {
                if (movementLocker.Count > 0)
                    return;

                IsInteracting?.Invoke();
            }
        }

        public static class Mouse
        {
            public static event Action OnSingleTapped;
            public static event Action OnDoubleTapped;
            public static event Action OnPressing;
            public static event Action<UnityEngine.Vector2> OnSwiped;
            public static event Action<UnityEngine.Vector2> OnDrag;

            public static void RiseSingleTapped()
            {
                if (mouseLocker.Count > 0)
                    return;

                OnSingleTapped?.Invoke();
            }

            public static void RiseDoubleTapped()
            {
                if (mouseLocker.Count > 0)
                    return;

                OnDoubleTapped?.Invoke();
            }

            public static void RisePressing()
            {
                if (mouseLocker.Count > 0)
                    return;

                OnPressing?.Invoke();
            }

            public static void RiseSwiped(UnityEngine.Vector2 direction)
            {
                if (mouseLocker.Count > 0)
                    return;

                OnSwiped?.Invoke(direction);
            }

            public static void RiseDrag(UnityEngine.Vector2 direction)
            {
                if (mouseLocker.Count > 0)
                    return;

                OnDrag?.Invoke(direction);
            }
        }

        public class UserInterface
        {
            public static event Action OnOptionInViewSelected;
            public static event Action OnMoveToPreviousOptionInView;
            public static event Action MoveToNextOptionInView;
            public static event Action OnInventoryCalled;
            public static event Action OnHideInventoryCalled;

            public static void RiseOptionInViewSelected()
            {
                if (userInterfaceLocker.Count > 0)
                    return;

                OnOptionInViewSelected?.Invoke();
            }

            public static void RiseMoveToPreviousOptionInView()
            {
                if (userInterfaceLocker.Count > 0)
                    return;

                OnMoveToPreviousOptionInView?.Invoke();
            }

            public static void RiseMoveToNextOptionInView()
            {
                if (userInterfaceLocker.Count > 0)
                    return;

                MoveToNextOptionInView?.Invoke();
            }

            public static void RiseInventoryCalled()
            {
                if (userInterfaceLocker.Count > 0)
                    return;

                OnInventoryCalled?.Invoke();
            }

            public static void RiseHideInventoryCalled()
            {
                if (userInterfaceLocker.Count > 0)
                    return;

                OnHideInventoryCalled?.Invoke();
            }

        }


        private static readonly List<object> movementLocker = new List<object>();
        private static readonly List<object> mouseLocker = new List<object>();
        private static readonly List<object> userInterfaceLocker = new List<object>();

        public static void LockMovement(object locker)
        {
            movementLocker.Add(locker);
        }

        public static void UnlockMovement(object locker)
        {
            movementLocker.Remove(locker);
        }

        public static void LockMouse(object locker)
        {
            mouseLocker.Add(locker);
        }

        public static void UnlockMouse(object locker)
        {
            mouseLocker.Remove(locker);
        }

        public static void LockUserInterface(object locker)
        {
            userInterfaceLocker.Add(locker);
        }

        public static void UnlockUserInterface(object locker)
        {
            userInterfaceLocker.Remove(locker);
        }
    }
}