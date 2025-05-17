using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Reload : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_r = new GameObject("Input_R").AddComponent<InputDetector>();
            input_r.detectKey = KeyCode.R;
            input_r.OnPressed += OnReloadPressed;
            inputDetectors.Add(input_r);
            input_r.transform.SetParent(transform);
        }

        private void OnReloadPressed()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.Reload();
        }
    }
}