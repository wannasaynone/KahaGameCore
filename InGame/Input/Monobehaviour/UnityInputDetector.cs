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
                InputEventHanlder.RiseOptionInViewSelected();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.W) || UnityEngine.Input.GetKeyUp(KeyCode.UpArrow))
            {
                InputEventHanlder.RiseMoveToPreviousOptionInView();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.S) || UnityEngine.Input.GetKeyUp(KeyCode.DownArrow))
            {
                InputEventHanlder.RiseMoveToNextOptionInView();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.Escape) || UnityEngine.Input.GetKeyUp(KeyCode.I))
            {
                InputEventHanlder.RiseHideInventoryCalled();
            }
        }

        private void UpdateNormalInput()
        {
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                InputEventHanlder.RiseSingleTapped();
            }

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
            {
                InputEventHanlder.RiseMovingUp();
            }

            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
            {
                InputEventHanlder.RiseMovingDown();
            }

            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                InputEventHanlder.RiseMovingLeft();
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                InputEventHanlder.RiseMovingRight();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                InputEventHanlder.RiseInteracting();
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.I))
            {
                InputEventHanlder.RiseInventoryCalled();
            }
        }
    }
}
