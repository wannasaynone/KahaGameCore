using KahaGameCore.Package.ActorSystem.Definition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SurvivorGameCore.ActorController
{
    public class MoveController : ControllerBase
    {
        public enum CoordinateType
        {
            XZ,
            XY
        }

        [SerializeField] private string moveSpeedValueKey = "MoveSpeed";
        [SerializeField] private float valueBase = 100f; // Base value for speed calculation
        [SerializeField] private float minDistance = 1.0f; // Minimum distance to maintain when chasing
        [SerializeField] private CoordinateType coordinateType = CoordinateType.XZ;

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
            if (controlTarget == null)
            {
                Debug.LogError("Control target or its Animator is not assigned in Move Controller [" + gameObject.name + "]", this);
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

                if (isMoving)
                {
                    Vector3 moveDirection = coordinateType == CoordinateType.XZ
                        ? new Vector3(moveInput.x, 0, moveInput.y).normalized
                        : new Vector3(moveInput.x, moveInput.y, 0).normalized;

                    // Update the facing direction in the Instance
                    controlTarget.UpdateFacingDirection(moveDirection);

                    // Move the character
                    float speed = controlTarget.GetTotal(moveSpeedValueKey, false) / valueBase;
                    controlTarget.transform.position += moveDirection * speed * Time.deltaTime;
                }
                else if (_lastMovementState) // Only publish event when state changes
                {

                }

                _lastMovementState = isMoving; // Record last movement state
            }
        }
    }
}
