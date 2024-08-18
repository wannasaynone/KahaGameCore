using System;
using System.Collections.Generic;

namespace KahaGameCore.Input
{
    public static class InputEventHanlder
    {
        public static event Action OnSingleTapped;
        public static event Action OnDoubleTapped;
        public static event Action OnPressing;
        public static event Action<UnityEngine.Vector2> OnSwiped;
        public static event Action<UnityEngine.Vector2> OnDrag;
        public static event Action IsMovingUp;
        public static event Action IsMovingDown;
        public static event Action IsMovingLeft;
        public static event Action IsMovingRight;
        public static event Action IsInteracting;
        public static event Action OnOptionInViewSelected;
        public static event Action OnMoveToPreviousOptionInView;
        public static event Action MoveToNextOptionInView;
        public static event Action OnInventoryCalled;
        public static event Action OnHideInventoryCalled;

        private static readonly List<object> movementLocker = new List<object>();
        private static readonly List<object> interactionLocker = new List<object>();
        private static readonly List<object> mouseLocker = new List<object>();

        public static void LockMovement(object locker)
        {
            movementLocker.Add(locker);
        }

        public static void UnlockMovement(object locker)
        {
            movementLocker.Remove(locker);
        }

        public static void LockInteraction(object locker)
        {
            interactionLocker.Add(locker);
        }

        public static void UnlockInteraction(object locker)
        {
            interactionLocker.Remove(locker);
        }

        public static void LockMouse(object locker)
        {
            mouseLocker.Add(locker);
        }

        public static void UnlockMouse(object locker)
        {
            mouseLocker.Remove(locker);
        }

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
            if (interactionLocker.Count > 0)
                return;

            IsInteracting?.Invoke();
        }

        public static void RiseOptionInViewSelected()
        {
            OnOptionInViewSelected?.Invoke();
        }

        public static void RiseMoveToPreviousOptionInView()
        {
            OnMoveToPreviousOptionInView?.Invoke();
        }

        public static void RiseMoveToNextOptionInView()
        {
            MoveToNextOptionInView?.Invoke();
        }

        public static void RiseInventoryCalled()
        {
            OnInventoryCalled?.Invoke();
        }

        public static void RiseHideInventoryCalled()
        {
            OnHideInventoryCalled?.Invoke();
        }

        public static void ClearAllEvents()
        {
            OnSingleTapped = null;
            OnDoubleTapped = null;
            OnPressing = null;
            OnSwiped = null;
            OnDrag = null;
            IsMovingUp = null;
            IsMovingDown = null;
            IsMovingLeft = null;
            IsMovingRight = null;
            IsInteracting = null;
            OnOptionInViewSelected = null;
            OnMoveToPreviousOptionInView = null;
            MoveToNextOptionInView = null;
            OnInventoryCalled = null;
            OnHideInventoryCalled = null;
        }
    }
}