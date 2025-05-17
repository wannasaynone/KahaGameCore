using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Camera
{
    public class BackgroundParallax : MonoBehaviour
    {
        private static List<BackgroundParallax> instances = new List<BackgroundParallax>();

        public static void TickAll()
        {
            foreach (var instance in instances)
            {
                instance.Tick();
            }

            foreach (var instance in instances)
            {
                instance.TickLoop();
            }
        }

        [SerializeField] private float parallaxSpeed = 0f;
        [SerializeField] private float halfWidth = 0f;
        [SerializeField] private bool loopX = false;

        private BackgroundParallax referenceBackground = null;
        private Vector3 cameraLastPosition = new Vector3(-1000f, 0f, 0f);

        private void Start()
        {
            instances.Add(this);

            if (loopX && !name.Contains("Clone"))
            {
                BackgroundParallax clone = Instantiate(this);
                clone.transform.SetParent(transform.parent);
                clone.transform.localPosition = transform.localPosition + new Vector3(halfWidth, 0f, 0f);
                clone.transform.localScale = transform.localScale;
                clone.transform.localRotation = transform.localRotation;
                clone.referenceBackground = this;
                referenceBackground = clone;
            }
        }

        private void OnDestroy()
        {
            instances.Remove(this);
        }

        private void Tick()
        {
            if (CameraController.Instance == null)
            {
                return;
            }

            if (cameraLastPosition.x <= -1000f)
            {
                cameraLastPosition = CameraController.Instance.transform.position;
                return;
            }

            if (Mathf.Approximately(cameraLastPosition.x, CameraController.Instance.transform.position.x))
            {
                return;
            }

            Vector3 cameraDelta = CameraController.Instance.transform.position - cameraLastPosition;
            cameraLastPosition = CameraController.Instance.transform.position;

            Vector3 newPosition = transform.position + cameraDelta * parallaxSpeed;

            transform.position = newPosition;
        }

        public void TickLoop()
        {
            if (loopX)
            {
                if (referenceBackground == null)
                {
                    Debug.LogError("BackgroundParallax referenceTransform is not assigned. name: " + gameObject.name);
                    return;
                }

                float referenceBackground_left = referenceBackground.transform.position.x - referenceBackground.halfWidth;
                float referenceBackground_right = referenceBackground.transform.position.x + referenceBackground.halfWidth;

                if (referenceBackground_right >= CameraController.Instance.RightX
                    && referenceBackground.transform.position.x < CameraController.Instance.transform.position.x)
                {
                    transform.position = new Vector3(referenceBackground_right + halfWidth, transform.position.y, transform.position.z);
                }
                else if (referenceBackground_left <= CameraController.Instance.LeftX
                    && referenceBackground.transform.position.x > CameraController.Instance.transform.position.x)
                {
                    transform.position = new Vector3(referenceBackground_left - halfWidth, transform.position.y, transform.position.z);
                }
            }
        }
    }
}