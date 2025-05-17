using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_PrepareAttack : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_rightMouse = new GameObject("Input_RightMouse").AddComponent<InputDetector>();
            input_rightMouse.detectMouseButton = 1;
            input_rightMouse.OnPressed += OnPrepareAttackPressDown;
            input_rightMouse.OnReleased += OnPrepareAttackPressUp;
            inputDetectors.Add(input_rightMouse);
            input_rightMouse.transform.SetParent(transform);
        }

        private void OnPrepareAttackPressDown()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.StartPrepareAttack();
        }

        private void OnPrepareAttackPressUp()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.EndPrepareAttack();
        }
    }
}