using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public static class InputDetector
    {
        private static List<object> movementLockers = new List<object>();

        public static bool IsMovementLocked()
        {
            return movementLockers.Count > 0;
        }

        public static void LockMovement(object locker)
        {
            movementLockers.Add(locker);
        }

        public static void UnlockMovement(object locker)
        {
            movementLockers.Remove(locker);
        }

        public static bool IsMovingUp()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        }

        public static bool IsMovingDown()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }

        public static bool IsMovingLeft()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        }

        public static bool IsMovingRight()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        }
        public static bool IsInteracting()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKeyUp(KeyCode.Z);
        }
        //----------------------------------------------------------------------
        public static bool IsKeyPressUp()
        {
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        public static bool IsKeyPressDown()
        {
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        }

        public static bool IsKeyPressLeft()
        {
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        }

        public static bool IsKeyPressRight()
        {
            return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        }
        public static bool IsKeyPressInteracting()
        {
            return Input.GetKeyDown(KeyCode.Z);
        }
        //----------------------------------------------------------------------

        public static bool IsSelectingInView()
        {
            return Input.GetMouseButtonDown(0) || Input.GetKeyUp(KeyCode.Z);
        }

        public static bool IsMovingToPreviousOptionInView()
        {
            return Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        }

        public static bool IsMoviongToNextOptionInView()
        {
            return Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow);
        }

        public static bool IsCallingInventory()
        {
            if (movementLockers.Count > 0)
                return false;

            return Input.GetKeyUp(KeyCode.I);
        }

        public static bool IsHidingInventory()
        {
            return Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.I);
        }
    }
}