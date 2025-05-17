using DG.Tweening;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public class Sight : MonoBehaviour
    {
        private Camera mainCamera;

        private bool isRunningSimpleAdditiveRotation;

        public void SimplePingPongRotation(float angle, float duration)
        {
            if (isRunningSimpleAdditiveRotation)
            {
                return;
            }

            isRunningSimpleAdditiveRotation = true;

            transform.DORotate(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + angle), duration / 2f).OnComplete(() =>
            {
                transform.DORotate(new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z - angle), duration / 2f).OnComplete(() =>
                {
                    isRunningSimpleAdditiveRotation = false;
                });
            });
        }

        private void Update()
        {
            if (isRunningSimpleAdditiveRotation)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mousePos - transform.position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (angle > 90f || angle < -90f)
            {
                transform.eulerAngles += new Vector3(180f, 0f, 0f);
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, -transform.eulerAngles.z);
            }
            else
            {
                transform.eulerAngles += new Vector3(0f, 0f, 0f);
            }
        }
    }
}