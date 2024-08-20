using System;
using UnityEngine;

namespace KahaGameCore.Input.Mono
{
    public class UnityInputDetector : MonoBehaviour
    {
        private void Update()
        {
            UpdateNormalInput();
            UpdateInViewInput();
        }

        private void UpdateInViewInput()
        {
            if (UnityEngine.Input.GetKeyUp(KeyCode.Return) || UnityEngine.Input.GetKeyUp(KeyCode.KeypadEnter) || UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                InputEventHanlder.UserInterface.RiseOptionInViewSelected();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.W) || UnityEngine.Input.GetKeyUp(KeyCode.UpArrow))
            {
                InputEventHanlder.UserInterface.RiseMoveToPreviousOptionInView();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.S) || UnityEngine.Input.GetKeyUp(KeyCode.DownArrow))
            {
                InputEventHanlder.UserInterface.RiseMoveToNextOptionInView();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.Escape) || UnityEngine.Input.GetKeyUp(KeyCode.I))
            {
                InputEventHanlder.UserInterface.RiseHideInventoryCalled();
            }
        }

        private void UpdateNormalInput()
        {
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                InputEventHanlder.Mouse.RiseSingleTapped();
            }

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
            {
                InputEventHanlder.Movement.RiseMovingUp();
            }

            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
            {
                InputEventHanlder.Movement.RiseMovingDown();
            }

            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                InputEventHanlder.Movement.RiseMovingLeft();
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                InputEventHanlder.Movement.RiseMovingRight();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                InputEventHanlder.Movement.RiseInteracting();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.I))
            {
                InputEventHanlder.UserInterface.RiseInventoryCalled();
            }
        }
    }
}
