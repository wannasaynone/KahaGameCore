using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    public class MouseFollower2D : MonoBehaviour
    {
        private UnityEngine.Camera mainCamera;

        private bool isAttacking = false;

        private void Update()
        {
            if (isAttacking)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            Vector3 mousePosition = Input.mousePosition;
            mousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

            transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        }
    }
}