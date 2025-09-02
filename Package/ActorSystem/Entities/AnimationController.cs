using KahaGameCore.Package.ActorSystem.Definition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SurvivorGameCore.ActorController
{
    public class AnimationController : ControllerBase
    {
        private InputAction moveAction;
        private bool _lastMovementState = false;

        [SerializeField] private bool isAiming = false;

        private float xInput;
        private float yInput;

        private void Start()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            if (moveAction == null)
            {
                Debug.LogError("Move action not found in Input System.");
            }
        }

        private void OnEnable()
        {
            _lastMovementState = false;
        }

        protected override void OnTick()
        {
            if (controlTarget == null || controlTarget.Animator == null)
            {
                Debug.LogError("Control target or its Animator is not assigned in Animation Controller [" + gameObject.name + "]", this);
                return;
            }

            HandleInputMovement();
        }

        private void HandleInputMovement()
        {
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                bool isMoving = moveInput != Vector2.zero;

                xInput = Mathf.Lerp(xInput, moveInput.x, Time.deltaTime * 10f);
                yInput = Mathf.Lerp(yInput, moveInput.y, Time.deltaTime * 10f);

                controlTarget.Animator.SetFloat("x", xInput);
                controlTarget.Animator.SetFloat("y", yInput);

                if (isMoving && !_lastMovementState)
                {
                    if (isAiming)
                    {
                        controlTarget.Animator.CrossFade("AimWalk", 0.1f, 0, 0f);
                    }
                    else
                    {
                        controlTarget.Animator.CrossFade("Walk", 0.1f, 0, 0f);
                    }
                }
                else if (_lastMovementState && !isMoving)
                {
                    if (isAiming)
                    {
                        controlTarget.Animator.CrossFade("AimIdle", 0.1f, 0, 0f);
                    }
                    else
                    {
                        controlTarget.Animator.CrossFade("Idle", 0.1f, 0, 0f);
                    }
                }

                _lastMovementState = isMoving;
            }
        }
    }
}
