using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Defense : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_s = new GameObject("Input_S").AddComponent<InputDetector>();
            input_s.detectKey = KeyCode.S;
            input_s.OnPressed += Defense;
            inputDetectors.Add(input_s);
            input_s.transform.SetParent(transform);
        }

        private void Defense()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.Defense();
        }
    }
}