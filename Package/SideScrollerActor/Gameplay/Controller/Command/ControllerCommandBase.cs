using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller.Command
{
    public abstract class ControllerCommandBase : MonoBehaviour
    {
        protected Actor controlTarget;

        protected List<InputDetector> inputDetectors = new List<InputDetector>();

        public void Bind(Actor controlTarget)
        {
            OnDisable();

            this.controlTarget = controlTarget;

            if (controlTarget == null)
            {
                gameObject.SetActive(false);
                return;
            }

            OnBind();
            gameObject.SetActive(true);
        }

        protected abstract void OnBind();

        private void OnDisable()
        {
            foreach (var inputDetector in inputDetectors)
            {
                inputDetector.ClearEvents();
                Destroy(inputDetector.gameObject);
            }
            inputDetectors.Clear();
            controlTarget = null;
        }
    }
}