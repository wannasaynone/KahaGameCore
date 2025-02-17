using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace KahaGameCore.Package.DialogueSystem
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private GameObject cameraShakeRoot;

        private Vector3 targetPosition;
        private Transform originalTarget;
        public bool IsTrackingOtherTarget => originalTarget != null;

        private void Awake()
        {
            Teleporter.OnTeleported += ForceSetToTargetPosition;
        }

        private void FixedUpdate()
        {
            if (target != null)
            {
                targetPosition = target.position;
                targetPosition.z = -10; // Camera's z position is -10

                transform.position = Vector3.Lerp(transform.position, targetPosition, 0.1f);
            }
        }

        public void ChangeTrget(Transform inputTransform)
        {
            if (originalTarget == null)
            {
                originalTarget = target;
                target = inputTransform;
            }
        }

        public void ForceSetToTargetPosition()
        {
            if (target != null)
            {
                targetPosition = target.position;
                targetPosition.z = -10;
                transform.position = targetPosition;
            }
        }

        public void ResuemTarget()
        {
            if (originalTarget == null)
                return;

            target = originalTarget;
            transform.position = target.position;
            originalTarget = null;
        }

        public void ShakeCamera(float duration, float magnitude, System.Action onCompleted = null)
        {
            StartCoroutine(Shake(duration, magnitude, onCompleted));
        }

        public void MoveCamera(float x, float y, System.Action onCompleted = null)
        {
            GameObject trackingTargetDummy = new GameObject("[TrackingTargetDummy]");
            trackingTargetDummy.transform.position = new Vector3(x, y, 0);
            targetPosition = trackingTargetDummy.transform.position;
            targetPosition.z = -10;
            ChangeTrget(trackingTargetDummy.transform);

            StartCoroutine(IEWaitMoveCameraEnd(onCompleted));
        }

        private IEnumerator IEWaitMoveCameraEnd(System.Action onCompleted)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                yield return null;
            }

            onCompleted?.Invoke();
        }

        private bool IsShaking = false;
        private IEnumerator Shake(float duration, float magnitude, System.Action onCompleted)
        {
            if (IsShaking)
            {
                onCompleted?.Invoke();
                yield break;
            }

            IsShaking = true;

            cameraShakeRoot.transform.DOShakePosition(duration, magnitude);
            yield return new WaitForSeconds(duration);

            IsShaking = false;
            onCompleted?.Invoke();
        }
    }
}