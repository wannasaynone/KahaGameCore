using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
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
            if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Z))
            {
                InputEventHanlder.UserInterface.RiseOptionInViewSelected();
            }

            if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                InputEventHanlder.UserInterface.RiseMoveToPreviousOptionInView();
            }

            if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow))
            {
                InputEventHanlder.UserInterface.RiseMoveToNextOptionInView();
            }

            if (Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.I))
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
            if (Input.GetMouseButtonUp(0))
            {
                InputEventHanlder.Mouse.RiseSingleTapped();
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Up);
            }

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Down);
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Left);
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                moveDirectionInputOrder.Add(MoveDirection.Right);
            }

            if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Up);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Down);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
            {
                moveDirectionInputOrder.Remove(MoveDirection.Left);
                if (moveDirectionInputOrder.Count == 0)
                {
                    InputEventHanlder.Movement.RiseReleased();
                }
            }

            if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow))
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

            if (Input.GetKeyUp(KeyCode.Z))
            {
                InputEventHanlder.Movement.RiseInteracting();
            }

            if (Input.GetKeyUp(KeyCode.I))
            {
                InputEventHanlder.UserInterface.RiseInventoryCalled();
            }
        }
    }
}
