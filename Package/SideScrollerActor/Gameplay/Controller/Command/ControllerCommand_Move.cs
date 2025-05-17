using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Move : ControllerCommandBase
    {
        private const float DASH_DOUBLE_TAP_TIME = 0.55f;
        private float dashDoubleTapTimer = 0f;

        private bool isPressingLeft = false;
        private bool isPressingRight = false;
        private bool isPressingSpecial = false;

        protected override void OnBind()
        {
            dashDoubleTapTimer = 0f;
            isPressingLeft = false;
            isPressingRight = false;
            isPressingSpecial = false;

            InputDetector input_a = new GameObject("Input_A").AddComponent<InputDetector>();
            input_a.detectKey = KeyCode.A;
            input_a.OnPressed += SetInPressingLeft;
            input_a.OnPressed += PrepareDashLeft;
            input_a.OnReleased += SetInReleasedLeft;
            inputDetectors.Add(input_a);
            input_a.transform.SetParent(transform);

            InputDetector input_d = new GameObject("Input_D").AddComponent<InputDetector>();
            input_d.detectKey = KeyCode.D;
            input_d.OnPressed += SetInPressingRight;
            input_d.OnPressed += PrepareDashRight;
            input_d.OnReleased += SetInReleasedRight;
            inputDetectors.Add(input_d);
            input_d.transform.SetParent(transform);

            InputDetector input_shift = new GameObject("Input_Shift").AddComponent<InputDetector>();
            input_shift.detectKey = KeyCode.LeftShift;
            input_shift.OnPressed += SetInPressingSpecial;
            input_shift.OnReleased += SetInReleasedSpecial;
            inputDetectors.Add(input_shift);
            input_shift.transform.SetParent(transform);
        }

        private bool readyDashRight = false;
        private bool readyDashLeft = false;

        private void PrepareDashLeft()
        {
            if (controlTarget == null) return;

            if (!readyDashLeft && !readyDashRight)
            {
                readyDashLeft = true;
                readyDashRight = false;
                dashDoubleTapTimer = 0f;
            }
            else if (readyDashLeft)
            {
                if (controlTarget != null) controlTarget.DashLeft();
                readyDashLeft = false;
                readyDashRight = false;
                dashDoubleTapTimer = 0f;
            }
        }

        private void PrepareDashRight()
        {
            if (controlTarget == null) return;

            if (!readyDashLeft && !readyDashRight)
            {
                readyDashRight = true;
                readyDashLeft = false;
                dashDoubleTapTimer = 0f;
            }
            else if (readyDashRight)
            {
                controlTarget.DashRight();
                readyDashLeft = false;
                readyDashRight = false;
                dashDoubleTapTimer = 0f;
            }
        }

        private void MoveRight()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf)
            {
                if (isPressingSpecial)
                {
                    controlTarget.RunRight();
                    if (controlTarget.currentStamina < controlTarget.actorSetting.runCostPerSecond * Time.deltaTime)
                    {
                        SetInReleasedSpecial();
                    }
                }
                else
                {
                    controlTarget.MoveRight();
                }
            }
        }

        private void MoveLeft()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf)
            {
                if (isPressingSpecial)
                {
                    controlTarget.RunLeft();
                    if (controlTarget.currentStamina < controlTarget.actorSetting.runCostPerSecond * Time.deltaTime)
                    {
                        SetInReleasedSpecial();
                    }
                }
                else
                {
                    controlTarget.MoveLeft();
                }
            }
        }

        private void SetInPressingLeft()
        {
            isPressingLeft = true;
        }

        private void SetInReleasedLeft()
        {
            isPressingLeft = false;

            if (!isPressingRight)
            {
                StopMove();
            }
        }

        private void SetInPressingRight()
        {
            isPressingRight = true;
        }

        private void SetInReleasedRight()
        {
            isPressingRight = false;

            if (!isPressingLeft)
            {
                StopMove();
            }
        }

        private void StopMove()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.SetToIdle();
        }

        private void SetInPressingSpecial()
        {
            isPressingSpecial = true;
        }

        private void SetInReleasedSpecial()
        {
            isPressingSpecial = false;
        }

        private void Update()
        {
            if (readyDashLeft || readyDashRight)
            {
                dashDoubleTapTimer += Time.deltaTime;
                if (dashDoubleTapTimer >= DASH_DOUBLE_TAP_TIME)
                {
                    readyDashLeft = false;
                    readyDashRight = false;
                    dashDoubleTapTimer = 0f;
                }
            }

            if (isPressingLeft && isPressingRight)
            {
                StopMove();
            }
            else if (isPressingLeft)
            {
                MoveLeft();
            }
            else if (isPressingRight)
            {
                MoveRight();
            }
        }
    }
}