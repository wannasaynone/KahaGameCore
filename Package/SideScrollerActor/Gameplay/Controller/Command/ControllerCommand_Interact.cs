using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public class ControllerCommand_Interact : ControllerCommandBase
    {
        protected override void OnBind()
        {
            InputDetector input_e = new GameObject("Input_E").AddComponent<InputDetector>();
            input_e.detectKey = KeyCode.E;
            input_e.OnPressed += Interact;
            inputDetectors.Add(input_e);
            input_e.transform.SetParent(transform);
        }

        private void Interact()
        {
            if (controlTarget != null && controlTarget.gameObject.activeSelf) controlTarget.Interact();
        }
    }
}