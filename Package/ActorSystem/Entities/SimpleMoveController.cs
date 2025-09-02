using KahaGameCore.Package.ActorSystem.Definition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.SurvivorGameCore.ActorController
{
    public class SimpleMoveController : ControllerBase
    {
        public enum CoordinateType
        {
            XZ,
            XY,
            CameraRelative
        }

        public enum RotateType
        {
            FaceMovementDirection,
            FixedDirection,
            RotateWithMouse
        }

        [SerializeField] private string moveSpeedValueKey = "MoveSpeed";
        [SerializeField] private float valueBase = 100f; // Base value for speed calculation
        [SerializeField] private float minDistance = 1.0f; // Minimum distance to maintain when chasing
        [SerializeField] private CoordinateType coordinateType = CoordinateType.XZ;
        [SerializeField] private RotateType rotateType = RotateType.FaceMovementDirection;
        [SerializeField] private float mouseRotationSensitivity = 2.0f; // Sensitivity for mouse rotation

        private InputAction moveAction;
        private InputAction lookAction;
        private Vector3 initialDirection; // Store initial direction for FixedDirection mode

        private void Start()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            if (moveAction == null)
            {
                Debug.LogError("Move action not found in Input System.");
            }

            lookAction = InputSystem.actions.FindAction("Look");
            if (lookAction == null)
            {
                Debug.LogError("Look action not found in Input System.");
            }

            // Store initial forward direction for FixedDirection mode
            if (controlTarget != null)
            {
                initialDirection = controlTarget.transform.forward;
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
            HandleRotation();
        }

        private void HandleInputMovement()
        {
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                bool isMoving = moveInput != Vector2.zero;

                if (isMoving)
                {
                    switch (coordinateType)
                    {
                        case CoordinateType.XZ:
                            MoveInXZPlane(moveInput);
                            break;
                        case CoordinateType.XY:
                            MoveInXYPlane(moveInput);
                            break;
                        case CoordinateType.CameraRelative:
                            MoveRelativeToCamera(moveInput);
                            break;
                    }
                }
            }
        }

        private void MoveInXZPlane(Vector2 moveInput)
        {
            Vector3 direction;
            float speed = GetMovementSpeed();

            // Handle character-relative movement for RotateWithMouse
            if (rotateType == RotateType.RotateWithMouse)
            {
                // Get the character's forward and right vectors (projected onto XZ plane)
                Vector3 forward = controlTarget.transform.forward;
                Vector3 right = controlTarget.transform.right;

                // Project onto XZ plane
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                // Transform input direction to be relative to character orientation
                direction = (right * moveInput.x + forward * moveInput.y).normalized;
            }
            else
            {
                // Standard world-space movement for other rotation types
                direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;

                // Only update facing direction if using FaceMovementDirection
                if (rotateType == RotateType.FaceMovementDirection && direction.sqrMagnitude > 0.001f)
                {
                    controlTarget.UpdateFacingDirection(direction);
                }
            }

            controlTarget.transform.position += direction * speed * Time.deltaTime;
        }

        private void MoveInXYPlane(Vector2 moveInput)
        {
            Vector3 direction;
            float speed = GetMovementSpeed();

            // Handle character-relative movement for RotateWithMouse
            if (rotateType == RotateType.RotateWithMouse)
            {
                // Get the character's forward and right vectors (projected onto XY plane)
                Vector3 forward = controlTarget.transform.forward;
                Vector3 right = controlTarget.transform.right;

                // For XY plane, we need to handle this differently
                // We'll use the character's right and up vectors
                forward.z = 0;
                right.z = 0;
                forward.Normalize();
                right.Normalize();

                // Transform input direction to be relative to character orientation
                direction = (right * moveInput.x + forward * moveInput.y).normalized;
            }
            else
            {
                // Standard world-space movement for other rotation types
                direction = new Vector3(moveInput.x, moveInput.y, 0).normalized;

                // Only update facing direction if using FaceMovementDirection
                if (rotateType == RotateType.FaceMovementDirection && direction.sqrMagnitude > 0.001f)
                {
                    controlTarget.UpdateFacingDirection(direction);
                }
            }

            controlTarget.transform.position += direction * speed * Time.deltaTime;
        }

        private void MoveRelativeToCamera(Vector2 moveInput)
        {
            Vector3 direction;
            float speed = GetMovementSpeed();

            // Determine the movement direction based on rotation type
            if (rotateType == RotateType.RotateWithMouse)
            {
                // Use character-relative movement for RotateWithMouse
                // Get the character's forward and right vectors
                Vector3 forward = controlTarget.transform.forward;
                Vector3 right = controlTarget.transform.right;

                // Project onto XZ plane
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                // Transform input direction to be relative to character orientation
                direction = (right * moveInput.x + forward * moveInput.y).normalized;
            }
            else
            {
                // Standard camera-relative movement for other rotation types
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("Main camera not found for camera-relative movement.");
                    return;
                }

                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                // Project forward and right vectors onto the XZ plane
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                direction = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;

                // Only update facing direction if using FaceMovementDirection
                if (rotateType == RotateType.FaceMovementDirection && direction.sqrMagnitude > 0.001f)
                {
                    controlTarget.UpdateFacingDirection(direction);
                }
            }

            // Apply movement
            controlTarget.transform.position += direction * speed * Time.deltaTime;
        }

        private float GetMovementSpeed()
        {
            if (controlTarget == null)
                return 0f;

            int speedValue = controlTarget.GetTotal(moveSpeedValueKey, false);
            return speedValue / 100f * valueBase;
        }

        private void HandleRotation()
        {
            if (controlTarget == null)
                return;

            switch (rotateType)
            {
                case RotateType.FixedDirection:
                    // For FixedDirection, we don't update rotation at all
                    // The character will maintain its initial rotation
                    if (initialDirection != Vector3.zero)
                    {
                        controlTarget.UpdateFacingDirection(initialDirection, 10f);
                    }
                    break;

                case RotateType.RotateWithMouse:
                    if (lookAction != null)
                    {
                        // Get mouse delta movement
                        Vector2 lookDelta = lookAction.ReadValue<Vector2>();

                        if (Mathf.Abs(lookDelta.x) > 0.001f)
                        {
                            // Create rotation around Y axis based on mouse X movement
                            float rotationAmount = lookDelta.x * mouseRotationSensitivity * Time.deltaTime;

                            // Apply rotation to the transform
                            controlTarget.transform.Rotate(0, rotationAmount, 0);
                        }
                    }
                    break;

                case RotateType.FaceMovementDirection:
                    // This is handled in the movement methods
                    break;
            }
        }
    }
}
