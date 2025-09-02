using KahaGameCore.Package.ActorSystem.Definition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SurvivorGameCore.ActorController
{
    public class AnimationController : ControllerBase
    {
        private InputAction moveAction;
        private bool _lastMovementState = false;


        private void Start()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            if (moveAction == null)
            {
                Debug.LogError("Move action not found in Input System.");
            }
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

                if (isMoving && !_lastMovementState)
                {
                    controlTarget.Animator.CrossFade("Walk", 0.2f, 0, 0f);
                }
                else if (_lastMovementState && !isMoving)
                {
                    controlTarget.Animator.CrossFade("Idle", 0.2f, 0, 0f);
                }

                _lastMovementState = isMoving;
            }
        }
    }
}
