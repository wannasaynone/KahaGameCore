using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Input.Mono
{
    public class UnityInputDetector : MonoBehaviour
    {
        private void Update()
        {
            UpdateInViewInput();
            UpdateNormalInput();
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

        private enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
            None
        }
        private List<MoveDirection> moveDirectionInputOrder = new List<MoveDirection>();
        private void UpdateNormalInput()
        {
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                InputEventHanlder.Mouse.RiseSingleTapped();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Up);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Down);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Left);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Right);
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.W) || UnityEngine.Input.GetKeyUp(KeyCode.UpArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Up);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.S) || UnityEngine.Input.GetKeyUp(KeyCode.DownArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Down);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.A) || UnityEngine.Input.GetKeyUp(KeyCode.LeftArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Left);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (UnityEngine.Input.GetKeyUp(KeyCode.D) || UnityEngine.Input.GetKeyUp(KeyCode.RightArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Right);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (moveDirectionInputOrder.Count > 0)
                switch (moveDirectionInputOrder[moveDirectionInputOrder.Count - 1])
                {
                    case MoveDirection.Up:
                        InputEventHanlder.Movement.RiseMovingUp();
                        break;
                    case MoveDirection.Down:
                        InputEventHanlder.Movement.RiseMovingDown();
                        break;
                    case MoveDirection.Left:
                        InputEventHanlder.Movement.RiseMovingLeft();
                        break;
                    case MoveDirection.Right:
                        InputEventHanlder.Movement.RiseMovingRight();
                        break;
                    case MoveDirection.None:
                        InputEventHanlder.Movement.RiseReleased();
                        break;
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
