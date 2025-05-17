using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Jump : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_w = new GameObject("Input_W").AddComponent<InputDetector>();
            input_w.detectKey = KeyCode.W;
            input_w.OnPressed += SimpleJump;
            inputDetectors.Add(input_w);
            input_w.transform.SetParent(transform);
        }

        private void SimpleJump()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.SimpleJumpUp();
        }
    }
}