using UnityEngine;
using DG.Tweening;

namespace KahaGameCore.Package.SideScrollerActor.Camera
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] private float cameraHalfWidth = 3.55f;
        [SerializeField] private UnityEngine.Camera referenceCamera;

        public Transform target;
        public Vector3 offset;
        public Vector3 targetAdditionOffset;
        public float lerpSpeed = 0.02f;

        public float RightX { get { return transform.position.x + cameraHalfWidth; } }
        public float LeftX { get { return transform.position.x - cameraHalfWidth; } }

        private float orginalOrthographicSize;
        private float targetOrthographicSize;
        private float currentOrthographicSize;
        private Vector3 currentTargetAdditionOffset;

        private void Awake()
        {
            Instance = this;
            orginalOrthographicSize = referenceCamera.orthographicSize;
            targetOrthographicSize = orginalOrthographicSize;
            currentOrthographicSize = orginalOrthographicSize;
        }

        public void SetOrthographicSize(float size)
        {
            targetOrthographicSize = size;
        }

        public void ResetOrthographicSize()
        {
            targetOrthographicSize = orginalOrthographicSize;
        }

        public void ResetOrthographicSizeImmediately()
        {
            targetOrthographicSize = orginalOrthographicSize;
            currentOrthographicSize = orginalOrthographicSize;
            referenceCamera.orthographicSize = orginalOrthographicSize;
        }

        private bool isShaking;
        private float startShakeY;

        public void Shake(float duration, float strength, int vibrato, float randomness)
        {
            if (isShaking)
            {
                return;
            }

            isShaking = true;
            startShakeY = referenceCamera.transform.position.y;
            referenceCamera.transform.DOShakePosition(duration, strength, vibrato, randomness)
                                     .OnUpdate(OnShakeTweenUpdate)
                                     .OnComplete(OnShakeTweenComplete);
        }

        private void OnShakeTweenUpdate()
        {
            if (referenceCamera.transform.position.y < startShakeY)
            {
                referenceCamera.transform.position = new Vector3(referenceCamera.transform.position.x, startShakeY, referenceCamera.transform.position.z);
            }
        }

        private void OnShakeTweenComplete()
        {
            isShaking = false;
        }

        public void SetToTargetPositionImmediately()
        {
            if (target == null)
            {
                return;
            }

            currentOrthographicSize = targetOrthographicSize;
            referenceCamera.orthographicSize = targetOrthographicSize;

            currentTargetAdditionOffset = targetAdditionOffset;
            Vector3 targetPosition = target.position + offset + currentTargetAdditionOffset;
            targetPosition.z = transform.position.z;

            if (targetPosition.x - cameraHalfWidth < BoardSetter.MIN_X)
            {
                targetPosition = new Vector3(BoardSetter.MIN_X + cameraHalfWidth, targetPosition.y, targetPosition.z);
            }
            else if (targetPosition.x + cameraHalfWidth > BoardSetter.MAX_X)
            {
                targetPosition = new Vector3(BoardSetter.MAX_X - cameraHalfWidth, targetPosition.y, targetPosition.z);
            }

            transform.position = targetPosition;
        }

        private void LateUpdate()
        {
            float orthographicSizeLerpSpeed = 0.05f;

            if (currentOrthographicSize < targetOrthographicSize)
            {
                orthographicSizeLerpSpeed = 0.02f;
            }

            currentTargetAdditionOffset = Vector3.Lerp(currentTargetAdditionOffset, targetAdditionOffset, orthographicSizeLerpSpeed * 6f);
            currentOrthographicSize = Mathf.Lerp(currentOrthographicSize, targetOrthographicSize, orthographicSizeLerpSpeed);
            referenceCamera.orthographicSize = currentOrthographicSize;

            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + offset + currentTargetAdditionOffset;
            targetPosition.z = transform.position.z;

            if (targetPosition.x - cameraHalfWidth < BoardSetter.MIN_X)
            {
                targetPosition = new Vector3(BoardSetter.MIN_X + cameraHalfWidth, targetPosition.y, targetPosition.z);
            }
            else if (targetPosition.x + cameraHalfWidth > BoardSetter.MAX_X)
            {
                targetPosition = new Vector3(BoardSetter.MAX_X - cameraHalfWidth, targetPosition.y, targetPosition.z);
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed);

            BackgroundParallax.TickAll();
        }
    }
}