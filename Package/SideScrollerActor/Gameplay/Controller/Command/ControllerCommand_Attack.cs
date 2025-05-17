using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Attack : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_leftMouse = new GameObject("Input_LeftMouse").AddComponent<InputDetector>();
            input_leftMouse.detectMouseButton = 0;
            input_leftMouse.OnPressed += Attack;
            inputDetectors.Add(input_leftMouse);
            input_leftMouse.transform.SetParent(transform);
        }

        private void Attack()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.AttackWithWeapon();
        }
    }
}