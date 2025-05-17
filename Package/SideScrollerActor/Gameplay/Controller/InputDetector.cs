using System;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay.Controller
{
    public class InputDetector : MonoBehaviour
    {
        public event Action OnPressed;
        public event Action OnReleased;

        public KeyCode detectKey = KeyCode.None;
        public int detectMouseButton = -1;

        public void ClearEvents()
        {
            OnPressed = null;
            OnReleased = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(detectKey))
            {
                OnPressed?.Invoke();
            }

            if (Input.GetKeyUp(detectKey))
            {
                OnReleased?.Invoke();
            }

            if (detectMouseButton != -1)
            {
                if (Input.GetMouseButtonDown(detectMouseButton))
                {
                    OnPressed?.Invoke();
                }

                if (Input.GetMouseButtonUp(detectMouseButton))
                {
                    OnReleased?.Invoke();
                }
            }
        }
    }
}