using System.Collections;
using Assets.SurvivorGameCore.ActorController;
using KahaGameCore.Package.ActorSystem.Definition;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KahaGameCore.Package.ActorSystem.Entities
{
    public class AimStateController : ControllerBase
    {
        [SerializeField] private SimpleMoveController normalMoveController;
        [SerializeField] private SimpleMoveController aimMoveController;
        [SerializeField] private AnimationController normalAnimationController;
        [SerializeField] private AnimationController aimAnimationController;
        [SerializeField] private CinemachineCamera normalCamera;
        [SerializeField] private CinemachineCamera aimCamera;
        [SerializeField] private CinemachineOrbitalFollow normalCameraFollower;
        [SerializeField] private CinemachineCamera aimBlendToNormalCamera;

        private InputAction aimAction;

        private void Start()
        {
            aimAction = InputSystem.actions.FindAction("Aim");
            if (aimAction == null)
            {
                Debug.LogError("Aim action not found in Input System.");
            }

            SetAiming(false);
            aimAction.performed += OnAimPerformed;
            aimAction.canceled += OnAimCanceled;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnAimPerformed(InputAction.CallbackContext context)
        {
            SetAiming(true);
            controlTarget.Animator.CrossFade("AimIdle", 0.1f, 0, 0f);
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found for camera-relative movement.");
                return;
            }
            Vector3 cameraForward = mainCamera.transform.forward;

            // Project forward and right vectors onto the XZ plane
            cameraForward.y = 0;
            cameraForward.Normalize();

            controlTarget.UpdateFacingDirection(cameraForward, 180f);
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            SetAiming(false);
            controlTarget.Animator.CrossFade("Idle", 0.1f, 0, 0f);
        }

        protected override void OnTick()
        {
            if (aimCamera.Priority > 0)
            {
                normalCameraFollower.HorizontalAxis.Value = aimCamera.transform.eulerAngles.y;
            }

            if (aimBlendToNormalCamera.Priority > 0)
            {
                normalCameraFollower.HorizontalAxis.Value = aimBlendToNormalCamera.transform.eulerAngles.y;
            }
        }

        private void SetAiming(bool isAiming)
        {
            if (normalMoveController != null) normalMoveController.enabled = !isAiming;
            if (aimMoveController != null) aimMoveController.enabled = isAiming;
            if (normalAnimationController != null) normalAnimationController.enabled = !isAiming;
            if (aimAnimationController != null) aimAnimationController.enabled = isAiming;
            StartCoroutine(SwitchToCamera(isAiming));
        }

        private IEnumerator SwitchToCamera(bool isAiming)
        {
            if (isAiming)
            {
                aimCamera.Priority = 10;
                normalCamera.Priority = 0;
            }
            else
            {
                aimBlendToNormalCamera.transform.position = aimCamera.transform.position;
                aimBlendToNormalCamera.transform.rotation = aimCamera.transform.rotation;
                aimBlendToNormalCamera.Priority = 20;
                yield return null;
                aimBlendToNormalCamera.Priority = -1;
                aimCamera.Priority = 0;
                normalCamera.Priority = 10;
            }
        }
    }
}